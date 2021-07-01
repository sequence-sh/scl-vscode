using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;

namespace LanguageServer
{
    internal class CompletionHandler : ICompletionHandler
    {
        public ILogger<CompletionHandler> Logger { get; }

        private readonly DocumentManager _documentManager;

        private readonly IAsyncFactory<StepFactoryStore> _stepFactoryStore;

        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        private CompletionCapability _capability;

        public CompletionHandler(
            ILogger<CompletionHandler> logger,
            DocumentManager documentManager, IAsyncFactory<StepFactoryStore> stepFactoryStore)
        {
            Logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
            _capability = new CompletionCapability();
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false
            };
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

            var sfs = await _stepFactoryStore.GetValueAsync();
            var cl = document.GetCompletionList(request.Position, sfs);

            Logger.LogInformation($"Completion Request returns {cl.Items.Count()} items ");

            return cl;
        }


        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector,
            };
        }
    }
}