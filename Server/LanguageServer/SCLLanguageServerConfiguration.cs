using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;

namespace LanguageServer
{
    /// <summary>
    /// Configuration for SCL language server
    /// </summary>
    [DataContract]
    public record SCLLanguageServerConfiguration
    {
        /// <summary>
        /// The key to the EDR path property
        /// </summary>
        public const string PathKey = "path";
        /// <summary>
        /// The key to the launch debugger property
        /// </summary>
        public const string LaunchDebuggerKey = "launchdebugger";

        /// <summary>
        /// The key to the connector settings property
        /// </summary>
        public const string ConnectorSettingsKey = "connectors";

        /// <summary>
        /// The path to the EDR executable
        /// </summary>
        [DataMember(Name = PathKey)]
        [JsonPropertyName(PathKey)]
        public string EDRPath { get; init; } = "";

        /// <summary>
        /// Whether to launch the C# debugger
        /// </summary>
        [DataMember(Name = LaunchDebuggerKey)]
        [JsonPropertyName(LaunchDebuggerKey)]
        public bool LaunchDebugger { get; init; }

        /// <summary>
        /// Settings for the Connector Manager
        /// </summary>
        [DataMember(Name = ConnectorManagerSettings.Key)]
        [JsonPropertyName(ConnectorManagerSettings.Key)]
        public ConnectorManagerSettings? ConnectorManagerSettings { get; init; }

        /// <summary>
        /// Settings for the Connector Registry
        /// </summary>
        [DataMember(Name = ConnectorRegistrySettings.Key)]
        [JsonPropertyName(ConnectorRegistrySettings.Key)]
        public ConnectorRegistrySettings? ConnectorRegistrySettings { get; init; }

        /// <summary>
        /// Dictionary of Connector Settings
        /// </summary>
        [DataMember(Name = ConnectorSettingsKey)]
        [JsonPropertyName(ConnectorSettingsKey)]
        public Dictionary<string, ConnectorSettings>? ConnectorSettingsDictionary { get; init; }
    }
}