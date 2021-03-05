using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.Core.Internal;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Server
{
    internal class DocumentManager
    {
        private readonly ConcurrentDictionary<string, SCLDocument> _documents = new();

        private readonly ILogger<DocumentManager> _logger;

        private readonly ILanguageServerFacade _facade;

        private readonly StepFactoryStore _stepFactoryStore;

        public DocumentManager(ILanguageServerFacade facade, ILogger<DocumentManager> logger, StepFactoryStore stepFactoryStore)
        {
            _facade = facade;
            _logger = logger;
            _stepFactoryStore = stepFactoryStore;
        }

        public void RemoveDocument(DocumentUri documentUri)
        {
            _documents.Remove(documentUri.ToString(), out var _);
        }

        public void UpdateDocument( SCLDocument document)
        {
            _documents.AddOrUpdate(document.DocumentUri.ToString(), document, (_, _) => document);


            var diagnostics =document.GetDiagnostics(_stepFactoryStore);

            var diagnosticCount = diagnostics.Diagnostics?.Count() ?? 0;

            _logger.LogDebug($"Publishing {diagnosticCount} diagnostics for {document.DocumentUri}");

            _facade.TextDocument.PublishDiagnostics(diagnostics);


        }

        public SCLDocument? GetDocument(DocumentUri documentPath)
        {
            return _documents.TryGetValue(documentPath.ToString(), out var document) ? document : null;
        }
    }
}
