using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
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
            IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> stepFactoryStore,
            ILogger<RunSCLHandler> logger, ILanguageServerFacade languageServerFacade)
        {
            _documentManager = documentManager;
            _stepFactoryStore = stepFactoryStore;
            Logger = logger;
            LanguageServerFacade = languageServerFacade;
        }

        public ILogger<RunSCLHandler> Logger { get; }
        public ILanguageServerFacade LanguageServerFacade { get; }

        private readonly DocumentManager _documentManager;

        private readonly IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>
            _stepFactoryStore;

        /// <inheritdoc />
        public async Task<RunResult> Handle(RunSCLParams request, CancellationToken cancellationToken)
        {
            var document = _documentManager.GetDocument(request.TextDocument.Uri);

            if (document is null)
                return new RunResult()
                {
                    Success = false,
                    Message = "Could not get document"
                };

            var sfs = await _stepFactoryStore.GetValueAsync();

            var runner = new SCLRunner(Logger, sfs.stepFactoryStore, sfs.externalContext);

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