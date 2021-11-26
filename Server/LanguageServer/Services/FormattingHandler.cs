using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services
{
    internal class FormattingHandler : IDocumentFormattingHandler
    {
        private readonly ILogger<FormattingHandler> _logger;
        private readonly DocumentManager _documentManager;
        private readonly IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>  _stepFactoryStore;

        public FormattingHandler(ILogger<FormattingHandler> logger, DocumentManager documentManager, IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> stepFactoryStore)
        {
            _logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
        }

        /// <inheritdoc />
        public async Task<TextEditContainer?> Handle(DocumentFormattingParams request, CancellationToken cancellationToken)
        {
            _logger.LogDebug($"Handling Document Formatting Request");

            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document == null)
            {
                return null;
            }

            var sfs = await _stepFactoryStore.GetValueAsync();

            var formatting = document.FormatDocument(sfs.stepFactoryStore);

            return formatting;
        }

        /// <inheritdoc />
        public DocumentFormattingRegistrationOptions GetRegistrationOptions(DocumentFormattingCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new DocumentFormattingRegistrationOptions
            {
                DocumentSelector = TextDocumentSyncHandler.DocumentSelector,
                WorkDoneProgress = false
            };
        }
    }
}