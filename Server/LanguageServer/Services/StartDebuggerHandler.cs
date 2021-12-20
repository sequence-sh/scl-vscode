using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;

namespace LanguageServer.Services;

internal class StartDebuggerHandler : IStartDebuggerHandler
{
    /// <inheritdoc />
    public async Task<StartDebuggerResult> Handle(StartDebuggerParams request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        Debugger.Launch();

        return new StartDebuggerResult();
    }

    /// <inheritdoc />
    public StartDebuggerRegistrationOptions GetRegistrationOptions(ClientCapabilities clientCapabilities)
    {
        return new StartDebuggerRegistrationOptions() { WorkDoneProgress = false };
    }

    /// <inheritdoc />
    public void SetCapability(StartDebuggerCapability capability, ClientCapabilities clientCapabilities)
    {
        StartDebuggerCapability = capability;
        ClientCapabilities = clientCapabilities;
    }

    public StartDebuggerCapability StartDebuggerCapability { get; set; } = null!;
    public ClientCapabilities ClientCapabilities { get; set; } = null!;
}


[Parallel, Method("scl/StartDebugger")]
internal class StartDebuggerResult : IRequest
{
}

[Parallel]
[Method("scl/StartDebugger", Direction.ClientToServer)]
internal record StartDebuggerParams :
    IRequest<StartDebuggerResult>
{
}

[Parallel, Method("scl/StartDebugger", Direction.ClientToServer)]
internal interface IStartDebuggerHandler :
    IJsonRpcRequestHandler<StartDebuggerParams, StartDebuggerResult>,
    IRegistration<StartDebuggerRegistrationOptions>,
    ICapability<StartDebuggerCapability>
{
}

// each key is a json segment of the ClientCapabilities object
[CapabilityKey("StartDebugger")]
internal class StartDebuggerCapability : DynamicCapability
{
}

internal class StartDebuggerRegistrationOptions : IWorkDoneProgressOptions, IRegistrationOptions
{
    [Optional] public bool WorkDoneProgress { get; set; }
}