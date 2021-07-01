using System.Collections.Generic;
using System.Runtime.Serialization;
using Reductech.EDR.ConnectorManagement;
using Reductech.EDR.ConnectorManagement.Base;

namespace LanguageServer
{
    [DataContract]
    public record SCLLanguageServerConfiguration
    {
        public const string PathKey = "path";
        public const string LaunchDebuggerKey = "launchdebugger";
        public const string ConnectorSettingsKey = "connectors";

        [DataMember(Name = PathKey)] public string EDRPath { get; init; } = "";

        [DataMember(Name = LaunchDebuggerKey)]
        public bool LaunchDebugger { get; init; }

        [DataMember(Name = ConnectorManagerSettings.Key)]
        public ConnectorManagerSettings? ConnectorManagerSettings { get; init; }

        [DataMember(Name = ConnectorRegistrySettings.Key)]
        public ConnectorRegistrySettings? ConnectorRegistrySettings { get; init; }

        [DataMember(Name = ConnectorSettingsKey)]
        public Dictionary<string, ConnectorSettings>? ConnectorSettingsDictionary { get; init; }
    }
}