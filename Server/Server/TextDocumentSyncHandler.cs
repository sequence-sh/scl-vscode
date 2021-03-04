using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace Server
{
    internal class TextDocumentSyncHandler : ITextDocumentSyncHandler
    {
        public ILogger<TextDocumentSyncHandler> Logger { get; }
        private readonly ILanguageServerConfiguration _configuration;
        private readonly DocumentManager _documentManager;

        private readonly DocumentSelector _documentSelector = new(
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        private SynchronizationCapability _capability;

        public TextDocumentSyncHandler(ILanguageServerConfiguration configuration, ILogger<TextDocumentSyncHandler> logger, DocumentManager documentManager)
        {
            Logger = logger;
            _configuration = configuration;
            _documentManager = documentManager;
        }

        public TextDocumentSyncKind Change { get; } = TextDocumentSyncKind.Full;

        public TextDocumentChangeRegistrationOptions GetRegistrationOptions()
        {
            return new()
            {
                DocumentSelector = _documentSelector,
                SyncKind = Change
            };
        }

        public TextDocumentAttributes GetTextDocumentAttributes(Uri uri)
        {
            return new(uri, "scl");
        }

        public Task<Unit> Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
        {
            var documentPath = request.TextDocument.Uri.ToString();
            var text = request.ContentChanges.FirstOrDefault()?.Text;


            _documentManager.UpdateDocument(documentPath, new SCLDocument(text));
            Logger.LogWarning($"Updated buffer for document: {documentPath}");

            return Unit.Task;
        }

        public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
        {
            _documentManager.UpdateDocument(request.TextDocument.Uri.ToString(), new SCLDocument(request.TextDocument.Text));
            return Unit.Task;
        }

        public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
        {
            return Unit.Task;
        }

        public void SetCapability(SynchronizationCapability capability)
        {
            _capability = capability;
        }

        /// <inheritdoc />
        public TextDocumentChangeRegistrationOptions GetRegistrationOptions(SynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector,
                SyncKind = TextDocumentSyncKind.Full
            };
        }


        /// <inheritdoc />
        TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector,
            };
        }

        /// <inheritdoc />
        TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector
            };
        }

        /// <inheritdoc />
        TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, SynchronizationCapability>.GetRegistrationOptions(SynchronizationCapability capability,
            ClientCapabilities clientCapabilities)
        {
            return new()
            {
                DocumentSelector = _documentSelector,
                IncludeText = true
            };
        }

        /// <inheritdoc />
        public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        {
            return new (uri, "SCL");
        }
    }
}