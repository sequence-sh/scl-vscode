using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Serialization;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Serialization;

namespace LanguageServer.Services
{



    internal class RunSCLHandler : IRunSCLHandler
    {
        /// <summary>
        /// Create a new RunSCLHandler
        /// </summary>
        public RunSCLHandler(DocumentManager documentManager,
            IAsyncFactory<(StepFactoryStore stepFactoryStore,
                IExternalContext externalContext)> stepFactoryStore,
            ILanguageServerFacade languageServerFacade)
        {
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
            LanguageServerFacade = languageServerFacade;
        }
        
        public ILanguageServerFacade LanguageServerFacade { get; }
        //public ILogger<RunSCLHandler> Logger { get; }

        private readonly DocumentManager _documentManager;

        private readonly IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>
            _stepFactoryStore;

        /// <inheritdoc />
        public async Task<RunResult> Handle(RunSCLParams request, CancellationToken cancellationToken)
        {
            var factory = new NLogLoggerFactory(new NLogLoggerProvider(new NLogProviderOptions()
            {

            },
                LogManager.LogFactory));

            var logger = factory.CreateLogger<RunSCLHandler>();
            
            //var nlogFactory = new NLog.LogFactory(new NLogLoggingConfiguration(Configuration.GetSection("nlog")));
            //ILogger logger =  new FacadeLogger(LanguageServerFacade);

            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document is null)
            {
                
                logger.LogError("Could not get document");

                return new RunResult()
                {
                    Success = false,
                    Message = "Could not get document"
                };

            }
                

            var (stepFactoryStore, externalContext) = await _stepFactoryStore.GetValueAsync();

            var runner = new SCLRunner(logger, stepFactoryStore, externalContext);

            var result =
                await runner.RunSequenceFromTextAsync(document.Text, new Dictionary<string, object>(),
                    cancellationToken);


            if (result.IsSuccess)
            {
                return new RunResult()
                {
                    Message = "Sequence Completed Successfully",
                    Success = true
                };
            }

            var diagnostics = new List<Diagnostic>();

            foreach (var error in result.Error.GetAllErrors())
            {
                if (error.Location.TextLocation is not null)
                {
                    var range = error.Location.TextLocation.GetRange(0, 0);
                    diagnostics.Add(new Diagnostic()
                    {
                        Severity = DiagnosticSeverity.Error,
                        Message = error.Message,
                        Range = range,
                        Code = error.ErrorBuilder.ErrorCode.Code,
                        Source = "SCL Language Server"
                    });
                }
            }

            if (diagnostics.Any())
            {
                LanguageServerFacade.TextDocument.PublishDiagnostics(
                    new PublishDiagnosticsParams()
                    {
                        Uri = request.TextDocument.Uri,
                        Diagnostics = new Container<Diagnostic>(diagnostics)
                    }
                );
            }

            logger.LogError("Sequence Failed");

            foreach (var error in result.Error.GetAllErrors())
            {
                logger.LogError(error.AsString);
                if(error.Location.TextLocation is not null)
                    logger.LogError(request.TextDocument.Uri.Path + ":" +
                                    error.Location.TextLocation.Start.Line);
            }


            return new RunResult()
            {
                Message = result.Error.AsStringWithLocation,
                Success = false
            };
        }

        /// <inheritdoc />
        public RunSCLRegistrationOptions GetRegistrationOptions(ClientCapabilities clientCapabilities)
        {
            return new RunSCLRegistrationOptions() { WorkDoneProgress = false };
        }

        /// <inheritdoc />
        public void SetCapability(RunSCLCapability capability, ClientCapabilities clientCapabilities)
        {
            RunSCLCapability = capability;
            ClientCapabilities = clientCapabilities;
        }

        public RunSCLCapability RunSCLCapability { get; set; } = null!;
        public ClientCapabilities ClientCapabilities { get; set; } = null!;
    }

    //internal class FacadeLogger : ILogger
    //{

    //    private readonly ILanguageServerFacade _responseRouter;

    //    public FacadeLogger(ILanguageServerFacade responseRouter)
    //    {
    //        _responseRouter = responseRouter;
    //    }

    //    /// <inheritdoc />
    //    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    //    {
    //        if (IsEnabled(logLevel))
    //        {
    //            var message = formatter.Invoke(state, exception);
                

    //            var messageType = logLevel switch
    //            {
    //                LogLevel.Trace => MessageType.Log,
    //                LogLevel.Debug => MessageType.Log,
    //                LogLevel.Information => MessageType.Info,
    //                LogLevel.Warning => MessageType.Warning,
    //                LogLevel.Error => MessageType.Error,
    //                LogLevel.Critical => MessageType.Error,
    //                LogLevel.None => MessageType.Log,
    //                _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
    //            };

    //            _responseRouter.Window.Log(new LogMessageParams(){Message = message, Type = messageType});
    //        }

    //    }

    //    /// <inheritdoc />
    //    public bool IsEnabled(LogLevel logLevel)
    //    {
    //        return logLevel >= LogLevel.Information;
    //    }

    //    /// <inheritdoc />
    //    public IDisposable BeginScope<TState>(TState state)
    //    {
    //        return new FakeDisposable();
    //    }

    //    private class FakeDisposable : IDisposable
    //    {
    //        /// <inheritdoc />
    //        public void Dispose()
    //        {
    //        }
    //    }
    //}


    //internal class ProgressManagerLogger : ILogger
    //{
    //    public ProgressManagerLogger(IProgressObserver<string> progressObserver)
    //    {
    //        ProgressObserver = progressObserver;
    //    }

    //    public IProgressObserver<string> ProgressObserver { get; }

    //    /// <inheritdoc />
    //    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    //    {
    //        if (logLevel >= LogLevel.Information)
    //        {
    //            var message = formatter.Invoke(state, exception);
    //            ProgressObserver.OnNext(message);
    //        }
    //    }

    //    /// <inheritdoc />
    //    public bool IsEnabled(LogLevel logLevel)
    //    {
    //        return logLevel >= LogLevel.Information;
    //    }

    //    /// <inheritdoc />
    //    public IDisposable BeginScope<TState>(TState state)
    //    {
    //        return new FakeDisposable();
    //    }

    //    private class FakeDisposable : IDisposable
    //    {
    //        /// <inheritdoc />
    //        public void Dispose()
    //        {
    //        }
    //    }
    //}


    [Parallel, Method("scl/runSCL")]
    internal class RunResult : IRequest
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }

    [Parallel]
    [Method("scl/runSCL", Direction.ClientToServer)]
    internal record RunSCLParams :
        ITextDocumentIdentifierParams,
        IWorkDoneProgressParams,
        IRequest<RunResult>
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public TextDocumentIdentifier TextDocument { get; init; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [Optional] public ProgressToken? WorkDoneToken { get; init; }

        [Optional] public ProgressToken? PartialResultToken { get; init; }
    }

    [Parallel, Method("scl/runSCL", Direction.ClientToServer)]
    internal interface IRunSCLHandler :
        IJsonRpcRequestHandler<RunSCLParams, RunResult>,
        IRegistration<RunSCLRegistrationOptions>,
        ICapability<RunSCLCapability>
    {
    }

    // each key is a json segment of the ClientCapabilities object
    [CapabilityKey("runscl")]
    internal class RunSCLCapability : DynamicCapability
    {
    }

    internal class RunSCLRegistrationOptions : IWorkDoneProgressOptions, IRegistrationOptions
    {
        [Optional] public bool WorkDoneProgress { get; set; }
    }
}