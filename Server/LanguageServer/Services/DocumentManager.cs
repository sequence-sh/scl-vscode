using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services
{
    internal class DocumentManager
    {
        private readonly ConcurrentDictionary<string, SCLDocument> _documents = new();

        private readonly ILogger<DocumentManager> _logger;

        private readonly ILanguageServerFacade _facade;

        private readonly IAsyncFactory<StepFactoryStore> _stepFactoryStore;
        

        public DocumentManager(ILanguageServerFacade facade,
            ILogger<DocumentManager> logger,
            IAsyncFactory<StepFactoryStore> stepFactoryStore)
        {
            _facade = facade;
            _logger = logger;
            _stepFactoryStore = stepFactoryStore;
        }

        public void RemoveDocument(DocumentUri documentUri)
        {
            _documents.Remove(documentUri.ToString(), out var _);
        }

        public async Task UpdateDocumentAsync( SCLDocument document)
        {
            _documents.AddOrUpdate(document.DocumentUri.ToString(), document, (_, _) => document);

            var sfs = await _stepFactoryStore.GetValueAsync();


            var diagnostics =document.GetDiagnostics(sfs);

            var diagnosticCount = diagnostics.Diagnostics?.Count() ?? 0;

            _logger.LogDebug($"Publishing {diagnosticCount} diagnostics for {document.DocumentUri}");

            _facade.TextDocument.PublishDiagnostics(diagnostics);


        }

        public SCLDocument? GetDocument(DocumentUri documentPath)
        {
            var result =  _documents.TryGetValue(documentPath.ToString(), out var document) ? document : null;

            if (result is null) _logger.LogWarning($"Document not found: {documentPath}");

            return result;
        }
    }
}
