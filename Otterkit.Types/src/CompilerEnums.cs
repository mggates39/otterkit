using System.Text.Json.Serialization;

namespace Otterkit.Types;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SourceFormat
{
    Auto,
    Fixed,
    Free,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BuildType
{
    LexOnly,
    ParseOnly,
    PrintTokens,
    PrintSymbols,
    BuildOnly,
    BuildAndRun,
    GenerateOnly,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OutputType
{
    Application,
    Library,
}
