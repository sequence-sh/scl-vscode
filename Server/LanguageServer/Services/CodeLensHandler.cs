using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace LanguageServer.Services;


//internal class CodeLensHandler : CodeLensHandlerBase
//{
//    /// <inheritdoc />
//    protected override CodeLensRegistrationOptions CreateRegistrationOptions(CodeLensCapability capability,
//        ClientCapabilities clientCapabilities)
//    {
//        return new CodeLensRegistrationOptions()
//        {
//            DocumentSelector = TextDocumentSyncHandler.DocumentSelector,
//            ResolveProvider = true
//        };
//    }

//    /// <inheritdoc />
//    public override async Task<CodeLensContainer> Handle(CodeLensParams request, CancellationToken cancellationToken)
//    {

//        var data = JToken.FromObject(request.TextDocument.Uri);

//        return new CodeLensContainer(new CodeLens()
//        {
//            Command = new Command(){Title = "Run SCL", Name = "reductech-scl.resolveCodeLensRun", Arguments = new JArray(data)},
//            Range = new  OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0,0,0,10),
//            Data = data
//        });
//    }

//    /// <inheritdoc />
//    public override async Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken)
//    {
//        throw new NotImplementedException("Hello Hello Hello");
//    }
//}


//internal class CodeLensHandler1 : ICodeLensHandler
//{
//    /// <inheritdoc />
//    public async Task<CodeLensContainer> Handle(CodeLensParams request, CancellationToken cancellationToken)
//    {
//        return new CodeLensContainer(new CodeLens()
//        {
//            Command = new Command(){Name = "codeLens/resolve", Title = "Run SCL"},
//            Range = new  OmniSharp.Extensions.LanguageServer.Protocol.Models.Range(0,0,0,10)
//        });
//    }

//    /// <inheritdoc />
//    public CodeLensRegistrationOptions
//        GetRegistrationOptions(CodeLensCapability capability, ClientCapabilities clientCapabilities)
//    {
//        Debugger.Launch();

//        Capability = capability;
//        ClientCapabilities = clientCapabilities;


//        return new CodeLensRegistrationOptions()
//        {
//            DocumentSelector = new DocumentSelector(
//                TextDocumentSyncHandler.DocumentSelector),
//            ResolveProvider = true
//        };
//    }

//    public CodeLensCapability Capability { get; set; }

//    public ClientCapabilities ClientCapabilities { get; set; }
//}

//internal class CodeLensResolveHandler2 : ICodeLensResolveHandler
//{
//    /// <inheritdoc />
//    public async Task<CodeLens> Handle(CodeLens request, CancellationToken cancellationToken)
//    {
//        throw new NotImplementedException("Hello Hello Hello");

//        return new CodeLens() { };
//    }

//    /// <inheritdoc />
//    public void SetCapability(CodeLensCapability capability, ClientCapabilities clientCapabilities)
//    {
//        Capability = capability;
//        ClientCapabilities = clientCapabilities;
//    }

//    public CodeLensCapability Capability { get; set; }

//    public ClientCapabilities ClientCapabilities { get; set; }

    
//    /// <inheritdoc />
//    public Guid Id => Guid.NewGuid();
//}