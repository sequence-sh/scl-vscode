using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace Server
{
    internal class HoverHandler : IHoverHandler
    {
        private readonly DocumentSelector _documentSelector = new DocumentSelector(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        private HoverCapability _capability;

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
        public TextDocumentRegistrationOptions GetRegistrationOptions()
        {
            return new ()
            {
                DocumentSelector = _documentSelector
            };
        }

        /// <inheritdoc />
        public void SetCapability(HoverCapability capability)
        {
            _capability = capability;
        }
    }
}