using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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


        public static IReadOnlyList<(string text, Position position)> SplitIntoCommands(string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new SCLLexer(inputStream);

            var tokens = lexer.GetAllTokens();

            var newCommandTokenType = lexer.GetTokenType("NEWCOMMAND");

            List<(string text, Position startPosition)> results = new();

            StringBuilder sb = new();
            Position? start = null;

            foreach (var token in tokens)
            {
                if (token.Type == newCommandTokenType)
                {
                    if (start is not null)
                    {
                        results.Add((sb.ToString(), start));
                    }

                    sb = new StringBuilder();
                    start = null;
                }

                if (start == null)
                {
                    var trimmedText = token.Text;
                    var lineOffset = 0;

                    while (true)
                    {
                        if (trimmedText.StartsWith('\n'))
                            trimmedText = trimmedText[1..];
                        else if (trimmedText.StartsWith("\r\n"))
                            trimmedText = trimmedText[2..];
                        else
                            break;

                        lineOffset++;
                    }

                    start = new(token.Line + lineOffset - 1, lineOffset > 0 ? 0 : token.Column);
                    sb.Append(trimmedText);
                }
                else
                {
                    sb.Append(token.Text);
                }
            }

            if (start is not null)
                results.Add((sb.ToString(), start));

            return results;
        }


        public static (string command, Position newPosition)? GetCommand(string text, Position originalPosition)
        {
            var commands = SplitIntoCommands(text);
            var myCommand = commands.TakeWhile(x => x.position <= originalPosition).LastOrDefault();

            if (myCommand == default) return null;

            Position newPosition;
            if (originalPosition.Line == myCommand.position.Line)
            {
                newPosition = new Position(0, originalPosition.Character - myCommand.position.Character);
            }
            else
            {
                newPosition = new Position(originalPosition.Line - myCommand.position.Line, originalPosition.Character);
            }

            return (myCommand.text, newPosition);
        }

        public static string RemoveToken(string text, Position tokenPosition)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new SCLLexer(inputStream);

            StringBuilder sb = new();
            foreach (var token in lexer.GetAllTokens())
            {
                if (token.ContainsPosition(tokenPosition))
                {
                    var length = token.StopIndex - token.StartIndex;

                    var ws = new string(' ', length);
                    sb.Append(ws);
                }
                else
                {
                    sb.Append(token.Text);
                }
            }

            return sb.ToString();
        }

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

        //public static Range GetRange(this TextLocation textLocation)
        //{
        //    return new(
        //        textLocation.Start.Line - 1, textLocation.Start.Column,
        //        textLocation.Stop.Line - 1, textLocation.Stop.Index + textLocation.Stop.Interval.Length
        //    );
        //}

        public static Range GetRange(this TextLocation textLocation, int lineOffset, int charOffSet)
        {
            return new(textLocation.Start.GetFromOffset(lineOffset, charOffSet),
                textLocation.Stop.GetFromOffset(lineOffset, charOffSet)
            );
        }

        public static Position GetFromOffset(this TextPosition position, int lineOffset, int charOffSet)
        {
            if (position.Line == 1)
                //same line, add columns
                return new Position(lineOffset, position.Column + charOffSet);
            else //add lines
                return new Position(position.Line - 1 + lineOffset, position.Column);
        }


        public static T LexParseAndVisit<T>(this SCLBaseVisitor<T> visitor, string text, Action<SCLLexer> setupLexer,
            Action<SCLParser> setupParser)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new SCLLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new SCLParser(commonTokenStream);

            setupLexer(lexer);
            setupParser(parser);

            var result = visitor.Visit(parser.fullSequence());

            return result;
        }

        public static string GetMarkDownDocumentation(IStepFactory stepFactory)
        {
            var grouping = new[] { stepFactory }
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