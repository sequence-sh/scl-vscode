using System;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Documentation;
using Reductech.EDR.Core.Internal.Parser;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer
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

        public static bool HasSiblingsAfter(this IRuleNode ruleContext, Position p)
        {
            if (ruleContext.Parent is ParserRuleContext prc)
            {
                if (prc.children.Reverse().OfType<ParserRuleContext>()
                    .Any(c => c.ContainsPosition(p) || c.StartsAfter(p)))
                    return true;

                return HasSiblingsAfter(prc, p);
            }

            return false;
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

        public static bool EndsAt(this IToken token, Position position)
        {
            if (token.Line - 1 < position.Line)
                return false;
            else if (token.Line - 1 == position.Line)
                return (token.Column + token.Text.Length) == position.Character;
            return true;
        }

        //public static bool ContainsPosition(this IParseTree parseTree, Position position)
        //{
        //    if (parseTree is ParserRuleContext prc)
        //        return prc.ContainsPosition(position);

        //    return false;
        //}

        //public static bool StartsAfter(this IParseTree parseTree, Position position)
        //{
        //    if (parseTree is IToken token)
        //        return !token.StartsBeforeOrAt(position);
        //    else if (parseTree is ParserRuleContext prc)
        //        return prc.StartsAfter(position);

        //    return false;
        //}
        //public static bool EndsBefore(this IParseTree parseTree, Position position)
        //{
        //    if (parseTree is IToken token)
        //        return !token.EndsAfterOrAt(position);
        //    else if (parseTree is ParserRuleContext prc)
        //        return prc.EndsBefore(position);

        //    return false;
        //}

        public static bool ContainsPosition(this ParserRuleContext context, Position position)
        {
            if (!context.Start.StartsBeforeOrAt(position))
                return false;
            if (!context.Stop.EndsAfterOrAt(position))
                return false;
            return true;
        }

        public static bool IsSameLineAs(this IToken token, Position position)
        {
            var sameLine = token.Line - 1 == position.Line;
            return sameLine;
        }

        public static bool EndsBefore(this ParserRuleContext context, Position position) =>
            !context.Stop.EndsAfterOrAt(position);

        public static bool StartsAfter(this ParserRuleContext context, Position position) =>
            !context.Start.StartsBeforeOrAt(position);

        public static Range GetRange(this ParserRuleContext context)
        {
            return new(
                context.Start.Line - 1,
                context.Start.Column,
                context.Stop.Line - 1,
                context.Stop.Column + context.Stop.Text.Length
            );
        }

        public static Range GetRange(this TextLocation textLocation)
        {
            return new(
                textLocation.Start.Line - 1, textLocation.Start.Column,
                textLocation.Stop.Line - 1, textLocation.Stop.Index + textLocation.Stop.Interval.Length
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
            var stepWrapper = new StepWrapper(stepFactoryGroup);

            try
            {
                var text = DocumentationCreator.GetStepPage(stepWrapper);

                return text.FileText;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}