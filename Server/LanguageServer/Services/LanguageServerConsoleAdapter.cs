using System.IO;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Window;
using Sequence.Core.Abstractions;

namespace LanguageServer.Services;

/// <summary>
/// Console adapter for the language server.
/// </summary>
public class LanguageServerConsoleAdapter : IConsole
{
    /// <summary>
    /// Create a new Language Server console adapter
    /// </summary>
    /// <param name="languageServerFacade"></param>
    public LanguageServerConsoleAdapter(ILanguageServerFacade languageServerFacade)
    {
        LanguageServerFacade = languageServerFacade;
    }

    private ILanguageServerFacade LanguageServerFacade { get; }

    /// <inheritdoc />
    public void WriteLine(string? value)
    {
        if (value is not null)
        {
            LanguageServerFacade.Window.LogMessage(new LogMessageParams(){Message = value, Type = MessageType.Info});
            //These messages will show up in the SCL Language Server window
        }
            
    }

    /// <inheritdoc />
    public Stream OpenStandardInput()
    {
        return Stream.Null;
    }

    /// <inheritdoc />
    public Stream OpenStandardOutput()
    {
        return Stream.Null;
    }

    /// <inheritdoc />
    public Stream OpenStandardError()
    {
        return Stream.Null;
    }
}