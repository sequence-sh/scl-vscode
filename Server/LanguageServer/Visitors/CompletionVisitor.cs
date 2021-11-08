using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer.Visitors
{
    /// <summary>
    /// Visits SCL for completion
    /// </summary>
    public class CompletionVisitor : SCLBaseVisitor<CompletionList?>
    {
        /// <summary>
        /// Creates a new Completion Visitor
        /// </summary>
        public CompletionVisitor(Position position, StepFactoryStore stepFactoryStore)
        {
            Position = position;
            StepFactoryStore = stepFactoryStore;
        }

        /// <summary>
        /// The position
        /// </summary>
        public Position Position { get; }
        /// <summary>
        /// The Step Factory Store
        /// </summary>
        public StepFactoryStore StepFactoryStore { get; }

        /// <inheritdoc />
        public override CompletionList? VisitChildren(IRuleNode node)
        {
            var i = 0;

            while (i < node.ChildCount)
            {
                var child = node.GetChild(i);

                if (child is TerminalNodeImpl tni && tni.GetText() == "<EOF>")
                {
                    break;
                }

                if (child is ParserRuleContext prc)
                { 
                    if (prc.StartsAfter(Position))
                    {
                        break;
                    }
                    else if(prc.ContainsPosition(Position))
                    {

                        var result = Visit(child);
                        if (result is not null)
                            return result;

                    }
                }
                i++;
            }

            if (i >= 1) //Go back to the last function and use that
            {
                var lastChild = node.GetChild(i - 1);

                var r = Visit(lastChild);
                return r;
            }

            return null;
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
        public override CompletionList? VisitFunction1(SCLParser.Function1Context context)
        {
            var func = context.function();

            var result = VisitFunction(func);

            return result;
        }

        /// <inheritdoc />
        public override CompletionList? VisitFunction(SCLParser.FunctionContext context)
        {
            var name = context.NAME().GetText();

            if (!context.ContainsPosition(Position))
            {
                if (context.EndsBefore(Position))
                {
                    //Assume this is another parameter to this function
                    if(StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
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

        private static CompletionList ReplaceWithSteps(IEnumerable<IGrouping<IStepFactory, string>> stepFactories,
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
                        Detail = factory.Key.Summary,
                        Documentation = new StringOrMarkupContent(new MarkupContent
                        {
                            Kind = MarkupKind.Markdown,
                            Value = documentation
                        })
                    };
                }
            }
        }

        /// <summary>
        /// Gets the step parameter completion list
        /// </summary>
        public static CompletionList StepParametersCompletionList(IStepFactory stepFactory, Range range)
        {
            var documentation = Helpers.GetMarkDownDocumentation(stepFactory);
            var options =
                stepFactory.ParameterDictionary
                    .Where(x => x.Key is StepParameterReference.Named)
                    .Select(x => CreateCompletionItem(x.Key, x.Value))
                    .ToList();


            CompletionItem CreateCompletionItem(StepParameterReference stepParameterReference,
                IStepParameter stepParameter)
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
                    Detail = stepParameter.Summary,
                    Documentation = new StringOrMarkupContent(new MarkupContent()
                    {
                        Kind = MarkupKind.Markdown,
                        Value = documentation
                    })
                };
            }

            return new CompletionList(options);
        }
    }
}