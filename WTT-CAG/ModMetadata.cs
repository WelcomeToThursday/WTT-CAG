using JetBrains.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace WTTClothingAndGear;

[UsedImplicitly]
public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.wtt.cag";
    public string Name { get; init; } = "WTT-CAG";
    public string Author { get; init; } = "The WTT Team";
    public List<string>? Contributors { get; init; } = null;
    public Version Version { get; init; } = new(typeof(ModMetadata).Assembly.GetName().Version!.ToString(3));
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, Range>? ModDependencies { get; init; } = new()
    {
        {
            "com.wtt.commonlib", new Range("^3.0.3")
        },
        {
            "com.wtt.contentbackport", new Range("^2.0.0")
        },
        {
            // Require Armory without guessing a minimum version for its 4.1 port.
            "com.wtt.armory", new Range("*")
        }
    };

    public string? Url { get; init; } = "https://github.com/WelcomeToTarkov/WTT-CAG";
    public string License { get; init; } = "CC-BY-NC-ND 4.0";
}
