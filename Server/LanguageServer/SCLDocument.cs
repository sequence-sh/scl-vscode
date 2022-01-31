

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