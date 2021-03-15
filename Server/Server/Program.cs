using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using System;
using System.Threading.Tasks;
using Reductech.EDR.Connectors.Nuix.Steps.Meta;
using Reductech.EDR.Core.Internal;

namespace Server
{
    internal class Program
    {
        private static void Main(string[] args) => MainAsync(args).Wait();

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


            var server = await LanguageServer.From(
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
                               , typeof(IRubyScriptStep)
                               , typeof(Reductech.EDR.Connectors.Sql.Steps.SqlQuery)
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
