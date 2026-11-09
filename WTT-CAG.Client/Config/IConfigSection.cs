using BepInEx.Configuration;
namespace WTT_CAG.Client.Config;

public interface IConfigSection
{
    void Init(ConfigFile cfg);
}
