using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Namotion.Reflection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;

namespace Server
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
        public override CompletionList? VisitFunction(SCLParser.FunctionContext context)
        {
            if (!context.ContainsPosition(Position))
                return null;

            var name = context.NAME().GetText();


            if (context.NAME().Symbol.ContainsPosition(Position))
            {
                var options =
                        StepFactoryStore.Dictionary
                            .Where(x => x.Key.Contains(context.NAME().GetText()))
                            .GroupBy(x => x.Value, x => x.Key)
                    ;


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

                        return ReplaceWithStepParameters(stepFactory, namedArgumentContext.NAME().Symbol.GetRange());
                    }


                    return Visit(namedArgumentContext);
                }
            }

            {
                if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                    return null; //No clue what name to use

                return ReplaceWithStepParameters(stepFactory, new Range(Position, Position));
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

        public static CompletionList ReplaceWithStepParameters(IStepFactory stepFactory, Range range)
        {
            var documentation = Helpers.GetMarkDownDocumentation(stepFactory);
            var options =
                stepFactory.ParameterDictionary
                    .Where(x => x.Key.Value.IsT0)
                    .Select(x => CreateCompletionItem(x.Key, x.Value))
                    .ToList();


            CompletionItem CreateCompletionItem(StepParameterReference stepParameterReference,
                PropertyInfo propertyInfo)
            {
                return new()
                {
                    TextEdit = TextEditOrInsertReplaceEdit.From(new InsertReplaceEdit()
                    {
                        Replace = range
                    }),
                    Label = stepParameterReference.Value.AsT0,
                    InsertTextMode = InsertTextMode.AsIs,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    InsertText = stepParameterReference.Value.AsT0 + ":",
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

        /// <inheritdoc />
        protected override bool ShouldVisitNextChild(IRuleNode node, CompletionList? currentResult)
        {
            return currentResult is null;
        }

        /// <inheritdoc />
        public override CompletionList? Visit(IParseTree tree)
        {
            if (tree is ParserRuleContext context && context.ContainsPosition(Position))
                return base.Visit(tree);


            return DefaultResult;
        }
    }
}