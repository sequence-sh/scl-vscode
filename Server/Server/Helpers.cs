using System.Linq;
using Antlr4.Runtime;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Documentation;
using Reductech.EDR.Core.Internal.Parser;

namespace Server
{
    public static class Helpers
    {
        public static bool ContainsPosition(this IToken token, Position position)
        {
            return token.StartsBeforeOrAt(position) && token.EndsAfterOrAt(position);
        }

        public static Range GetRange(this IToken token)
        {
            return new(
                token.Line - 1,
                token.Column,
                token.Line - 1,
                token.Column + token.Text.Length
            );
        }

        public static bool StartsBeforeOrAt(this IToken token, Position position)
        {
            if (token.Line - 1 < position.Line)
                return true;
            else if (token.Line - 1 == position.Line)
                return token.Column <= position.Character;
            return false;
        }

        public static bool EndsAfterOrAt(this IToken token, Position position)
        {
            if (token.Line - 1 < position.Line)
                return false;
            else if (token.Line - 1 == position.Line)
                return (token.Column + token.Text.Length) >= position.Character;
            return true;
        }


        public static bool ContainsPosition(this ParserRuleContext context, Position position)
        {
            return context.Start.StartsBeforeOrAt(position) && context.Stop.EndsAfterOrAt(position);
        }

        public static Range GetRange(this ParserRuleContext context)
        {
            return new(
                context.Start.Line - 1,
                context.Start.Column,
                context.Stop.Line - 1,
                context.Stop.Column + context.Stop.Text.Length
            );
        }

        public static T LexParseAndVisit<T>(this SCLBaseVisitor<T> visitor, string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new SCLLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new SCLParser(commonTokenStream);

            //Todo error strategy

            lexer.RemoveErrorListeners();
            parser.RemoveErrorListeners();


            var result = visitor.Visit(parser.fullSequence());

            return result;
        }

        public static string GetMarkDownDocumentation(IStepFactory stepFactory)
        {
            var grouping = new[] {stepFactory}
                .GroupBy(x => x, x => x.TypeName).Single();

            return GetMarkDownDocumentation(grouping);
        }

        public static string GetMarkDownDocumentation(IGrouping<IStepFactory, string> stepFactoryGroup)
        {
            var text = DocumentationCreator.GetPageText(new StepWrapper(stepFactoryGroup));

            return text;
        }
    }
}