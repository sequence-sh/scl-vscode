using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Namotion.Reflection;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;

namespace LanguageServer
{
    public class HoverVisitor : SCLBaseVisitor<Hover?>
    {
        public HoverVisitor(Position position, StepFactoryStore stepFactoryStore)
        {
            Position = position;
            StepFactoryStore = stepFactoryStore;
        }

        public Position Position { get; }
        public StepFactoryStore StepFactoryStore { get; }

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
                                    new StepParameterReference(index + 1),
                                    out var pi
                                ))
                            {
                                var nHover = Visit(term);

                                if (nHover is null)
                                    return Description(pi.GetXmlDocsSummary(), term);

                                return nHover;
                            }

                            return Error($"Step '{name}' does not take an argument {index}", context);
                        }
                    }

                    foreach (var namedArgumentContext in context.namedArgument())
                    {
                        if (namedArgumentContext.ContainsPosition(Position))
                        {
                            var argumentName = namedArgumentContext.NAME().GetText();

                            if (stepFactory.ParameterDictionary.TryGetValue(
                                new StepParameterReference(argumentName),
                                out var pi
                            ))
                            {
                                var nHover = Visit(namedArgumentContext);

                                if (nHover is null)
                                    return Description(pi.GetXmlDocsSummary(), namedArgumentContext);

                                return nHover;
                            }

                            return Error($"Step '{name}' does not take an argument {argumentName}", context);
                        }
                    }
                }

                var summary = stepFactory.StepType.GetXmlDocsSummary();

                return Description(summary, context);
            }
            else
            {
                return Error(name, context);
            }
        }

        public static Hover Description(string message, ParserRuleContext context)
        {
            return new()
            {
                Range = context.GetRange(),
                Contents = new MarkedStringsOrMarkupContent(message)
            };
        }

        public static Hover Error(string message, ParserRuleContext context)
        {
            return new()
            {
                Range = context.GetRange(),
                Contents = new MarkedStringsOrMarkupContent(message)
            };
        }
    }
}