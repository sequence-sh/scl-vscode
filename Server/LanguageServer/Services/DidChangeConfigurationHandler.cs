using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;
using Reductech.EDR.Core.Entities;

namespace LanguageServer.Services
{
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

        /// <inheritdoc />
        public async Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var mainSection = request.Settings?["reductech-scl"]?["edr"];

            var newText = mainSection?.ToString();

            if (newText is null)
            {
                _logger.LogError("Configuration did not contain 'reductech-scl.edr' ");
                return Unit.Value;
            }

            var options = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter(), VersionJsonConverter.Instance },
                PropertyNameCaseInsensitive = true
            };

            var newConfig = JsonSerializer.Deserialize<SCLLanguageServerConfiguration>(newText, options);

            if (newConfig is null)
            {
                _logger.LogError("Could not deserialize 'reductech-scl.edr' ");
                return Unit.Value;
            }

            _ = EntityChangeSync.TryUpdate(newConfig);

            //if (changed)
            //{
            //    await File.WriteAllTextAsync(Program.AppSettingsPath, newText, cancellationToken);
            //}

            return Unit.Value;
        }

        /// <inheritdoc />
        public void SetCapability(DidChangeConfigurationCapability capability, ClientCapabilities clientCapabilities)
        {
            _capability = capability;
        }
    }
}