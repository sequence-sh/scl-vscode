using System;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Threading.Tasks;
using LanguageServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;

namespace LanguageServer
{
    internal class Program
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        private static void Main(string[] args) => MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        private static async Task MainAsync(string[] _)
        {

            //await OmniSharp.Extensions.LanguageServer.Server.
            
            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
                options =>
                {
                    options
                        .ConfigureLogging(x=>x.SetMinimumLevel(LogLevel.Debug))
                        .WithInput(Console.OpenStandardInput())
                        .WithOutput(Console.OpenStandardOutput())
                        .ConfigureLogging(x => x.AddLanguageProtocolLogging().SetMinimumLevel(LogLevel.Debug))
                        .WithServices(x =>
                            x.AddSingleton<IFileSystem>(new FileSystem())
                                .AddSingleton<DocumentManager>()
                                .AddSingleton<IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>, StepFactoryStoreFactory>()
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


                        .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Debug)))
                        .OnStarted((ls, token) =>
                        {
                            var logger = ls.GetRequiredService<ILogger<Program>>();
                            logger.LogInformation("Language Server started");
                            var changeSync = ls.GetRequiredService<EntityChangeSync<SCLLanguageServerConfiguration>>();

                            if (changeSync.Latest.LaunchDebugger)
                            {
                                Debugger.Launch();
                            }
                            
                            changeSync.OnChange += (_, x) =>
                            {
                                if (x.LaunchDebugger)
                                {
                                    Debugger.Launch();
                                }
                            };


                            return Task.CompletedTask;
                        })
                        ;
                });

            await server.WaitForExit;
        }
    }
}