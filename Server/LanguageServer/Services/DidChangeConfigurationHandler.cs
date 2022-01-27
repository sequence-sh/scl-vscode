
using Unit = MediatR.Unit;

namespace LanguageServer.Services;

internal class DidChangeConfigurationHandler : IDidChangeConfigurationHandler
{
    public EntityChangeSync<SCLLanguageServerConfiguration> EntityChangeSync { get; }

    public DidChangeConfigurationHandler( ILogger<DidChangeConfigurationHandler> logger, EntityChangeSync<SCLLanguageServerConfiguration> entityChangeSync)
    {
        EntityChangeSync = entityChangeSync;
        _capability = new DidChangeConfigurationCapability();
        _logger = logger;
    }

    /// <summary>
    /// The capability
    /// </summary>
    // ReSharper disable once NotAccessedField.Local
    private DidChangeConfigurationCapability _capability;

    private readonly ILogger<DidChangeConfigurationHandler> _logger;

    private static readonly JsonSerializerOptions JSONSerializerOptions = new ()
    {
        Converters = { new JsonStringEnumConverter(), VersionJsonConverter.Instance },
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public async Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        var mainSection = request.Settings?["reductech-scl"]?["sequence"];

        var newText = mainSection?.ToString();

        if (newText is null)
        {
            _logger.LogError("Configuration did not contain 'reductech-scl.sequence' ");
            return Unit.Value;
        }
            
        var newConfig = JsonSerializer.Deserialize<SCLLanguageServerConfiguration>(newText, JSONSerializerOptions);

        if (newConfig is null)
        {
            _logger.LogError("Could not deserialize 'reductech-scl.sequence' ");
            return Unit.Value;
        }

        _ = EntityChangeSync.TryUpdate(newConfig);

        SetNLogConfiguration(newText);

        return Unit.Value;
    }

    /// <inheritdoc />
    public void SetCapability(DidChangeConfigurationCapability capability, ClientCapabilities clientCapabilities)
    {
        _capability = capability;
    }

    private static void SetNLogConfiguration(string text)
    {
        const string sectionName = "nlog";

        var sectionBytes = Encoding.UTF8.GetBytes(text);
        var sectionStream = new MemoryStream(sectionBytes);

        var builder = new ConfigurationBuilder().AddJsonStream(sectionStream).Build();

        var section = builder.GetSection(sectionName);
        if (!section.Exists())
        {
            sectionBytes = Encoding.UTF8.GetBytes(Logging.Defaults.DefaultConfig);
            sectionStream = new MemoryStream(sectionBytes);

            builder = new ConfigurationBuilder().AddJsonStream(sectionStream).Build();

            section = builder.GetSection(sectionName);
        }

        LogManager.Configuration = new NLogLoggingConfiguration(section);
    }
}