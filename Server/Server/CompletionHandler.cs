using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;

namespace Server
{
    internal class CompletionHandler : ICompletionHandler
    {
        public ILogger<CompletionHandler> Logger { get; }

        private readonly ILanguageServerConfiguration _configuration;
        private readonly DocumentManager _documentManager;

        private readonly DocumentSelector _documentSelector = new (
            new DocumentFilter()
            {
                Pattern = "**/*.scl"
            }
        );

        private CompletionCapability _capability;

        public CompletionHandler(ILanguageServerConfiguration configuration, ILogger<CompletionHandler> logger, DocumentManager documentManager)
        {
            Logger = logger;
            _configuration = configuration;
            _documentManager = documentManager;
        }

        public CompletionRegistrationOptions GetRegistrationOptions()
        {
            return new CompletionRegistrationOptions
            {
                DocumentSelector = _documentSelector,
                ResolveProvider = false
            };
        }

        public async Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;

            var documentPath = request.TextDocument.Uri.ToString();
            var buffer = _documentManager.GetBuffer(documentPath);

            if (buffer == null)
            {
                return new CompletionList();
            }

            return new CompletionList(
                new CompletionItem
                {
                    Label = "Mark Is Cool",
                    Kind = CompletionItemKind.Reference,
                    TextEdit = new TextEdit
                    {
                        NewText = "Mark Is very Cool",
                        Range = new Range(
                            new Position
                            {
                                Line = request.Position.Line,
                                Character = request.Position.Character
                            }, new Position
                            {
                                Line = request.Position.Line,
                                Character = request.Position.Character + 5
                            })
                    }
                });
        }


        private static int GetPosition(string buffer, int line, int col)
        {
            var position = 0;
            for (var i = 0;
                i < line;
                i++)
            {
                position = buffer.IndexOf('\n', position) + 1;
            }

            return position + col;
        }

        public void SetCapability(CompletionCapability capability)
        {
            _capability = capability;
        }

        public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities)
        {
            return new ()
            {
                DocumentSelector = _documentSelector,

            };
        }
    }
}