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

namespace LanguageServer
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
            return currentResult is null;
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
                else if (context.EndsBefore(Position) && !HasSiblingsAfter(context, Position))
                {
                    //This position is at the end of this line - enter anyway
                    return base.Visit(tree);
                }
            }

            return DefaultResult;
        }

        /// <inheritdoc />
        public override CompletionList? VisitTerminal(ITerminalNode node)
        {
            return base.VisitTerminal(node);
        }

        /// <inheritdoc />
        public override CompletionList? VisitTerm1(SCLParser.Term1Context context)
        {
            return base.VisitTerm1(context);
        }

        ///// <inheritdoc />
        //public override CompletionList? VisitChildren(IRuleNode node)
        //{
        //    if (node.ChildCount <= 0)
        //        return null;

        //    if (node.ContainsPosition(Position))
        //    {
        //        return base.VisitChildren(node);
        //    }
        //    else if (node.EndsBefore(Position) && !HasSiblingsAfter(node, Position))
        //    {
        //        //This position is at the end of this line - enter anyway
        //        return Visit(node.GetChild(node.ChildCount - 1));
        //    }

        //    return null;
        //}


        private static bool HasSiblingsAfter(IRuleNode ruleContext, Position p)
        {
            if (ruleContext.Parent is ParserRuleContext prc)
            {
                if (prc.children.Reverse().Any(c => c.ContainsPosition(p) || c.StartsAfter(p)))
                    return true;

                return HasSiblingsAfter(prc, p);
            }

            return false;
        }

        /// <inheritdoc />
        public override CompletionList? VisitFunction(SCLParser.FunctionContext context)
        {
            var name = context.NAME().GetText();

            if (!context.ContainsPosition(Position))
            {
                if (context.EndsBefore(Position) && context.Stop.IsSameLineAs(Position)) //This position is on the line after the step definition
                {
                    if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                        return null; //No clue what name to use

                    var replace = new InsertReplaceEdit() {Replace = new Range(Position, Position)};

                    return StepParametersCompletionList(stepFactory, replace);
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

                if (options.Count() == 1)
                {
                    if (options.Single().Any(k => k.Equals(nameText, StringComparison.OrdinalIgnoreCase))) //The text is exactly the step
                    {
                        if (context.Stop.IsSameLineAs(Position) && context.Stop.EndsAt(Position))
                        {
                            // Insert option straight after
                            var range = new Range(Position, Position);
                            var sfs = options.Single().Key;

                            var insert = new InsertReplaceEdit()
                            {
                                Insert = new Range(Position, Position)
                            };

                            return StepParametersCompletionList(sfs, insert);
                        }
                    }
                }


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
                            return null; //No clue what name to use

                        var replace = new InsertReplaceEdit() {Replace = namedArgumentContext.NAME().Symbol.GetRange()};

                        return StepParametersCompletionList(stepFactory, replace);
                    }


                    return Visit(namedArgumentContext);
                }
            }

            {
                if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                    return null; //No clue what name to use

                var replace = new InsertReplaceEdit() {Replace = new Range(Position, Position)};

                return StepParametersCompletionList(stepFactory,replace);
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

        public static CompletionList StepParametersCompletionList(IStepFactory stepFactory, InsertReplaceEdit insertReplaceEdit)
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
                    TextEdit = TextEditOrInsertReplaceEdit.From(
                        
                        insertReplaceEdit),
                    Label = stepParameterReference.Name,
                    InsertTextMode = InsertTextMode.AsIs,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    InsertText = stepParameterReference.Name + ":",
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