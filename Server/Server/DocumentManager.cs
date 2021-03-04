using System;
using System.Collections.Concurrent;

namespace Server
{
    internal class DocumentManager
    {
        private readonly ConcurrentDictionary<string, SCLDocument> _documents = new();

        public void UpdateDocument(string documentPath, SCLDocument document)
        {
            _documents.AddOrUpdate(documentPath, document, (_, _) => document);
        }

        public SCLDocument GetDocument(string documentPath)
        {
            return _documents.TryGetValue(documentPath, out var document) ? document : null;
        }
    }
}
