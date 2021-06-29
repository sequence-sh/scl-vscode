using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core.Connectors;
using Reductech.EDR.Core.Internal;

namespace LanguageServer
{
    internal class CompletionHandler : ICompletionHandler
    {
        public ILogger<CompletionHandler> Logger { get; }

        private readonly ILanguageServerConfiguration _configuration;
        private readonly DocumentManager _documentManager;

        private readonly AsyncLazy<StepFactoryStore> _stepFactoryStore;

        private readonly DocumentSelector _documentSelector = new (
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        private CompletionCapability _capability;

        public CompletionHandler(ILanguageServerConfiguration configuration,
            ILogger<CompletionHandler> logger,
            DocumentManager documentManager,
            IConnectorManager connectorManager)
        {
            Logger = logger;
            _configuration = configuration;
            _documentManager = documentManager;
            _capability = new CompletionCapability();

            _stepFactoryStore =
                new AsyncLazy<StepFactoryStore>(() =>
                    connectorManager.GetStepFactoryStoreAsync(CancellationToken.None));
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

            Logger.LogDebug($"Completion Request Context: {request.Context} Position: {request.Position} Document: {request.TextDocument.Uri}");

            if (document == null)
            {
                Logger.LogWarning($"Document not found: {request.TextDocument.Uri}");
                return new CompletionList();
            }

            var sfs = await _stepFactoryStore;

            var cl = document.GetCompletionList(request.Position, sfs);

            Logger.LogDebug($"Completion Request returns {cl.Items.Count()} items ");

            return cl;
        }


        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ()
            {
                DocumentSelector = _documentSelector,

            };
        }
    }
}