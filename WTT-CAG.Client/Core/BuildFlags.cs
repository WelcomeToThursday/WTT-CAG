namespace WTT_CAG.Client.Core;

// DebugBuild true: every thermal setting in F12, the Diagnostics section, the tube mask selector and
// all info logging. false (release): F12 shows only Disable Thermal Overlays and the mode key, and only
// warnings and errors are logged. In a debug build, values saved in the .cfg override the baked
// ones in ThermalDeviceProfiles - delete a goggle's old sections before retuning it.
public static class BuildFlags
{
    // static readonly rather than const, so code behind a false flag does not warn as unreachable.
    public static readonly bool DebugBuild = true;
}
