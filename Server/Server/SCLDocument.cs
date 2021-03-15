using System.Linq;
using CSharpFunctionalExtensions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Util;

namespace Server
{
    public record SCLDocument(string Text, DocumentUri DocumentUri)
    {
        public Hover GetHover(Position position, StepFactoryStore stepFactoryStore)
        {
            var hover = new HoverVisitor(position, stepFactoryStore).LexParseAndVisit(Text);

            return hover ?? new Hover();
        }

        public CompletionList GetCompletionList(Position position, StepFactoryStore stepFactoryStore)
        {
            var completionList = new CompletionVisitor(position, stepFactoryStore).LexParseAndVisit(Text);

            return completionList ?? new CompletionList();
        }

        public PublishDiagnosticsParams GetDiagnostics(StepFactoryStore stepFactoryStore)
        {
            var result = SCLParsing.ParseSequence(Text).Bind(x=>x.TryFreeze(TypeReference.Any.Instance,  stepFactoryStore));

            if (result.IsSuccess)
            {
                return new PublishDiagnosticsParams()
                {
                    Diagnostics = new Container<Diagnostic>(),
                    Uri = DocumentUri
                };
            }


            var diagnostics = result.Error.GetAllErrors().Select(ToDiagnostic).WhereNotNull().ToList();

            return new PublishDiagnosticsParams()
            {
                Diagnostics = new Container<Diagnostic>(diagnostics),
                Uri = DocumentUri
            };

            static Diagnostic? ToDiagnostic(SingleError error)
            {
                if (error.Location.TextLocation is null) return null;

                return
                    new Diagnostic()
                    {
                        Range = error.Location.TextLocation.GetRange(),
                        Severity = DiagnosticSeverity.Error,
                        Source = "SCL",
                        Message = error.Message
                    };
            }
        }
    }
}