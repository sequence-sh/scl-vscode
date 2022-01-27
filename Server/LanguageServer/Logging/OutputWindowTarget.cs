using NLog.Targets;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;

namespace LanguageServer.Logging;

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
            nameof(NLog.LogLevel.Trace) => MessageType.Log,
            nameof(NLog.LogLevel.Debug) => MessageType.Log,
            nameof(NLog.LogLevel.Info) => MessageType.Info,
            nameof(NLog.LogLevel.Warn) => MessageType.Warning,
            nameof(NLog.LogLevel.Error) => MessageType.Error,
            nameof(NLog.LogLevel.Fatal) => MessageType.Error,
            nameof(NLog.LogLevel.Off) => MessageType.Log,
            _ => MessageType.Log,
        };

        //ugly static object
        Program.LanguageServerFacade.Window.Log(new LogMessageParams {Message = logEvent.FormattedMessage, Type = messageType});
    }
}