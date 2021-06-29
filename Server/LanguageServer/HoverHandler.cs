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


    internal class HoverHandler : IHoverHandler
    {
        private readonly ILanguageServerConfiguration _configuration;
        private readonly ILogger<HoverHandler> _logger;
        private readonly DocumentManager _documentManager;
        private readonly AsyncLazy<StepFactoryStore>  _stepFactoryStore;


        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        public HoverHandler(ILanguageServerConfiguration configuration, ILogger<HoverHandler> logger,
            DocumentManager documentManager, IConnectorManager connectorManager)
        {
            _configuration = configuration;
            _logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore =
                new AsyncLazy<StepFactoryStore>(() =>
                    connectorManager.GetStepFactoryStoreAsync(CancellationToken.None));
        }

        /// <inheritdoc />
        public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _logger.LogDebug($"Hover Position: {request.Position} Document: {request.TextDocument}");

            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document == null)
            {
                _logger.LogWarning($"Document not found: {request.TextDocument.Uri}");
                return new Hover();
            }

            var sfs = await _stepFactoryStore.Value;

            var hover = document.GetHover(request.Position, sfs);

            _logger.LogDebug($"Hover: {hover.Contents}");

            return hover;
        }


        /// <inheritdoc />
        public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector
            };
        }
    }
}