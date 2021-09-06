using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Antlr4.Runtime;
using CSharpFunctionalExtensions;
using LanguageServer.Visitors;
using NuGet.Packaging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace LanguageServer
{
    public record SCLDocument(string Text, DocumentUri DocumentUri)
    {
        public Hover GetHover(Position position, StepFactoryStore stepFactoryStore)
        {
            var lazyTypeResolver = HoverVisitor.CreateLazyTypeResolver(Text, stepFactoryStore);

            var command = Helpers.GetCommand(Text, position);

            if (command is null) return new Hover();

            var visitor2 = new HoverVisitor(command.Value.newPosition, command.Value.positionOffset, stepFactoryStore, lazyTypeResolver);

            var errorListener = new ErrorErrorListener();

            var hover = visitor2.LexParseAndVisit(command.Value.command, x => { x.RemoveErrorListeners(); },
                x =>
                {
                    x.RemoveErrorListeners();
                    x.AddErrorListener(errorListener);
                });
            if (hover is not null) return hover;

            if (errorListener.Errors.Any())
            {
                var error = errorListener.Errors.First();
                var errorHover = new Hover()
                {
                    Range = error.Location.TextLocation?.GetRange(command.Value.newPosition.Line,
                        command.Value.newPosition.Character),
                    Contents = new MarkedStringsOrMarkupContent(error.Message)
                };
                return errorHover;
            }

            return hover ?? new Hover();
        }

        public SignatureHelp? GetSignatureHelp(Position position, StepFactoryStore stepFactoryStore)
        {
            var visitor = new SignatureHelpVisitor(position, stepFactoryStore);
            var signatureHelp = visitor.LexParseAndVisit(Text, x => { x.RemoveErrorListeners(); },
                x => { x.RemoveErrorListeners(); });

            return signatureHelp;
        }

        public List<TextEdit> FormatDocument(StepFactoryStore stepFactoryStore)
        {
            var commands = Helpers.SplitIntoCommands(Text);

            var typeResolver = HoverVisitor.CreateLazyTypeResolver(Text, stepFactoryStore).Value;

            var textEdits = new List<TextEdit>();

            var commandCallerMetadata = new CallerMetadata("Command", "", TypeReference.Any.Instance);

            foreach (var (command, offset) in commands)
            {
                var stepParseResult = SCLParsing.TryParseStep(command);

                if (stepParseResult.IsSuccess)
                {
                    Result<IStep, IError> freezeResult;

                    if (typeResolver.IsSuccess)
                    {
                        freezeResult = stepParseResult.Value.TryFreeze(commandCallerMetadata, typeResolver.Value);
                    }
                    else
                    {
                        freezeResult = stepParseResult.Value.TryFreeze(commandCallerMetadata, stepFactoryStore);
                    }

                    if (freezeResult.IsSuccess)
                    {
                        var text = Formatter.Format(freezeResult.Value).Trim();// freezeResult.Value.Serialize().Trim();

                        var range = freezeResult.Value.TextLocation?.GetRange(offset.Line, offset.Character)!;
                        var realRange = new Range(offset, new Position(range.End.Line, range.End.Character + 1)); //Need to end one character later

                        textEdits.Add(new TextEdit()
                        {
                            NewText = text,
                            Range = realRange
                        });
                    }
                }
            }

            return textEdits;
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

            var completionList = visitor.LexParseAndVisit(Text, x => { x.RemoveErrorListeners(); },
                x => { x.RemoveErrorListeners(); });

            if (completionList is not null)
                return completionList;

            var command = Helpers.GetCommand(Text, position);

            if (command is not null)
            {
                visitor = new CompletionVisitor(command.Value.newPosition, stepFactoryStore);

                var lineCompletionList = visitor.LexParseAndVisit(command.Value.command,
                    x => { x.RemoveErrorListeners(); },
                    x => { x.RemoveErrorListeners(); });

                if (lineCompletionList is not null)
                    return lineCompletionList;

                var textWithoutToken = Helpers.RemoveToken(command.Value.command, command.Value.newPosition);

                var withoutTokenCompletionList = visitor.LexParseAndVisit(textWithoutToken,
                    x => { x.RemoveErrorListeners(); }, x => { x.RemoveErrorListeners(); });

                if (withoutTokenCompletionList is not null)
                    return withoutTokenCompletionList;
            }


            return new CompletionList(); //Give up
        }

        public PublishDiagnosticsParams GetDiagnostics(StepFactoryStore stepFactoryStore)
        {
            IList<Diagnostic> diagnostics;

            var initialParseResult = SCLParsing.TryParseStep(Text);

            if (initialParseResult.IsSuccess)
            {
                var freezeResult = initialParseResult.Value.TryFreeze(SCLRunner.RootCallerMetadata, stepFactoryStore);

                if (freezeResult.IsSuccess)
                {
                    diagnostics = ImmutableList<Diagnostic>.Empty;
                }

                else
                {
                    diagnostics = freezeResult.Error.GetAllErrors().Select(x => ToDiagnostic(x, new Position(0, 0)))
                        .WhereNotNull().ToList();
                }
            }
            else
            {
                var commands = Helpers.SplitIntoCommands(Text);
                diagnostics = new List<Diagnostic>();
                foreach (var (commandText, commandPosition) in commands)
                {
                    var visitor = new DiagnosticsVisitor();
                    var listener = new ErrorErrorListener();
                    var parseResult = visitor.LexParseAndVisit(commandText, _ => { },
                        x => { x.AddErrorListener(listener); });

                    IList<Diagnostic> newDiagnostics = listener.Errors.Select(x => ToDiagnostic(x, commandPosition))
                        .WhereNotNull().ToList();

                    if (!newDiagnostics.Any())
                        newDiagnostics = parseResult.Select(x => ToDiagnostic(x, commandPosition)).WhereNotNull()
                            .ToList();
                    diagnostics.AddRange(newDiagnostics);
                }
            }


            return new PublishDiagnosticsParams
            {
                Diagnostics = new Container<Diagnostic>(diagnostics),
                Uri = DocumentUri
            };

            static Diagnostic? ToDiagnostic(SingleError error, Position positionOffset)
            {
                if (error.Location.TextLocation is null) return null;

                return
                    new Diagnostic()
                    {
                        Range = error.Location.TextLocation.GetRange(positionOffset.Line, positionOffset.Character),
                        Severity = DiagnosticSeverity.Error,
                        Source = "SCL",
                        Message = error.Message
                    };
            }
        }
    }
}