using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Sequence.Core.Internal;
using Sequence.Core.Internal.Parser;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer;

/// <summary>
/// General helper methods for the Language Server
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Does this token contain this position
    /// </summary>
    public static bool ContainsPosition(this IToken token, Position position)
    {
        return token.StartsBeforeOrAt(position) && token.EndsAfterOrAt(position);
    }

    /// <summary>
    /// Gets the range of this token
    /// </summary>
    public static Range GetRange(this IToken token)
    {
        return new(
            token.Line - 1,
            token.Column,
            token.Line - 1,
            token.Column + token.Text.Length
        );
    }

    /// <summary>
    /// Returns whether this node siblings after the position
    /// </summary>
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

    /// <summary>
    /// Whether this token starts before or at the position
    /// </summary>
    public static bool StartsBeforeOrAt(this IToken token, Position position)
    {
        if (token.Line - 1 < position.Line)
            return true;
        else if (token.Line - 1 == position.Line)
            return token.Column <= position.Character;
        return false;
    }

    /// <summary>
    /// Whether this token ends after or at the position
    /// </summary>
    public static bool EndsAfterOrAt(this IToken token, Position position)
    {
        if (token.Line - 1 < position.Line)
            return false;
        else if (token.Line - 1 == position.Line)
            return (token.Column + token.Text.Length) >= position.Character;
        return true;
    }
    /// <summary>
    /// Whether this token ends at the position
    /// </summary>
    public static bool EndsAt(this IToken token, Position position)
    {
        if (token.Line - 1 < position.Line)
            return false;
        else if (token.Line - 1 == position.Line)
            return (token.Column + token.Text.Length) == position.Character;
        return true;
    }

    /// <summary>
    /// Splits SCL text into commands
    /// </summary>
    public static IReadOnlyList<(string text, Position position)> SplitIntoCommands(string text)
    {
        var inputStream = new AntlrInputStream(text);
        var lexer = new SCLLexer(inputStream, TextWriter.Null, TextWriter.Null);

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

    /// <summary>
    /// Gets a particular command from the text
    /// </summary>
    public static (string command, Position newPosition, Position positionOffset)? GetCommand(string text, Position originalPosition)
    {
        var commands = SplitIntoCommands(text);
        var myCommand = commands.TakeWhile(x => x.position <= originalPosition).LastOrDefault();

        if (myCommand == default) return null;

        Position newPosition;
        Position offsetPosition;
        if (originalPosition.Line == myCommand.position.Line)
        {
            newPosition = new Position(0, originalPosition.Character - myCommand.position.Character);
            offsetPosition = myCommand.position;
        }
        else
        {
            newPosition = new Position(originalPosition.Line - myCommand.position.Line, originalPosition.Character);
            offsetPosition = myCommand.position;
        }

        return (myCommand.text, newPosition, offsetPosition);
    }
    

    /// <summary>
    /// Whether the context contains the position
    /// </summary>
    public static bool ContainsPosition(this ParserRuleContext context, Position position)
    {
        if (!context.Start.StartsBeforeOrAt(position))
            return false;
        if (!context.Stop.EndsAfterOrAt(position))
            return false;
        return true;
    }

    /// <summary>
    /// Whether the token is on the same line as the position
    /// </summary>
    public static bool IsSameLineAs(this IToken token, Position position)
    {
        var sameLine = token.Line - 1 == position.Line;
        return sameLine;
    }

    /// <summary>
    /// Whether the context ends before the position
    /// </summary>
    public static bool EndsBefore(this ParserRuleContext context, Position position) =>
        !context.Stop.EndsAfterOrAt(position);

    /// <summary>
    /// Whether the context starts after the position
    /// </summary>
    public static bool StartsAfter(this ParserRuleContext context, Position position) =>
        !context.Start.StartsBeforeOrAt(position);

    /// <summary>
    /// Get the range of the context
    /// </summary>
    public static Range GetRange(this ParserRuleContext context)
    {
        return new(
            context.Start.Line - 1,
            context.Start.Column,
            context.Stop.Line - 1,
            context.Stop.Column + context.Stop.Text.Length
        );
    }
        
    /// <summary>
    /// Get the range of the Text Location
    /// </summary>
    public static Range GetRange(this TextLocation textLocation, int lineOffset, int charOffSet)
    {
        return new(textLocation.Start.GetFromOffset(lineOffset, charOffSet),
            textLocation.Stop.GetFromOffset(lineOffset, charOffSet)
        );
    }

    /// <summary>
    /// Offsets the range by the position
    /// </summary>
    public static Range Offset(this Range range, Position offset)
    {
        return new Range(new Position(offset.Line + range.Start.Line, range.Start.Character), new Position(offset.Line + range.End.Line, range.End.Character));
    }

    /// <summary>
    /// Get the position adjusted by the offset
    /// </summary>
    public static Position GetFromOffset(this TextPosition position, int lineOffset, int charOffSet)
    {
        if (position.Line == 1)
            //same line, add columns
            return new Position(lineOffset, position.Column + charOffSet);
        else //add lines
            return new Position(position.Line - 1 + lineOffset, position.Column);
    }
}