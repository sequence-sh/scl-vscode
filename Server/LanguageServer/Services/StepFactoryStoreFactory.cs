using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core.Connectors;
using Reductech.EDR.Core.Internal;

namespace LanguageServer.Services
{
    public interface IAsyncFactory<T>
    {
        public Task<T> GetValueAsync();
    }


    public class StepFactoryStoreFactory : IAsyncFactory<StepFactoryStore>
    {
        public ILanguageServerConfiguration LanguageServerConfiguration { get; }
        private readonly ILogger<StepFactoryStoreFactory> _logger;

        private readonly ReactiveSource<StepFactoryStore, SCLLanguageServerConfiguration> _stepFactoryStoreSource;

        public StepFactoryStoreFactory(
            ILanguageServerConfiguration languageServerConfiguration,
            EntityChangeSync<SCLLanguageServerConfiguration> optionsMonitor,
            ILoggerFactory loggerFactory,
            ILogger<StepFactoryStoreFactory> logger,
            IFileSystem fileSystem
            )
        {
            LanguageServerConfiguration = languageServerConfiguration;
            _logger = logger;

            _stepFactoryStoreSource = new ReactiveSource<StepFactoryStore, SCLLanguageServerConfiguration>(
                async config =>
                {
                    var connectorRegistry = new ConnectorRegistry(
                        loggerFactory.CreateLogger<ConnectorRegistry>(),
                        config.ConnectorRegistrySettings ?? ConnectorRegistrySettings.Reductech);


                    
                    var connectorManagerLogger = loggerFactory.CreateLogger<ConnectorManager>();
                    var settings = config.ConnectorManagerSettings ?? ConnectorManagerSettings.Default;

                    var connectorConfigurationDict = config.ConnectorSettingsDictionary?
                        .Where(x=>!string.IsNullOrWhiteSpace(x.Value.Id))
                        .ToDictionary(x=>x.Key, x=>x.Value);
                    if (connectorConfigurationDict is null)
                    {
                        //load latest connectors from repository
                        var manager1 = new ConnectorManager(connectorManagerLogger, settings, connectorRegistry,
                            new ConnectorConfiguration(), fileSystem);

                        var found = await manager1.Find(null, false);

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

                    var sfs = await connectorManager.GetStepFactoryStoreAsync();

                    return sfs;
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