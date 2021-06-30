using System;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Server;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;

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

            //Log.Logger.Information("This only goes file...");


            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

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
                        .WithServices(x =>
                            x.AddSingleton<IFileSystem>(new FileSystem())
                                //.AddSingleton(SCLSettings.CreateFromIConfiguration(configuration))
                                .AddSingleton<DocumentManager>()
                                .AddInMemoryConnectorManager(configuration)
                        )
                        .WithHandler<CompletionHandler>()
                        .WithHandler<TextDocumentSyncHandler>()
                        .WithHandler<HoverHandler>()
                        .WithServices(x => x.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace)))
            );

            await server.WaitForExit;
        }
    }

    /// <summary>
    /// Extension methods for dependency injection using IServiceCollection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Create all the required services to set up a ConnectorManager using
        /// a JSON configuration file.
        /// 
        ///   - ConnectorManagerSettings
        ///   - ConnectorRegistrySettings
        ///   - IConnectorRegistry
        ///   - IConnectorConfiguration (FileConnectorConfiguration)
        ///   - IConnectorManager
        ///
        /// Additional services required for the connector manager are:
        ///
        ///    - System.IO.Abstractions.IFileSystem
        ///    - Microsoft.Extensions.Logging.ILogger
        /// 
        /// </summary>
        /// <param name="services">IServiceCollection</param>
        /// <param name="configuration">The application Configuration.</param>
        /// <returns>IServiceCollection</returns>
        public static IServiceCollection AddInMemoryConnectorManager(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var managerSettings = configuration.GetSection(ConnectorManagerSettings.Key)
                .Get<ConnectorManagerSettings>() ?? ConnectorManagerSettings.Default;

            services.AddSingleton(managerSettings);

            var registrySettings = configuration.GetSection(ConnectorRegistrySettings.Key)
                .Get<ConnectorRegistrySettings>() ?? ConnectorRegistrySettings.Reductech;

            services.AddSingleton(registrySettings);

            services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();

            services.AddSingleton<IConnectorConfiguration>(_=> new ConnectorConfiguration());

            //services.AddSingleton<IConnectorConfiguration>(serviceProvider =>
            //{
            //    //var sclSettings = serviceProvider.GetRequiredService<SCLSettings>();

            //    //var settings = ConnectorSettings.CreateFromSCLSettings(sclSettings).ToDictionary(x => x.Key, x => x.Settings);
            //    var connectorConfiguration = new ConnectorConfiguration();

            //    return connectorConfiguration;
            //});

            services.AddSingleton<IConnectorManager, ConnectorManager>();


            return services;
        }
    }
}