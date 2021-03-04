using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;

namespace Server
{
    public record SCLDocument(string Text)
    {
        public Hover GetHover(Position position, StepFactoryStore stepFactoryStore)
        {
            var hover = new HoverVisitor(position, stepFactoryStore).LexParseAndVisit(Text);

            return hover ?? new Hover();
        }

        public CompletionList GetCompletionList(Position position, StepFactoryStore stepFactoryStore)
        {
            var completionList = new CompletionVisitor(position, stepFactoryStore).LexParseAndVisit(Text);

            return completionList ?? new CompletionList();
        }
    }
}