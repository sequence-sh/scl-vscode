using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.Services;

internal class RenameHandler : IRenameHandler
{
    private readonly ILogger<RenameHandler> _logger;
    private readonly DocumentManager _documentManager;

    public RenameHandler(
        ILogger<RenameHandler> logger,
        DocumentManager documentManager)
    {
        _logger = logger;
        _documentManager = documentManager;
    }


    /// <inheritdoc />
    public async Task<WorkspaceEdit?> Handle(RenameParams request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var document = _documentManager.GetDocument(request.TextDocument.Uri);

        if (document is null) return null;

        var textEdits = document.RenameVariable(request.Position, request.NewName);

        _logger.LogInformation($"Renamed {request.Position} to '{request.NewName}' in {textEdits.Count} places.");

        return new WorkspaceEdit
        {
            Changes = new Dictionary<DocumentUri, IEnumerable<TextEdit>>()
            {
                {request.TextDocument.Uri, textEdits}
            },
        };
    }


    /// <inheritdoc />
    public RenameRegistrationOptions GetRegistrationOptions(RenameCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new()
        {
            DocumentSelector = new DocumentSelector(
                DocumentFilter.ForLanguage("scl"),
                DocumentFilter.ForPattern("<*>")),
            WorkDoneProgress = false
        };
    }
}