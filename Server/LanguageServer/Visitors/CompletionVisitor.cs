using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Namotion.Reflection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer.Visitors
{
    public class CompletionVisitor : SCLBaseVisitor<CompletionList?>
    {
        public CompletionVisitor(Position position, StepFactoryStore stepFactoryStore)
        {
            Position = position;
            StepFactoryStore = stepFactoryStore;
        }

        public Position Position { get; }
        public StepFactoryStore StepFactoryStore { get; }


        /// <inheritdoc />
        protected override bool ShouldVisitNextChild(IRuleNode node, CompletionList? currentResult)
        {
            if (currentResult is null)
                return true;
            return false;
        }

        /// <inheritdoc />
        public override CompletionList? Visit(IParseTree tree)
        {
            if (tree is ParserRuleContext context)
            {
                if (context.ContainsPosition(Position))
                {
                    return base.Visit(tree);
                }
                else if (context.EndsBefore(Position) && !context.HasSiblingsAfter(Position))
                {
                    //This position is at the end of this line - enter anyway
                    return base.Visit(tree);
                }
            }

            return DefaultResult;
        }

        /// <inheritdoc />
        public override CompletionList? VisitChildren(IRuleNode node)
        {
            var result = this.DefaultResult;
            int childCount = node.ChildCount;
            for (int i = 0; i < childCount && this.ShouldVisitNextChild(node, result); ++i)
            {
                var nextResult = node.GetChild(i).Accept(this);
                result = this.AggregateResult(result, nextResult);
            }
            return result;
        }

        /// <inheritdoc />
        public override CompletionList? VisitErrorNode(IErrorNode node)
        {
            if(node.Symbol.ContainsPosition(Position))
            {
                return base.VisitErrorNode(node);
            }

            return base.VisitErrorNode(node);
        }

        /// <inheritdoc />
        public override CompletionList? VisitFunction(SCLParser.FunctionContext context)
        {
            var name = context.NAME().GetText();

            if (!context.ContainsPosition(Position))
            {
                if (context.EndsBefore(Position) &&
                    context.Stop.IsSameLineAs(Position)) //This position is on the line after the step definition
                {
                    if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                        return null; //No clue what name to use

                    return StepParametersCompletionList(stepFactory, new Range(Position, Position));
                }

                return null;
            }


            if (context.NAME().Symbol.ContainsPosition(Position))
            {
                var nameText = context.NAME().GetText();

                var options =
                    StepFactoryStore.Dictionary
                        .Where(x => x.Key.Contains(nameText, StringComparison.OrdinalIgnoreCase))
                        .GroupBy(x => x.Value, x => x.Key).ToList();


                return ReplaceWithSteps(options, context.NAME().Symbol.GetRange());
            }

            var positionalTerms = context.term();

            for (var index = 0; index < positionalTerms.Length; index++)
            {
                var term = positionalTerms[index];

                if (term.ContainsPosition(Position))
                {
                    return Visit(term);
                }
            }

            foreach (var namedArgumentContext in context.namedArgument())
            {
                if (namedArgumentContext.ContainsPosition(Position))
                {
                    if (namedArgumentContext.NAME().Symbol.ContainsPosition(Position))
                    {
                        if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                            return null; //Don't know what step factory to use

                        var range = namedArgumentContext.NAME().Symbol.GetRange();

                        return StepParametersCompletionList(stepFactory, range);
                    }


                    return Visit(namedArgumentContext);
                }
            }

            {
                if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                    return null; //No clue what name to use


                return StepParametersCompletionList(stepFactory, new Range(Position, Position));
            }
        }

        public static CompletionList ReplaceWithSteps(IEnumerable<IGrouping<IStepFactory, string>> stepFactories,
            Range range)
        {
            var options = stepFactories.SelectMany(CreateCompletionItems);


            return new CompletionList(options);

            IEnumerable<CompletionItem> CreateCompletionItems(IGrouping<IStepFactory, string> factory)
            {
                var documentation = Helpers.GetMarkDownDocumentation(factory);

                foreach (var key in factory)
                {
                    yield return new()
                    {
                        TextEdit = TextEditOrInsertReplaceEdit.From(new InsertReplaceEdit
                        {
                            Replace = range
                        }),
                        Label = key,
                        InsertTextMode = InsertTextMode.AsIs,
                        InsertTextFormat = InsertTextFormat.PlainText,
                        InsertText = key,
                        Detail = factory.Key.StepType.GetXmlDocsSummary(),
                        Documentation = new StringOrMarkupContent(new MarkupContent
                        {
                            Kind = MarkupKind.Markdown,
                            Value = documentation
                        })
                    };
                }
            }
        }

        public static CompletionList StepParametersCompletionList(IStepFactory stepFactory, Range range)
        {
            var documentation = Helpers.GetMarkDownDocumentation(stepFactory);
            var options =
                stepFactory.ParameterDictionary
                    .Where(x => x.Key is StepParameterReference.Named)
                    .Select(x => CreateCompletionItem(x.Key, x.Value))
                    .ToList();


            CompletionItem CreateCompletionItem(StepParameterReference stepParameterReference,
                PropertyInfo propertyInfo)
            {
                return new()
                {
                    TextEdit =
                        new InsertReplaceEdit()
                        {
                            Replace = range, NewText = stepParameterReference.Name + ":"
                        },
                    Label = stepParameterReference.Name,
                    InsertTextMode = InsertTextMode.AsIs,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    Detail = propertyInfo.GetXmlDocsSummary(),
                    Documentation = new StringOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = documentation
                    })
                };
            }

            return new CompletionList(options);
        }


        public static CompletionList EmptyList()
        {
            return new();
        }
    }
}