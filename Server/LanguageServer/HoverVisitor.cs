using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using CSharpFunctionalExtensions;
using Namotion.Reflection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;
using Entity = CSharpFunctionalExtensions.Entity;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer
{
    public class HoverVisitor : SCLBaseVisitor<Hover?>
    {
        public HoverVisitor(Position position, StepFactoryStore stepFactoryStore, string fullSCL)
        {
            Position = position;
            StepFactoryStore = stepFactoryStore;


            LazyTypeResolver = new Lazy<Result<TypeResolver, IError>>(
                () =>
                    SCLParsing.TryParseStep(fullSCL).Bind(
                        x => TypeResolver.TryCreate(stepFactoryStore, SCLRunner.RootCallerMetadata,
                            Maybe<VariableName>.None, x))
            );
        }

        public Position Position { get; }
        public StepFactoryStore StepFactoryStore { get; }
        public Lazy<Result<TypeResolver, IError>> LazyTypeResolver { get; }

        /// <inheritdoc />
        protected override bool ShouldVisitNextChild(IRuleNode node, Hover? currentResult)
        {
            return currentResult == null;
        }

        /// <inheritdoc />
        public override Hover? Visit(IParseTree tree)
        {
            if (tree is ParserRuleContext context && context.ContainsPosition(Position))
                return base.Visit(tree);


            return DefaultResult;
        }

        /// <inheritdoc />
        public override Hover? VisitFunction(SCLParser.FunctionContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            var name = context.NAME().GetText();

            if (StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
            {
                if (!context.NAME().Symbol.ContainsPosition(Position))
                {
                    var positionalTerms = context.term();

                    for (var index = 0; index < positionalTerms.Length; index++)
                    {
                        var term = positionalTerms[index];

                        if (term.ContainsPosition(Position))
                        {
                            if (
                                stepFactory.ParameterDictionary.TryGetValue(
                                    new StepParameterReference.Index(index + 1),
                                    out var pi
                                ))
                            {
                                var nHover = Visit(term);

                                if (nHover is null)
                                {
                                    var type = GetTypeReference(pi);

                                    return Description(
                                        pi.Name,
                                        type.Name,
                                        pi.GetXmlDocsSummary(), term.GetRange());
                                }


                                return nHover;
                            }

                            return Error($"Step '{name}' does not take an argument {index}", context.GetRange());
                        }
                    }

                    foreach (var namedArgumentContext in context.namedArgument())
                    {
                        if (namedArgumentContext.ContainsPosition(Position))
                        {
                            var argumentName = namedArgumentContext.NAME().GetText();

                            if (stepFactory.ParameterDictionary.TryGetValue(
                                new StepParameterReference.Named(argumentName),
                                out var pi
                            ))
                            {
                                var nHover = Visit(namedArgumentContext);

                                var type = GetTypeReference(pi);

                                if (nHover is null)
                                    return Description(
                                        pi.Name,
                                        type.Name,
                                        pi.GetXmlDocsSummary(), namedArgumentContext.GetRange());

                                return nHover;
                            }

                            return Error(
                                $"Step '{name}' does not take an argument {argumentName}"
                                , context.GetRange());
                        }
                    }
                }

                var summary = stepFactory.StepType.GetXmlDocsSummary();

                return Description(
                    stepFactory.TypeName,
                    stepFactory.OutputTypeExplanation,
                    summary
                    , context.GetRange());
            }
            else
            {
                return Error(name, context.GetRange());
            }
        }

        /// <inheritdoc />
        public override Hover? VisitNumber(SCLParser.NumberContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            var text = context.GetText();
            var typeReference = int.TryParse(text, out var _)
                ? TypeReference.Actual.Integer
                : TypeReference.Actual.Double;


            return Description(text, typeReference.Name, null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitBoolean(SCLParser.BooleanContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            return Description(context.GetText(), TypeReference.Actual.Bool.Name, null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitEntity(SCLParser.EntityContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            foreach (var contextChild in context.children)
            {
                var r = Visit(contextChild);
                if (r is not null) return r;
            }

            return Description(context.GetText(), TypeReference.Actual.Entity.Name, null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitDateTime(SCLParser.DateTimeContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            return Description(context.GetText(), TypeReference.Actual.Date.Name, null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitEnumeration(SCLParser.EnumerationContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            if (context.children.Count != 3 || context.NAME().Length != 2)
                return null;

            var prefix = context.NAME(0).GetText();
            var suffix = context.NAME(1).GetText();

            if (!StepFactoryStore.EnumTypesDictionary.TryGetValue(prefix, out var enumType))
            {
                return Error($"'{prefix}' is not a valid enum type.", context.GetRange());
            }

            if (!Enum.TryParse(enumType, suffix, true, out var value))
            {
                return Error($"'{suffix}' is not a member of enumeration '{prefix}'", context.GetRange());
            }

            return Description(value!.ToString(), enumType.Name, value.GetType().GetXmlDocsSummary(),
                context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitGetAutomaticVariable(SCLParser.GetAutomaticVariableContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;


            return Description("<>", null, "Automatic Variable", context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitGetVariable(SCLParser.GetVariableContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;


            if (LazyTypeResolver.Value.IsFailure)
                return Description(context.GetText(), nameof(VariableName), null, context.GetRange());

            var vn = new VariableName(context.GetText().TrimStart('<').TrimEnd('>'));

            if (LazyTypeResolver.Value.Value.Dictionary.TryGetValue(vn, out var tr))
            {
                return Description(context.GetText(), tr.Name, null, context.GetRange());
            }

            return Description(context.GetText(), nameof(VariableName), null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitSetVariable(SCLParser.SetVariableContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            var variableHover = VisitVariable(context.VARIABLENAME());
            if (variableHover is not null) return variableHover;

            var h2 = Visit(context.step());

            if (h2 is not null) return h2;

            var setVariable = new SetVariable<int>().StepFactory;


            return Description(setVariable.TypeName, setVariable.OutputTypeExplanation,
                setVariable.StepType.GetXmlDocsSummary(), context.GetRange());
        }

        public Hover? VisitVariable(ITerminalNode variableNameNode)
        {
            if (!variableNameNode.Symbol.ContainsPosition(Position))
                return null;


            var text = variableNameNode.GetText();

            if (LazyTypeResolver.Value.IsFailure)
                return Description(text, nameof(VariableName), null, variableNameNode.Symbol.GetRange());

            var vn = new VariableName(text.TrimStart('<').TrimEnd('>'));

            if (LazyTypeResolver.Value.Value.Dictionary.TryGetValue(vn, out var tr))
            {
                return Description(text, tr.Name, null, variableNameNode.Symbol.GetRange());
            }

            return Description(text, nameof(VariableName), null, variableNameNode.Symbol.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitQuotedString(SCLParser.QuotedStringContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            return Description(context.GetText(), TypeReference.Actual.String.Name, null, context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitArray(SCLParser.ArrayContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            foreach (var contextChild in context.children)
            {
                var h1 = Visit(contextChild);
                if (h1 is not null)
                    return h1;
            }

            return DescribeStep(context.GetText(), context.GetRange());
        }

        /// <inheritdoc />
        public override Hover? VisitInfixOperation(SCLParser.InfixOperationContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            foreach (var termContext in context.term())
            {
                var h1 = Visit(termContext);
                if (h1 is not null)
                    return h1;
            }

            var operatorSymbols =
                context.infixOperator().Select(x => x.GetText()).Distinct().ToList();

            if (operatorSymbols.Count != 1)
            {
                return Error("Invalid mix of operators", context.GetRange());
            }

            return DescribeStep(context.GetText(), context.GetRange());
        }

        public Hover DescribeStep(string text, Range range)
        {
            var step = SCLParsing.TryParseStep(text);

            if (step.IsFailure)
                return Error(step.Error.AsString, range);

            var callerMetadata = new CallerMetadata("Step", "Parameter", TypeReference.Any.Instance);

            Result<IStep, IError> freezeResult;

            if (LazyTypeResolver.Value.IsFailure)
            {
                freezeResult = step.Value.TryFreeze(callerMetadata, StepFactoryStore);
            }
            else
            {
                freezeResult = step.Value.TryFreeze(callerMetadata, LazyTypeResolver.Value.Value);
            }

            if (freezeResult.IsFailure)
                return Error(freezeResult.Error.AsString, range);


            return Description(freezeResult.Value, range);
        }

        public static Hover Description(IStep step, Range range)
        {
            var name = step.Name;
            string type = GetHumanReadableTypeName(step.OutputType);
            string? description;

            if (step is ICompoundStep cs)
            {
                description = cs.StepFactory.StepType.GetXmlDocsSummary();
            }

            else
            {
                description = null;
            }


            return Description(name, type, description, range);
        }

        public static Hover Description(string? name, string? type, string? summary, Range range)
        {
            var markedStrings = new[] {$"`{name}`", $"`{type}`", summary}
                .WhereNotNull()
                .Select(x => new MarkedString(x)).ToList();

            return new()
            {
                Range = range,
                Contents = new MarkedStringsOrMarkupContent(markedStrings)
            };
        }

        public static Hover Error(string message, Range range)
        {
            return new()
            {
                Range = range,
                Contents = new MarkedStringsOrMarkupContent(message)
            };
        }

        private static TypeReference GetTypeReference(PropertyInfo propertyInfo)
        {
            return TypeReference.CreateFromStepType(propertyInfo.PropertyType);
        }

        public static string GetHumanReadableTypeName(Type t)
        {
            if (!t.IsSignatureType && t.IsEnum)
                return t.Name;

            if (TypeAliases.TryGetValue(t, out var name))
                return name;

            if (!t.IsGenericType)
                return t.Name;

            if (t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var underlyingType = Nullable.GetUnderlyingType(t);

                if (underlyingType == null)
                    return t.Name;

                return GetHumanReadableTypeName(underlyingType) + "?";
            }

            var typeName = t.Name.Split("`")[0];

            var arguments =
                $"<{string.Join(",", t.GetGenericArguments().Select(GetHumanReadableTypeName))}>";

            return typeName + arguments;
        }

        private static readonly Dictionary<Type, string> TypeAliases =
            new()
            {
                {typeof(byte), "byte"},
                {typeof(sbyte), "sbyte"},
                {typeof(short), "short"},
                {typeof(ushort), "ushort"},
                {typeof(int), "int"},
                {typeof(uint), "uint"},
                {typeof(long), "long"},
                {typeof(ulong), "ulong"},
                {typeof(float), "float"},
                {typeof(double), "double"},
                {typeof(decimal), "decimal"},
                {typeof(object), "object"},
                {typeof(bool), "bool"},
                {typeof(char), "char"},
                {typeof(string), "string"},
                {typeof(StringStream), "string"},
                {typeof(Entity), "entity"},
                {typeof(DateTime), "dateTime"},
                {typeof(void), "void"}
            };
    }
}