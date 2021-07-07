using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Workspace;

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

        private DidChangeConfigurationCapability _capability;

        private readonly ILogger<DidChangeConfigurationHandler> _logger;

        /// <inheritdoc />
        public async Task<Unit> Handle(DidChangeConfigurationParams request, CancellationToken cancellationToken)
        {
            var mainSection = request.Settings?["reductech-scl"]?["edr"];

            var newText = mainSection?.ToString();

            if (newText is null)
            {
                _logger.LogError("Configuration did not contain 'reductech-scl.edr' ");
                return Unit.Value;
            }

            var newConfig = JsonConvert.DeserializeObject<SCLLanguageServerConfiguration>(newText);

            if (newConfig is null)
            {
                _logger.LogError("Could not deserialize 'reductech-scl.edr' ");
                return Unit.Value;
            }

            var changed = EntityChangeSync.TryUpdate(newConfig);

            if (changed)
            {
                await File.WriteAllTextAsync(Program.AppSettingsPath, newText, cancellationToken);
            }

            return Unit.Value;
        }

        /// <inheritdoc />
        public void SetCapability(DidChangeConfigurationCapability capability, ClientCapabilities clientCapabilities)
        {
            _capability = capability;
        }
    }
}