using System;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Reductech.Sequence.ConnectorManagement;
using Reductech.Sequence.ConnectorManagement.Base;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.Connectors;
using Reductech.Sequence.Core.Internal;

namespace LanguageServer.Services;

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
public class
    StepFactoryStoreFactory : IAsyncFactory<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)>
{
    private ILoggerFactory LoggerFactory { get; }
    private ILogger<StepFactoryStoreFactory> Logger { get; }
    private IFileSystem FileSystem { get; }
    private ILanguageServerFacade LanguageServerFacade { get; }

    private readonly
        ReactiveSource<(StepFactoryStore stepFactoryStore, IExternalContext externalContext),
            SCLLanguageServerConfiguration> _stepFactoryStoreSource;

    /// <summary>
    /// Create a new StepFactoryStoreFactory
    /// </summary>
    public StepFactoryStoreFactory(
        EntityChangeSync<SCLLanguageServerConfiguration> optionsMonitor,
        ILoggerFactory loggerFactory,
        ILogger<StepFactoryStoreFactory> logger,
        IFileSystem fileSystem,
        ILanguageServerFacade languageServerFacade
    )
    {
        LoggerFactory = loggerFactory;
        Logger = logger;
        FileSystem = fileSystem;
        LanguageServerFacade = languageServerFacade;

        _stepFactoryStoreSource =
            new ReactiveSource<(StepFactoryStore stepFactoryStore, IExternalContext externalContext),
                SCLLanguageServerConfiguration>(
                Create, optionsMonitor
            );
    }

    async Task<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> Create(
        SCLLanguageServerConfiguration config)
    {

        var settings = config.ConnectorManagerSettings ?? ConnectorManagerSettings.Default;

        var connectorRegistry = new ConnectorRegistry(LoggerFactory.CreateLogger<ConnectorRegistry>(),settings);

        var connectorManagerLogger = LoggerFactory.CreateLogger<ConnectorManager>();
        

        Logger.LogInformation(
            $"Connector Settings\r\nConfiguration Path: {settings.ConfigurationPath}\r\nConnector Path: {settings.ConnectorPath}");


        var connectorConfigurationDict = config.ConnectorSettingsDictionary
            ?.Where(x => !string.IsNullOrWhiteSpace(x.Value.Id))
            .ToDictionary(x => x.Key, x => x.Value);

        if (connectorConfigurationDict is null || connectorConfigurationDict.Count == 0)
        {
            const string connectorFilter = "Reductech.Sequence";

            //load latest connectors from repository
            var manager1 = new ConnectorManager(connectorManagerLogger, settings, connectorRegistry,
                new ConnectorConfiguration(), FileSystem);

            var found = await manager1.Find(); //Find all connectors

            connectorConfigurationDict = found
                .Where(x=>x.Id.Contains(connectorFilter, StringComparison.OrdinalIgnoreCase))
                
                .ToDictionary(x => x.Id,
                x => new ConnectorSettings() { Enable = true, Id = x.Id, Version = GetBestVersion(x.Version) });
        }

        var connectorManager = new ConnectorManager(connectorManagerLogger, settings, connectorRegistry,
            new ConnectorConfiguration(connectorConfigurationDict), FileSystem);

        var consoleAdapter = new LanguageServerConsoleAdapter(LanguageServerFacade);

        var defaultExternalContext = new ExternalContext(ExternalContext.Default.ExternalProcessRunner,
            ExternalContext.Default.RestClientFactory, consoleAdapter);

        var externalContextResult = await connectorManager.GetExternalContextAsync(
            defaultExternalContext.ExternalProcessRunner, defaultExternalContext.RestClientFactory,
            defaultExternalContext.Console, CancellationToken.None);


        if (externalContextResult.IsFailure)
        {
            Logger.LogError(externalContextResult.Error.AsString);
            return (StepFactoryStore.Create(), defaultExternalContext);
        }

        var sfsResult =
            await connectorManager.GetStepFactoryStoreAsync(externalContextResult.Value, CancellationToken.None);

        if (sfsResult.IsFailure)
        {
            Logger.LogError(sfsResult.Error.AsString);
            return (StepFactoryStore.Create(), externalContextResult.Value);
        }

        return (sfsResult.Value, externalContextResult.Value);
    }

    private static string GetBestVersion(string latestVersionString)
    {
        try
        {
            var latestVersion = Version.Parse(latestVersionString);

            var thisVersion = Assembly.GetEntryAssembly()!.GetName().Version!;

            if (latestVersion.Major > thisVersion.Major || latestVersion.Minor > thisVersion.Minor)
                return thisVersion.ToString();

            return latestVersionString;
        }
        catch (Exception)
        {
            //In case something goes wrong
            return latestVersionString;
        }
    }

    /// <inheritdoc />
    public async Task<(StepFactoryStore stepFactoryStore, IExternalContext externalContext)> GetValueAsync()
    {
        return await _stepFactoryStoreSource.Value; //Errors here may be caused by the project version being lower than the desired connector version
    }
}