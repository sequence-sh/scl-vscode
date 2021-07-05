using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using CSharpFunctionalExtensions;
using LanguageServer.Visitors;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;

namespace LanguageServer
{
    public record SCLDocument(string Text, DocumentUri DocumentUri)
    {
        public Hover GetHover(Position position, StepFactoryStore stepFactoryStore)
        {
            var visitor = new HoverVisitor(position, stepFactoryStore, Text);
            var hover = visitor.LexParseAndVisit(Text);

            return hover ?? new Hover();
        }

        public SignatureHelp? GetSignatureHelp(Position position, StepFactoryStore stepFactoryStore)
        {
            var visitor = new SignatureHelpVisitor(position, stepFactoryStore);
            var signatureHelp = visitor.LexParseAndVisit(Text);

            return signatureHelp;
        }

        public List<TextEdit> RenameVariable(Position position, string newName)
        {
            var inputStream = new AntlrInputStream(Text);
            var lexer = new SCLLexer(inputStream);

            var tokenAtPosition = lexer.GetAllTokens().FirstOrDefault(x => x.ContainsPosition(position));

            var edits = new List<TextEdit>();

            if (tokenAtPosition is null)
                return edits;

            var newText = $"<{newName.TrimStart('<').TrimEnd('>')}>";

            var variableNameTokenType = lexer.TokenTypeMap["VARIABLENAME"];

            if (tokenAtPosition.Type == variableNameTokenType)
            {
                lexer.Reset(); //back to the start

                foreach (var matchingToken in lexer.GetAllTokens().Where(x =>
                    string.Equals(x.Text, tokenAtPosition.Text, StringComparison.OrdinalIgnoreCase)))
                {
                    edits.Add(new TextEdit()
                    {
                        Range = matchingToken.GetRange(),
                        NewText = newText
                    });
                }
            }

            return edits;
        }

        public CompletionList GetCompletionList(Position position, StepFactoryStore stepFactoryStore)
        {
            var visitor = new CompletionVisitor(position, stepFactoryStore);

            var completionList = visitor.LexParseAndVisit(Text);

            if (completionList is not null)
                return completionList;

            var (line, linePosition) = Helpers.GetLine(Text, position);

            visitor = new CompletionVisitor(linePosition, stepFactoryStore);
            
            var lineCompletionList = visitor.LexParseAndVisit(line);

            if (lineCompletionList is not null)
                return lineCompletionList;

            var textWithoutToken = Helpers.RemoveToken(line, linePosition);

            var withoutTokenCompletionList = visitor.LexParseAndVisit(textWithoutToken);

            if (withoutTokenCompletionList is not null)
                return withoutTokenCompletionList;

            return new CompletionList(); //Give up
        }

        public PublishDiagnosticsParams GetDiagnostics(StepFactoryStore stepFactoryStore)
        {
            var result = SCLParsing.TryParseStep(Text)
                .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, stepFactoryStore));

            if (result.IsSuccess)
            {
                return new PublishDiagnosticsParams
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