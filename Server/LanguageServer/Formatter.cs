using System.Collections.Generic;
using System.Linq;
using System.Text;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Steps;

namespace LanguageServer
{
    public static class Formatter
    {
        //public static Result<string, IError> Format(IFreezableStep step, StepFactoryStore stepFactoryStore)
        //{
        //    var sb = new IndentationStringBuilder();
        //    var errors = new List<IError>();
        //    Format1(sb, step);

        //    void Format1(IndentationStringBuilder sb, IFreezableStep step)
        //    {
        //        if (step  is CompoundFreezableStep compoundStep)
        //        {
        //            var sfs = compoundStep.TryGetStepFactory(stepFactoryStore);
        //            if (compoundStep.TryGetStepFactory(s) .StepFactory.Serializer is FunctionSerializer)
        //            {
        //            }
        //        }
        //        //TODO entity constant

        //        //Default to basic serialization
        //        var freezeResult = step.TryFreeze()


        //        sb.WriteLine(step..Serialize());
        //    }

        //    if (errors.Any())
        //        return Result.Failure<string, IError>(ErrorList.Combine(errors));

        //    return sb.ToString();
        //}

        public static string Format(IStep step)
        {
            var sb = new IndentationStringBuilder();
            var errors = new List<IError>();
            Format1(sb, step, true);

            static void Format1(IndentationStringBuilder sb, IStep step, bool topLevel)
            {
                step.TextLocation.
                if (step is ICompoundStep compoundStep)
                {
                    if (compoundStep is ISequenceStep sequenceStep)
                    {
                        foreach (var seqStep in sequenceStep.AllSteps)
                        {
                            sb.AppendLine();
                            sb.Append("- ");
                            Format1(sb, seqStep, true);
                        }

                        return;
                    }

                    if (compoundStep.StepFactory.Serializer is FunctionSerializer)
                    {
                        var allProperties =
                            ((IEnumerable<StepProperty>)compoundStep.GetType().GetProperty("AllProperties")
                                .GetValue(compoundStep)).ToList();

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
                                Format1(sb, ssp.Step, false);
                            }
                            else if (stepProperty is StepProperty.LambdaFunctionProperty lfp)
                            {
                                sb.Append("(");
                                if(lfp.LambdaFunction.Variable is null)
                                    sb.Append("<>");
                                else sb.Append(lfp.LambdaFunction.Variable.Value.Serialize());
                                sb.Append(" => ");
                                Format1(sb, lfp.LambdaFunction.Step, false);
                                sb.Append(")");
                            }
                            else
                            {
                                sb.Append(stepProperty.Serialize());
                            }
                            if (allProperties.Count > 1)
                                sb.AppendLine();
                        }

                        if (!topLevel && compoundStep.ShouldBracketWhenSerialized)
                            sb.Append(")");
                        if (allProperties.Count > 1)
                            sb.UnIndent();
                        return;
                    }
                }
                //TODO entity constant

                //Default to basic serialization


                sb.Append(step.Serialize());
            }

            return sb.ToString();
        }


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
}