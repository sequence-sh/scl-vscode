using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Reductech.EDR.Core.Internal;

namespace LanguageServer
{
    internal class Program
    {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
        private static void Main(string[] args) => MainAsync(args).Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits

        private static async Task MainAsync(string[] args)
        {
            // Debugger.Launch();
            // while (!Debugger.IsAttached)
            // {
            //     await Task.Delay(100);
            // }

            //Log<>.Logger = new LoggerConfiguration()
            //            .Enrich.FromLogContext()
            //            .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
            //            .MinimumLevel.Verbose()
            //            .CreateLogger();

            //Log.Logger.Information("This only goes file...");


            var server = await OmniSharp.Extensions.LanguageServer.Server.LanguageServer.From(
                options =>
                    options
                       .WithInput(Console.OpenStandardInput())
                       .WithOutput(Console.OpenStandardOutput())
                       .ConfigureLogging(
                            x => x
                                .AddLanguageProtocolLogging()
                                .SetMinimumLevel(LogLevel.Debug)
                        )
                       .WithServices(x=> x.AddSingleton<DocumentManager>().AddSingleton(
                           _=> StepFactoryStore.CreateUsingReflection(typeof(IStep)
                               , typeof(Reductech.EDR.Connectors.Nuix.Steps.NuixAddConcordance)
                               , typeof(Reductech.EDR.Connectors.Sql.Steps.SqlInsert)
                               , typeof(Reductech.EDR.Connectors.Pwsh.PwshRunScript)
                               )))
                       .WithHandler<CompletionHandler>()
                       .WithHandler<TextDocumentSyncHandler>()
                       .WithHandler<HoverHandler>()
                       .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))


            );

            await server.WaitForExit;
        }
    }
}
