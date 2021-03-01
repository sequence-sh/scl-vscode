using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Server
{
    internal class HoverHandler : IHoverHandler
    {
        private readonly ILanguageServerConfiguration _configuration;
        private readonly ILogger<HoverHandler> _logger;
        private readonly DocumentManager _documentManager;


        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        public HoverHandler(ILanguageServerConfiguration configuration, ILogger<HoverHandler> logger,
            DocumentManager documentManager)
        {
            _configuration = configuration;
            _logger = logger;
            _documentManager = documentManager;
        }

        /// <inheritdoc />
        public async Task<Hover> Handle(HoverParams request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            return new Hover()
            {
                Range = new Range(request.Position, request.Position),
                Contents = new MarkedStringsOrMarkupContent(new MarkedString("Hello Hello Hello"))
            };
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