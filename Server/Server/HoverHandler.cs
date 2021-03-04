using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.Core.Internal;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Server
{
    internal class HoverHandler : IHoverHandler
    {
        private readonly ILanguageServerConfiguration _configuration;
        private readonly ILogger<HoverHandler> _logger;
        private readonly DocumentManager _documentManager;
        private readonly StepFactoryStore _stepFactoryStore;


        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        public HoverHandler(ILanguageServerConfiguration configuration, ILogger<HoverHandler> logger,
            DocumentManager documentManager, StepFactoryStore stepFactoryStore)
        {
            _configuration = configuration;
            _logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
        }

        /// <inheritdoc />
        public async Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _logger.LogWarning($"Hover Position: {request.Position} Document: {request.TextDocument}");

            var documentPath = request.TextDocument.Uri.ToString();
            var document = _documentManager.GetDocument(documentPath);

            if (document == null)
            {
                _logger.LogWarning($"Document not found: {request.TextDocument.Uri}");
                return new Hover();
            }

            var hover = document.GetHover(request.Position, _stepFactoryStore);

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