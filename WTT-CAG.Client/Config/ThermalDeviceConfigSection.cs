using BepInEx.Configuration;
using WTT_CAG.Client.Core.Thermal;

namespace WTT_CAG.Client.Config;

// Exposes one thermal device's tuning values in the F12 menu. Only used while
// ThermalConfigSection.TuningEnabled is on - the values themselves live on the profile.
public class ThermalDeviceConfigSection(ThermalDeviceProfile profile) : IConfigSection
{
    public void Init(ConfigFile cfg)
    {
        var cam = $"{profile.Name} — Zone Camera";
        var place = $"{profile.Name} — Zone Placement";
        var image = $"{profile.Name} — Zone Image";
        var fx = $"{profile.Name} — Sensor";
        var dev = $"{profile.Name} — Device Overlay";

        profile.Fov.Bind(cfg, cam, "FOV", "Field of view of the thermal camera. Narrower than the NVG view means the circle is magnified, as on the real device.", new AcceptableValueRange<float>(5f, 90f));
        profile.Resolution.Bind(cfg, cam, "Resolution", "Horizontal resolution of the thermal render. Lower is cheaper; 320 approximates a real device's sensor.", new AcceptableValueRange<int>(128, 1024));
        profile.RenderMode.Bind(cfg, cam, "Render Mode", "0 = Trace, 1 = Full, 2 = Outline.", new AcceptableValueRange<int>(0, 2));
        profile.Zoom.Bind(cfg, cam, "Zoom", "1 keeps the thermal aligned 1:1 with the world behind the circle. Above 1 magnifies.", new AcceptableValueRange<float>(0.25f, 4f));
        profile.CullLayers.Bind(cfg, cam, "Cull Layers", "Off by default: a real thermal sees whatever is in front of it, including your own hands.");
        profile.ExcludeLayers.Bind(cfg, cam, "Exclude Layers", "Comma-separated layer names to hide when Cull Layers is on. Unknown names are ignored and listed in the log.");

        profile.ZoneEnabled.Bind(cfg, place, "Enabled", "Master switch for this goggle's thermal channel.");
        profile.ZoneCount.Bind(cfg, place, "Zone Count", "How many thermal circles to draw. 1 for a single clip-on module, 2 for a dual-tube set, 4 for a panoramic.", new AcceptableValueRange<int>(1, ThermalDeviceProfile.MaxZones));
        profile.ZoneShape.Bind(cfg, place, "Zone Shape", "0 = circles, set by Zone Count and the Zone sections. 1 = one rectangle, set by the Rectangle settings below.", new AcceptableValueRange<int>(0, 1));
        profile.RectFromHud.Bind(cfg, place, "Rectangle Follows HUD", "Fit the rectangle to the HUD's Display panel, so thermal and HUD always line up. The size and offset settings are only used when this is off or the goggle has no HUD.");
        profile.RectWidth.Bind(cfg, place, "Rectangle Width", "In HUD canvas units, where the screen is 1080 tall.", new AcceptableValueRange<float>(50f, 4000f));
        profile.RectHeight.Bind(cfg, place, "Rectangle Height", "In HUD canvas units, where the screen is 1080 tall.", new AcceptableValueRange<float>(50f, 1080f));
        profile.RectOffsetX.Bind(cfg, place, "Rectangle Offset X", "Moves the rectangle from screen centre, in HUD canvas units.", new AcceptableValueRange<float>(-1500f, 1500f));
        profile.RectOffsetY.Bind(cfg, place, "Rectangle Offset Y", "Moves the rectangle from screen centre, in HUD canvas units.", new AcceptableValueRange<float>(-540f, 540f));
        profile.RectInset.Bind(cfg, place, "Rectangle Inset", "Shrinks the rectangle on every side, in HUD canvas units. Use it to keep the thermal inside a frame drawn on the HUD.", new AcceptableValueRange<float>(-100f, 200f));
        profile.RectSoftness.Bind(cfg, place, "Rectangle Edge", "Edge softness of the rectangle. 0 is a hard edge.", new AcceptableValueRange<float>(0f, 0.05f));
        profile.Opacity.Bind(cfg, place, "Opacity", "How strongly the thermal replaces the night vision image, across all zones.", new AcceptableValueRange<float>(0f, 1f));

        for (var z = 0; z < ThermalDeviceProfile.MaxZones; z++)
        {
            var slot = profile.Zones[z];
            var zs = $"{profile.Name} — Zone {z + 1}";

            slot.Enabled.Bind(cfg, zs, "Enabled", "Draw thermal in this zone. A dual-tube goggle with thermal in one eye only leaves the other zone off.");
            slot.CenterX.Bind(cfg, zs, "Center X", "Horizontal centre, in screen fractions.", new AcceptableValueRange<float>(0f, 1f));
            slot.CenterY.Bind(cfg, zs, "Center Y", "Vertical centre, in screen fractions.", new AcceptableValueRange<float>(0f, 1f));
            slot.Radius.Bind(cfg, zs, "Radius", "Radius of this circle.", new AcceptableValueRange<float>(0.02f, 1.5f));
            slot.Softness.Bind(cfg, zs, "Edge", "Edge softness. Raise on adjacent tubes so their thermal blends where they meet rather than butting hard.", new AcceptableValueRange<float>(0f, 0.5f));
        }

        profile.ClipToTubes.Bind(cfg, place, "Clip To Tubes", "Clip thermal to the goggle's own tube mask, so nothing spills into the black outside the tubes. Uses the same mask texture BSG cuts the night vision with, so it aligns by construction.");
        profile.TubeMaskScale.Bind(cfg, place, "Tube Mask Scale", "Fine adjustment if the clip does not line up with the tubes. 1 uses the game's own mask size unmodified.", new AcceptableValueRange<float>(0.5f, 2f));
        profile.TubeMaskSoftness.Bind(cfg, place, "Tube Mask Edge", "Softens the clip edge so thermal fades into the tube rim.", new AcceptableValueRange<float>(0f, 0.3f));
        profile.TubeMaskChannel.Bind(cfg, place, "Tube Mask Channel", "Which channel of the mask texture is the tube shape. 0 = alpha, 1 = red. Try 1 if clipping removes everything.", new AcceptableValueRange<int>(0, 1));
        profile.HeatKeyOnly.Bind(cfg, place, "Heat Only", "ON: draw only hot pixels and let the night vision show through the cold ones, so the circle is an overlay rather than a cutout. OFF: the circle replaces the NVG image, cold areas included, which reads as black.");
        profile.HeatKeyThreshold.Bind(cfg, place, "Heat Only Threshold", "How bright a thermal pixel must be to be drawn when Heat Only is on. Raise to show only the hottest objects.", new AcceptableValueRange<float>(0f, 1f));
        profile.HeatKeySoftness.Bind(cfg, place, "Heat Only Softness", "Edge softness of the Heat Only cutoff. Higher fades hot areas into the night vision instead of cutting sharply.", new AcceptableValueRange<float>(0.001f, 0.5f));

        profile.HotColor.Bind(cfg, image, "Hot Color", "Colour of hot objects.");
        profile.ColdColor.Bind(cfg, image, "Cold Color", "Colour of cold areas. Near-black unless Heat Only is on, in which case cold is transparent and this barely matters.");
        profile.HeatFloor.Bind(cfg, image, "Heat Floor", "Cold cutoff. Raise to reject cool objects.", new AcceptableValueRange<float>(0f, 1f));
        profile.HeatGain.Bind(cfg, image, "Heat Gain", "Overall heat strength multiplier.", new AcceptableValueRange<float>(1f, 4f));
        profile.HeatPower.Bind(cfg, image, "Heat Power", "Contrast curve. Below 1 brightens mid heat.", new AcceptableValueRange<float>(0.2f, 3f));
        profile.TraceThreshold.Bind(cfg, image, "Trace Threshold", "How hot something must be to show in Trace/Outline modes.", new AcceptableValueRange<float>(0f, 1f));
        profile.TraceSoftness.Bind(cfg, image, "Trace Softness", "Threshold band softness.", new AcceptableValueRange<float>(0.01f, 0.5f));
        profile.TraceIntensity.Bind(cfg, image, "Trace Brightness", "Brightness of the thermal colour.", new AcceptableValueRange<float>(0f, 2f));
        profile.OutlineIntensity.Bind(cfg, image, "Outline Intensity", "Sobel edge strength in Outline mode.", new AcceptableValueRange<float>(0f, 5f));
        profile.OutlineWidth.Bind(cfg, image, "Outline Width", "Edge sampling thickness.", new AcceptableValueRange<float>(0.001f, 0.02f));

        profile.FxNoise.Bind(cfg, fx, "Noise", "Sensor noise, using the game's own thermal noise rather than a reimplementation. Uncooled microbolometers are noisier than cooled ones, so a cheap clip-on wants this and a dedicated sight may not.");
        profile.FxFpsStuck.Bind(cfg, fx, "Low Refresh Rate", "Emulate a slow sensor refresh, so the thermal image updates in steps rather than every frame. This is the single most recognisable thermal artefact — export-restricted civilian units in particular run far below display framerate.");
        profile.FxMotionBlur.Bind(cfg, fx, "Motion Blur", "Smearing as the sensor integrates across a frame. Pairs with a low refresh rate; on its own it tends to look like a post effect rather than a sensor.");
        profile.FxGlitch.Bind(cfg, fx, "Glitch", "Occasional frame corruption. Sparing use only — constant glitching reads as broken hardware rather than a working device.");
        profile.FxPixelate.Bind(cfg, fx, "Pixelate", "Coarsen the image to suggest a low sensor resolution. Consider lowering Zone Camera Resolution instead, which is the same idea applied at the source and costs less.");
        profile.UnsharpRadius.Bind(cfg, fx, "Sharpen Radius", "Edge sharpening radius. Thermal imagers apply their own edge enhancement; 0 disables.", new AcceptableValueRange<float>(0f, 4f));
        profile.UnsharpBias.Bind(cfg, fx, "Sharpen Bias", "Strength of the edge sharpening.", new AcceptableValueRange<float>(0f, 4f));

        profile.OverlayTexture.Bind(cfg, dev, "Overlay Texture", "Asset name of an RGBA texture in the bundle to draw over the whole view — bezel, reticle, tube artwork, dirt. Leave empty for none. Names that fail to load are reported once in the log.");
        profile.OverlayOpacity.Bind(cfg, dev, "Overlay Opacity", "How strongly the overlay texture is drawn.", new AcceptableValueRange<float>(0f, 1f));
        profile.OverlayTint.Bind(cfg, dev, "Overlay Tint", "Multiplied into the overlay texture. Lets one shared faceplate be recoloured per goggle instead of authoring several.");
        profile.VignetteStrength.Bind(cfg, dev, "Vignette Strength", "Darkening toward the edge of the view, as a tube falls off. 0 disables it.", new AcceptableValueRange<float>(0f, 1f));
        profile.VignetteStart.Bind(cfg, dev, "Vignette Start", "Distance from centre where the vignette begins.", new AcceptableValueRange<float>(0f, 1f));
        profile.ScintillationStrength.Bind(cfg, dev, "Scintillation", "Live sparkle from photon noise in an image intensifier. Subtle values read best; 0 disables it.", new AcceptableValueRange<float>(0f, 1f));
        profile.ScintillationScale.Bind(cfg, dev, "Scintillation Grain", "Speck size. Higher is coarser.", new AcceptableValueRange<float>(1f, 32f));
        profile.ScintillationSpeed.Bind(cfg, dev, "Scintillation Speed", "How fast the sparkle reshuffles.", new AcceptableValueRange<float>(0f, 60f));
    }
}
