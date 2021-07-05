using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services
{
    internal class HoverHandler : IHoverHandler
    {
        private readonly ILogger<HoverHandler> _logger;
        private readonly DocumentManager _documentManager;
        private readonly IAsyncFactory<StepFactoryStore>  _stepFactoryStore;
        

        public HoverHandler(ILogger<HoverHandler> logger,
            DocumentManager documentManager, IAsyncFactory<StepFactoryStore> stepFactoryStore)
        {
            _logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
        }

        /// <inheritdoc />
        public async Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Hover Position: {request.Position} Document: {request.TextDocument}");

            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document == null)
            {
                return new Hover();
            }

            var sfs = await _stepFactoryStore.GetValueAsync();

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
                DocumentSelector = TextDocumentSyncHandler.DocumentSelector
            };
        }
    }
}