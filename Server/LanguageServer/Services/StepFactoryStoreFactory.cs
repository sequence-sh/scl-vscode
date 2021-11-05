using System.IO.Abstractions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Connectors;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services
{
    /// <summary>
    /// A factory that creates its objects asynchronously
    /// </summary>
    public interface IAsyncFactory<T>
    {
        /// <summary>
        /// Gets the factory value
        /// </summary>
        public Task<T> GetValueAsync();
    }

    /// <summary>
    /// Service that creates StepFactoryStores
    /// </summary>
    public class StepFactoryStoreFactory : IAsyncFactory<StepFactoryStore>
    {
        private readonly ReactiveSource<StepFactoryStore, SCLLanguageServerConfiguration> _stepFactoryStoreSource;

        /// <summary>
        /// Create a new StepFactoryStoreFactory
        /// </summary>
        public StepFactoryStoreFactory(
            EntityChangeSync<SCLLanguageServerConfiguration> optionsMonitor,
            ILoggerFactory loggerFactory,
            ILogger<StepFactoryStoreFactory> logger,
            IFileSystem fileSystem
        )
        {
            _stepFactoryStoreSource = new ReactiveSource<StepFactoryStore, SCLLanguageServerConfiguration>(
                async config =>
                {
                    var connectorRegistry = new ConnectorRegistry(
                        loggerFactory.CreateLogger<ConnectorRegistry>(),
                        config.ConnectorRegistrySettings ?? ConnectorRegistrySettings.Reductech);

                    var connectorManagerLogger = loggerFactory.CreateLogger<ConnectorManager>();
                    var settings = config.ConnectorManagerSettings ?? ConnectorManagerSettings.Default;

                    logger.LogWarning(
                        $"Connector Settings\r\nConfiguration Path: {settings.ConfigurationPath}\r\nConnector Path: {settings.ConnectorPath}");


                    var connectorConfigurationDict = config.ConnectorSettingsDictionary?
                        .Where(x => !string.IsNullOrWhiteSpace(x.Value.Id))
                        .ToDictionary(x => x.Key, x => x.Value);

                    if (connectorConfigurationDict is null || !connectorConfigurationDict.Any())
                    {
                        //load latest connectors from repository
                        var manager1 = new ConnectorManager(connectorManagerLogger, settings, connectorRegistry,
                            new ConnectorConfiguration(), fileSystem);

                        var found = await manager1.Find(); //Find all connectors

                        connectorConfigurationDict = found.ToDictionary(x => x.Id, x => new ConnectorSettings()
                        {
                            Enable = true,
                            Id = x.Id,
                            Version = x.Version
                        });
                    }

                    var connectorManager =
                        new ConnectorManager(connectorManagerLogger,
                            settings,
                            connectorRegistry,
                            new ConnectorConfiguration(connectorConfigurationDict),
                            fileSystem
                        );

                    var externalContextResult = await
                        connectorManager.GetExternalContextAsync(
                            ExternalContext.Default.ExternalProcessRunner,
                            ExternalContext.Default.RestClientFactory,
                            ExternalContext.Default.Console,
                            CancellationToken.None);


                    if (externalContextResult.IsFailure)
                    {
                        logger.LogError(externalContextResult.Error.AsString);
                        return StepFactoryStore.Create();
                    }

                    var sfsResult = await connectorManager. GetStepFactoryStoreAsync(externalContextResult.Value,
                        CancellationToken.None);

                    if (sfsResult.IsFailure)
                    {
                        logger.LogError(sfsResult.Error.AsString);
                        return StepFactoryStore.Create();
                    }

                    return sfsResult.Value;
                }, optionsMonitor
            );
        }

        /// <inheritdoc />
        public async Task<StepFactoryStore> GetValueAsync()
        {
            return await _stepFactoryStoreSource.Value;
        }
    }
}