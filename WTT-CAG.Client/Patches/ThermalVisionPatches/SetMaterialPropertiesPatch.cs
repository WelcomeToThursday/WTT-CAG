using System.Collections.Generic;
using System;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.Content.Thermals;
using WTT_CAG.Client.Content.Thermals.Fusion;
using WTT_CAG.Client.Content.Thermals.Holosun;
using WTT_CAG.Client.Core.Thermal;
namespace WTT_CAG.Client.Patches.ThermalVisionPatches;

public class SetMaterialPropertiesPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(ThermalVision), nameof(ThermalVision.SetMaterialProperties));
    }

    private static readonly Dictionary<ThermalVision, RegisteredThermalDevice> _registeredThermalDevices = [];
    private static readonly Dictionary<ThermalVision, Material> _originalMaterials = [];
    private static readonly Dictionary<ThermalVision, Func<ThermalVision, bool>> _opticValidators = [];
    private static readonly HashSet<ThermalVision> _cachedInstances = [];
    private static readonly FieldInfo? _materialField = FindMaterialField();

    // ThermalVision's material field is private and its name differs between game builds, so it is
    // looked up by known names first and then by type.
    private static FieldInfo? FindMaterialField()
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (var fieldName in new[] { "_material", "material_0" })
        {
            var named = typeof(ThermalVision).GetField(fieldName, flags);

            if (named != null && named.FieldType == typeof(Material))
            {
                Plugin.logger!.LogInfo($"[WTT-CAG] ThermalVision material field: {named.Name}");
                return named;
            }
        }

        var candidates = new List<FieldInfo>();
        var allFields = new List<string>();

        foreach (var field in typeof(ThermalVision).GetFields(flags))
        {
            allFields.Add($"{field.FieldType.Name} {field.Name}");

            if (field.FieldType == typeof(Material)) { candidates.Add(field); }
        }

        if (candidates.Count == 1)
        {
            Plugin.logger!.LogInfo($"[WTT-CAG] ThermalVision material field: {candidates[0].Name}");
            return candidates[0];
        }

        Plugin.logger!.LogError($"[WTT-CAG] Could not identify ThermalVision's material field ({candidates.Count} candidates). Thermal materials will not apply. Fields: {string.Join(", ", allFields)}");

        return null;
    }

    public static bool Register(ThermalVision instance, Material material, EThermalDeviceType type, Func<ThermalVision, bool>? isCurrentOptic = null)
    {
        if (!instance || !material || _materialField == null)
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] Cannot register thermal material - instance: {(bool)instance} material: {(bool)material} material field: {_materialField != null}");
            return false;
        }

        if (type == EThermalDeviceType.HOLOSUN && isCurrentOptic == null)
        {
            Plugin.logger!.LogWarning("[WTT-CAG] Holosun registration requires a current optic validator.");
            return false;
        }

        if (!_originalMaterials.ContainsKey(instance) && _materialField.GetValue(instance) is Material original)
        {
            _originalMaterials.Add(instance, original);
        }

        _registeredThermalDevices[instance] = new RegisteredThermalDevice(material, type);

        if (isCurrentOptic != null)
        {
            _opticValidators[instance] = isCurrentOptic;
        }

        Plugin.logger!.LogInfo($"[WTT-CAG] Registered {type} on {instance.gameObject.name}.");

        return true;
    }

    public static void Unregister(ThermalVision instance)
    {
        _registeredThermalDevices.Remove(instance);
        _opticValidators.Remove(instance);
        _cachedInstances.Remove(instance);

        if (_originalMaterials.TryGetValue(instance, out var original) && instance && _materialField != null)
        {
            _materialField.SetValue(instance, original);
        }

        _originalMaterials.Remove(instance);
    }

    [PatchPrefix]
    public static void Prefix(ThermalVision __instance)
    {
        if (!_registeredThermalDevices.TryGetValue(__instance, out var device))
        {
            if (_cachedInstances.Add(__instance))
            {
                Plugin.logger!.LogDebug($"[WTT-CAG] ThermalVision found on {__instance.gameObject.name}.");
            }

            // The optic camera is pooled by the game rather than built by us, so the Holosun is
            // picked up here. The zone camera registers itself when it is built.
            if (!HolosunThermalOptic.TryAttach(__instance) || !_registeredThermalDevices.TryGetValue(__instance, out device))
            {
                return;
            }
        }

        if (device.DeviceType == EThermalDeviceType.FUSION_THERMAL_ZONE
            && __instance.gameObject.name != FusionThermalZoneCamera.CameraObjectName)
        {
            Unregister(__instance);
            return;
        }

        // The pooled optic camera is handed to whichever scope is being aimed, so the claim on it
        // is re-checked every frame and dropped the moment the sight is no longer ours.
        if (device.DeviceType == EThermalDeviceType.HOLOSUN
            && (!_opticValidators.TryGetValue(__instance, out var isCurrentOptic) || !isCurrentOptic(__instance)))
        {
            HolosunThermalOptic.Detach(__instance);
            Unregister(__instance);
            return;
        }

        if (_materialField == null)
        {
            return;
        }

        _materialField.SetValue(__instance, device.MaterialInstance);

        switch (device.DeviceType)
        {
            case EThermalDeviceType.HOLOSUN:
                // The optic is a clean image. The zone camera's sensor effects come from its profile instead.
                __instance.IsGlitch = false;
                __instance.IsPixelated = false;
                __instance.IsMotionBlurred = false;

                ThermalMaterialProperties.ApplyOptic(device.MaterialInstance, HolosunThermalOptic.ScopeMode);
                break;

            case EThermalDeviceType.FUSION_THERMAL_ZONE:
                ThermalMaterialProperties.ApplyZone(device.MaterialInstance, ThermalDeviceProfiles.Active);
                break;
        }
    }
}
