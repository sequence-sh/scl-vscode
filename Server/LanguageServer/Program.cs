using System.IO.Abstractions;
using LanguageServer.Logging;
using LanguageServer.Services;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.LanguageServer.Server;
using Reductech.Sequence.Core.Abstractions;
using NLog.Config;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace LanguageServer;

internal class Program
{
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
    // ReSharper disable once UnusedParameter.Local
    private static void Main(string[] _) => MainAsync().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

    public static ILanguageServerFacade LanguageServerFacade { get; private set; } = null!;

    private static async Task MainAsync()
    {
        ConfigurationItemFactory
            .Default
            .Targets
            .RegisterDefinition("OutputWindow", typeof(OutputWindowTarget));

        var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
            options =>
            {
                options
                    .WithInput(Console.OpenStandardInput())
                    .WithOutput(Console.OpenStandardOutput())
                    .ConfigureLogging(x =>
                    {
                        x.AddLanguageProtocolLogging()
                            .SetMinimumLevel(Microsoft.Extensions.Logging. LogLevel.Debug);
                    })
                    .WithServices(x =>
                        x.AddSingleton<IFileSystem>(new FileSystem())
                            .AddSingleton<DocumentManager>()
                            .AddSingleton<
                                IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>
                                , StepFactoryStoreFactory>()
                            .AddSingleton(typeof(EntityChangeSync<>))
                    )
                    .WithHandler<DidChangeConfigurationHandler>()
                    .WithHandler<CompletionHandler>()
                    .WithHandler<TextDocumentSyncHandler>()
                    .WithHandler<HoverHandler>()
                    .WithHandler<RenameHandler>()
                    .WithHandler<SignatureHelpHandler>()
                    .WithHandler<FormattingHandler>()
                    .WithHandler<RunSCLHandler>()
                    .WithHandler<StartDebuggerHandler>()
                    .OnStarted((ls, _) =>
                    {
                        LanguageServerFacade = ls.GetRequiredService<ILanguageServerFacade>();
                        var logger = ls.GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Language Server started");


                        return Task.CompletedTask;
                    });
            });

        await server.WaitForExit;
    }
}