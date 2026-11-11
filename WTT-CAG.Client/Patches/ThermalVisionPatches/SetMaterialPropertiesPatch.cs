using System.Collections.Generic;
using System;
using System.Reflection;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;
using WTT_CAG.Client.Content.Thermals.Fusion;
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
    private static readonly FieldInfo? _materialField = typeof(ThermalVision).GetField("material_0", BindingFlags.NonPublic | BindingFlags.Instance);
    internal static bool _equippedFusionNVG = false;
    internal static bool _equippedHolosunThermal = false;

    public static bool Register(ThermalVision instance, Material material, EThermalDeviceType type, Func<ThermalVision, bool>? isCurrentOptic = null)
    {
        if (!instance || !material || _materialField == null)
        {
            Plugin.logger!.LogWarning("[WTT-CAG] Cannot register thermal material: instance, material or material_0 field is missing.");
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

            return;
        }

        if (device.DeviceType == EThermalDeviceType.FUSION_THERMAL_ZONE
            && __instance.gameObject.name != FusionThermalZoneCamera.CameraObjectName)
        {
            Unregister(__instance);
            return;
        }

        if (device.DeviceType == EThermalDeviceType.HOLOSUN
            && (!_opticValidators.TryGetValue(__instance, out var isCurrentOptic) || !isCurrentOptic(__instance)))
        {
            Unregister(__instance);
            return;
        }

        if (_materialField == null)
        {
            return;
        }

        _materialField.SetValue(__instance, device.MaterialInstance);
    }
}
