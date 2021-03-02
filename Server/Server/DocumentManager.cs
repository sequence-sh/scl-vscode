using System;
using System.Collections.Concurrent;

namespace Server
{
    internal record SCLDocument(string Text);

    internal class DocumentManager
    {
        private readonly ConcurrentDictionary<string, SCLDocument> _buffers = new();

        public void UpdateBuffer(string documentPath, SCLDocument buffer)
        {
            _buffers.AddOrUpdate(documentPath, buffer, (k, v) => buffer);
        }

        public SCLDocument GetBuffer(string documentPath)
        {
            return _buffers.TryGetValue(documentPath, out var buffer) ? buffer : null;
        }
    }
}
