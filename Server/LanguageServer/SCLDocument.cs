using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Antlr4.Runtime;
using NuGet.Packaging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.Sequence.Core.Internal;
using Reductech.Sequence.Core.Internal.Errors;
using Reductech.Sequence.Core.Internal.Parser;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.LanguageServer;
using Reductech.Sequence.Core.Util;

namespace LanguageServer;

/// <summary>
/// A document containing SCL text
/// </summary>
public record SCLDocument(string Text, DocumentUri DocumentUri)
{
    /// <summary>
    /// Gets the hover at a particular position
    /// </summary>
    public Hover GetHover(Position position, StepFactoryStore stepFactoryStore)
    {
        var result = QuickInfoHelper.GetQuickInfoAsync(Text, position.ToLinePosition(), stepFactoryStore);
        return result.ToHover();
    }

    /// <summary>
    /// Get the signature help at a particular position
    /// </summary>
    public SignatureHelp? GetSignatureHelp(Position position, StepFactoryStore stepFactoryStore)
    {
        var response = SignatureHelpHelper.GetSignatureHelpResponse(Text, position.ToLinePosition(), stepFactoryStore);

        return response?.ToSignatureHelp();
    }

    /// <summary>
    /// Format an SCL document
    /// </summary>
    public List<TextEdit> FormatDocument(StepFactoryStore stepFactoryStore)
    {
        var result = 
        FormattingHelper.FormatSCL(Text, stepFactoryStore)
            .Select(x => x.ToTextEdit()).ToList();

        return result;
    }

    /// <summary>
    /// Rename a particular variable
    /// </summary>
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

    /// <summary>
    /// Get the Completion List from a particular position
    /// </summary>
    public CompletionList GetCompletionList(Position position, StepFactoryStore stepFactoryStore)
    {
        var result = CompletionHelper.GetCompletionResponse(Text, position.ToLinePosition(), stepFactoryStore);

        return result.ToCompletionList();
    }

    /// <summary>
    /// Get diagnostics for this document
    /// </summary>
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