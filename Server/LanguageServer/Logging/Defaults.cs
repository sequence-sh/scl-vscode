namespace LanguageServer.Logging;

internal class Defaults
{

    public const string DefaultConfig = @"{
  ""nlog"": {
    ""throwConfigExceptions"": true,
    ""variables"": {
      ""sequencelogname"": ""..\\scl""
    },
    ""targets"": {
      ""fileTarget"": {
        ""type"": ""File"",
        ""fileName"": ""${basedir:fixtempdir=true}\\${sequencelogname}.log"",
        ""layout"": ""${date} ${level:uppercase=true} ${message} ${exception}""
      },
      ""outputWindow"": {
        ""type"": ""OutputWindow"",
        ""layout"": ""${date} ${message}""
      }
    },
    ""rules"": [
      {
        ""logger"": ""*"",
        ""minLevel"": ""Error"",
        ""writeTo"": ""fileTarget,outputWindow"",
        ""final"": true
      },
      {
        ""logger"": ""*"",
        ""minLevel"": ""Debug"",
        ""writeTo"": ""outputWindow""
      },
      {
        ""logger"": ""*"",
        ""minLevel"": ""Debug"",
        ""writeTo"": ""fileTarget""
      }
    ]
  }

}";
}