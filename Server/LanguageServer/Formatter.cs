using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Antlr4.Runtime;
using Reductech.Sequence.Core.Internal;
using Reductech.Sequence.Core.Internal.Parser;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.Steps;

namespace LanguageServer;

/// <summary>
/// Contains methods for formatting steps
/// </summary>
public static class Formatter
{
    /// <summary>
    /// Format a step
    /// </summary>
    public static string Format(IStep step)
    {
        var sb = new IndentationStringBuilder();
            
        var usedComments = new HashSet< (int type, int channel, int line, int column)>();

        Format1(sb, step, true, usedComments);

        static void Format1(IndentationStringBuilder sb, IStep step, bool topLevel, HashSet< (int type, int channel, int line, int column)> usedComments)
        {
            if (step is ICompoundStep compoundStep)
            {
                if (compoundStep is ISequenceStep sequenceStep)
                {
                    foreach (var seqStep in sequenceStep.AllSteps)
                    {
                        sb.AppendLine();
                        sb.Append("- ");
                        Format1(sb, seqStep, true, usedComments);
                    }

                    var comments0 = ReadComments(step);
                    foreach (var commentToken in comments0)
                    {
                        if (usedComments.Add(commentToken.GetTokenKey()))
                        {
                            if(commentToken.Type == SCLLexer.DELIMITEDCOMMENT)
                                sb.AppendLine();
                            sb.Append(commentToken.Text);
                        }
                    }

                    return;
                }

                if (compoundStep.StepFactory.Serializer is FunctionSerializer)
                {
                    var allProperties =
                        ((IEnumerable<StepProperty>)compoundStep.GetType().GetProperty("AllProperties")!
                            .GetValue(compoundStep)!).ToList();

                    if (!topLevel && compoundStep.ShouldBracketWhenSerialized)
                        sb.Append("(");
                    if (allProperties.Count > 1)
                    {
                        sb.AppendLine(compoundStep.Name);
                        sb.Indent();
                    }
                    else
                        sb.Append(compoundStep.Name+ " ");
                        

                    foreach (var stepProperty in allProperties)
                    {
                        sb.Append(stepProperty.Name);
                        sb.Append(": ");

                        if (stepProperty is StepProperty.SingleStepProperty ssp)
                        {
                            Format1(sb, ssp.Step, false,usedComments);
                        }
                        else if (stepProperty is StepProperty.LambdaFunctionProperty lfp)
                        {
                            sb.Append("(");
                            if(lfp.LambdaFunction.Variable is null)
                                sb.Append("<>");
                            else sb.Append(lfp.LambdaFunction.Variable.Value.Serialize(SerializeOptions.Serialize));
                            sb.Append(" => ");
                            Format1(sb, lfp.LambdaFunction.Step, false, usedComments);
                            sb.Append(")");
                        }
                        else
                        {
                            sb.Append(stepProperty.Serialize(SerializeOptions.Serialize));
                        }
                        if (allProperties.Count > 1)
                            sb.AppendLine();
                    }

                    if (!topLevel && compoundStep.ShouldBracketWhenSerialized)
                        sb.Append(")");
                    if (allProperties.Count > 1)
                        sb.UnIndent();

                    var comments1 = ReadComments(step);
                    foreach (var commentToken in comments1)
                    {
                        if (usedComments.Add(commentToken.GetTokenKey()))
                        {
                            if(commentToken.Type == SCLLexer.DELIMITEDCOMMENT)
                                sb.AppendLine();
                            sb.Append(commentToken.Text);
                        }
                    }

                    return;
                }
            }
            //TODO entity constant

            //Default to basic serialization

            sb.Append(step.Serialize(SerializeOptions.Serialize));
            var comments2 = ReadComments(step);
            foreach (var commentToken in comments2)
            {
                if (usedComments.Add(commentToken.GetTokenKey()))
                {
                    if(commentToken.Type == SCLLexer.DELIMITEDCOMMENT)
                        sb.AppendLine();
                    sb.Append(commentToken.Text);
                }
            }
        }

        return sb.ToString();
    }

    private static IEnumerable<IToken> ReadComments(IStep step)
    {
        if(step.TextLocation is null)
            yield break;

        var inputStream       = new AntlrInputStream(step.TextLocation.Text);
        var lexer             = new SCLLexer(inputStream, TextWriter.Null, TextWriter.Null);

            

        foreach (var token in lexer.GetAllTokens())
        {
            if (token.Type is SCLLexer.DELIMITEDCOMMENT or SCLLexer.SINGLELINECOMMENT)
            {
                yield return token;
            }
        }
    }

    /// <summary>
    /// Get a unique key for a token
    /// </summary>
    public static (int type, int channel, int line, int column) GetTokenKey(this IToken token) => (token.Type, token.Channel, token.Line, token.Column);

    private class IndentationStringBuilder
    {
        public int Indentation { get; private set; } = 0;

        public void Indent() => Indentation++;
        public void UnIndent() => Indentation--;

        public void AppendLine(string line)
        {
            _current.Add(line);
            AppendLine();
        }

        public void AppendLine()
        {
            _lines.Add((_current, Indentation));
            _current = new List<string>();
        }

        public void Append(string text)
        {
            _current.Add(text);
        }

        private readonly List<(List<string> words, int indentation)> _lines = new();

        private List<string> _current = new();

        /// <inheritdoc />
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var (line, indentation) in _lines)
            {
                if (indentation > 0)
                    sb.Append(new string('\t', indentation));
                sb.AppendJoin("", line);
                sb.AppendLine();
            }

            if (_current.Any())
            {
                sb.Append(new string('\t', Indentation));
                foreach (var thing in _current)
                {
                    sb.Append(thing);
                }
            }

            return sb.ToString();
        }
    }
}