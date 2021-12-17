using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services;

internal class CompletionHandler : ICompletionHandler
{
    public ILogger<CompletionHandler> Logger { get; }

    private readonly DocumentManager _documentManager;

    private readonly IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> _stepFactoryStore;

    public CompletionHandler(
        ILogger<CompletionHandler> logger,
        DocumentManager documentManager, IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> stepFactoryStore)
    {
        Logger = logger;
        _documentManager = documentManager;
        _stepFactoryStore = stepFactoryStore;
    }

    public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var document = _documentManager.GetDocument(request.TextDocument.Uri);
        Logger.LogInformation(
            $"Completion Request Context: {request.Context} Position: {request.Position} Document: {request.TextDocument.Uri}");

        if (document == null)
        {
            return new CompletionList();
        }

        var (stepFactoryStore, _) = await _stepFactoryStore.GetValueAsync();
        var cl = document.GetCompletionList(request.Position, stepFactoryStore);

        Logger.LogInformation($"Completion Request returns {cl.Items.Count()} items ");

        return cl;
    }


    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new()
        {
            DocumentSelector = TextDocumentSyncHandler.DocumentSelector,
        };
    }
}