using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Reductech.Sequence.ConnectorManagement;
using Reductech.Sequence.ConnectorManagement.Base;

namespace LanguageServer;

/// <summary>
/// Configuration for SCL language server
/// </summary>
[DataContract]
public record SCLLanguageServerConfiguration
{

    /// <summary>
    /// The key to the connector settings property
    /// </summary>
    public const string ConnectorSettingsKey = "connectors";

    /// <summary>
    /// Settings for the Connector Manager
    /// </summary>
    [DataMember(Name = ConnectorManagerSettings.Key)]
    [JsonPropertyName(ConnectorManagerSettings.Key)]
    public ConnectorManagerSettings? ConnectorManagerSettings { get; init; }
    

    /// <summary>
    /// Dictionary of Connector Settings
    /// </summary>
    [DataMember(Name = ConnectorSettingsKey)]
    [JsonPropertyName(ConnectorSettingsKey)]
    public Dictionary<string, ConnectorSettings>? ConnectorSettingsDictionary { get; init; }
}