using NLog;
using NLog.Targets;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace LanguageServer.Logging
{
    /// <summary>
    /// NLog Target that writes to the output window
    /// </summary>
    [Target("OutputWindow")]
    public sealed class OutputWindowTarget : TargetWithLayout
    {
        /// <inheritdoc />
        protected override void Write(LogEventInfo logEvent)
        {

            var messageType = logEvent.Level.Name switch
            {
                nameof(LogLevel.Trace) => MessageType.Log,
                nameof(LogLevel.Debug) => MessageType.Log,
                nameof(LogLevel.Info) => MessageType.Info,
                nameof(LogLevel.Warn) => MessageType.Warning,
                nameof(LogLevel.Error) => MessageType.Error,
                nameof(LogLevel.Fatal) => MessageType.Error,
                nameof(LogLevel.Off) => MessageType.Log,
                _ => MessageType.Log,
            };

            //ugly static object
            Program.LanguageServerFacade.Window.Log(new LogMessageParams {Message = logEvent.FormattedMessage, Type = messageType});
        }
    }
}
