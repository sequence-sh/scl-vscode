

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
        var lazyTypeResolver =  HoverVisitor.CreateLazyTypeResolver(Text, stepFactoryStore);

        var command = Helpers.GetCommand(Text, position);

        if (command is null) return new Hover();

        var visitor2 = new HoverVisitor(command.Value.newPosition.ToLinePosition(),
            command.Value.positionOffset.ToLinePosition(), 
            stepFactoryStore,
            lazyTypeResolver);

        var errorListener = new ErrorErrorListener();

        var quickInfoResponse = visitor2.LexParseAndVisit(command.Value.command, x => { x.RemoveErrorListeners(); },
            x =>
            {
                x.RemoveErrorListeners();
                x.AddErrorListener(errorListener);
            });
        if (quickInfoResponse is not null) return quickInfoResponse.ToHover();

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

        return new Hover();
    }

    /// <summary>
    /// Get the signature help at a particular position
    /// </summary>
    public SignatureHelp? GetSignatureHelp(Position position, StepFactoryStore stepFactoryStore)
    {
        var visitor = new SignatureHelpVisitor(position.ToLinePosition(), stepFactoryStore);
        var signatureHelpResponse = visitor.LexParseAndVisit(Text, x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); });

        return signatureHelpResponse?.ToSignatureHelp();
    }

    /// <summary>
    /// Format an SCL document
    /// </summary>
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
        var visitor = new CompletionVisitor(position.ToLinePosition(), stepFactoryStore);

        var completionResponse = visitor.LexParseAndVisit(Text, x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); });

        if (completionResponse is not null)
            return completionResponse.ToCompletionList();

        var command = Helpers.GetCommand(Text, position);

        if (command is not null)
        {
            visitor = new CompletionVisitor(command.Value.newPosition.ToLinePosition(), stepFactoryStore);

            var lineCompletionResponse = visitor.LexParseAndVisit(command.Value.command,
                x => { x.RemoveErrorListeners(); },
                x => { x.RemoveErrorListeners(); });

            if (lineCompletionResponse is not null)
                return lineCompletionResponse.ToCompletionList();

            var textWithoutToken = Helpers.RemoveToken(command.Value.command, command.Value.newPosition);

            var withoutTokenResponse = visitor.LexParseAndVisit(textWithoutToken,
                x => { x.RemoveErrorListeners(); }, x => { x.RemoveErrorListeners(); });

            if (withoutTokenResponse is not null)
                return withoutTokenResponse.ToCompletionList();
        }


        return new CompletionList(); //Give up
    }

    /// <summary>
    /// Get diagnostics for this document
    /// </summary>
    public PublishDiagnosticsParams GetDiagnostics(StepFactoryStore stepFactoryStore)
    {

        var diags = DiagnosticsHelper.GetDiagnostics(Text, stepFactoryStore);

        return new PublishDiagnosticsParams
        {
            Diagnostics = diags.Select(ToDiagnostic).ToContainer(),
            Uri = DocumentUri
        };

        static Diagnostic ToDiagnostic(Reductech.Sequence.Core.LanguageServer.Objects.Diagnostic d1)
        {
            return new Diagnostic()
            {
                Message = d1.Message,
                Range = new Range(d1.Start.Line, d1.Start.Character, d1.End.Line, d1.End.Character)
            };
        }
    }
}