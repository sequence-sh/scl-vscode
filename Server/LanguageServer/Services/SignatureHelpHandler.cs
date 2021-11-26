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
    internal class SignatureHelpHandler : ISignatureHelpHandler
    {

        private readonly ILogger<SignatureHelpHandler> _logger;
        private readonly DocumentManager _documentManager;
        private readonly IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>  _stepFactoryStore;

        public SignatureHelpHandler(ILogger<SignatureHelpHandler> logger, DocumentManager documentManager, IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> stepFactoryStore)
        {
            _logger = logger;
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
        }

        /// <inheritdoc />
        public async Task<SignatureHelp?> Handle(SignatureHelpParams request, CancellationToken cancellationToken)
        {

            _logger.LogDebug($"Signature Help Position: {request.Position} Document: {request.TextDocument}");

            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document == null)
            {
                return null;
            }

            var sfs = await _stepFactoryStore.GetValueAsync();

            var signatureHelp = document.GetSignatureHelp(request.Position, sfs.stepFactoryStore);

            _logger.LogDebug($"Signature Help: {signatureHelp}");

            return signatureHelp;
        }

        /// <inheritdoc />
        public SignatureHelpRegistrationOptions GetRegistrationOptions(SignatureHelpCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new ()
            {
                WorkDoneProgress = false,
                TriggerCharacters = new Container<string>(" "),
                DocumentSelector = TextDocumentSyncHandler.DocumentSelector
            };
        }
    }
}