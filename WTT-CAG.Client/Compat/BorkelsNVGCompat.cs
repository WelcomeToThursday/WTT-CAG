using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using Newtonsoft.Json.Linq;
using UnityEngine;
using WTT_CAG.Client.Compat.Patches;
using WTT_CAG.Client.Core;
namespace WTT_CAG.Client.Compat;

public static class BorkelsNVGCompat
{
    public const string GUID = "com.borkel.nvgmasks";

    public static bool Active { get; private set; }

    private static readonly Dictionary<string, string> _folderByItem = new();
    private static readonly Dictionary<string, Texture2D> _textureCache = new();
    private static readonly HashSet<string> _reportedMissing = [];
    private static readonly HashSet<string> _reportedUnmatched = [];
    private static bool _reportedFailure;

    public static bool TryEnable()
    {
        if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info))
        {
            Active = false;
            return false;
        }

        try
        {
            IndexFolders(Path.Combine(Path.GetDirectoryName(info.Location) ?? "", "Assets", "NVG"));
            new ConfigureRuntimePatch().Enable();
            Active = true;
        }
        catch (Exception e)
        {
            Plugin.logger!.LogError(e.ToString());
            Active = false;
        }

        return Active;
    }

    internal static void ConfigurePrefix(ref Texture __1, ref Texture __2)
    {
        try
        {
            if (TrySwapTextures(ref __1, ref __2))
            {
                return;
            }

            if (NVGPodRotator.ActiveProfile == null || NVGPodRotator.StowedPod is not (NVGPodRotator.PodSide.Left or NVGPodRotator.PodSide.Right))
            {
                return;
            }
            
            var itemId = GetWornNVGItemId();
            if (itemId != null && _reportedUnmatched.Add(itemId))
            {
                Plugin.logger!.LogWarning($"[WTT-CAG] No Borkel NVG asset folder matches {itemId}; keeping Borkel's textures.");
            }
        }
        catch (Exception e)
        {
            if (!_reportedFailure)
            {
                _reportedFailure = true;
                Plugin.logger!.LogError($"[WTT-CAG] Borkel texture swap failed: {e}");
            }
        }
    }

    private static void IndexFolders(string nvgRoot)
    {
        if (!Directory.Exists(nvgRoot))
        {
            Plugin.logger!.LogWarning($"[WTT-CAG] Borkel NVG assets not found at {nvgRoot}.");
            return;
        }

        foreach (var folder in Directory.EnumerateDirectories(nvgRoot))
        {
            var configPath = Path.Combine(folder, "config.json");

            if (!File.Exists(configPath))
            {
                continue;
            }

            try
            {
                var config = JObject.Parse(File.ReadAllText(configPath));
                var itemId = (string?)config["itemId"];

                if (!string.IsNullOrWhiteSpace(itemId))
                {
                    _folderByItem[itemId] = folder;
                }

                if (config["itemIds"] is JArray itemIds)
                {
                    foreach (var value in itemIds)
                    {
                        itemId = (string?)value;
                        if (!string.IsNullOrWhiteSpace(itemId)) _folderByItem[itemId] = folder;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.logger!.LogWarning($"[WTT-CAG] Could not read {configPath}: {e.Message}");
            }
        }
    }

    private static bool TrySwapTextures(ref Texture lensTexture, ref Texture maskTexture)
    {
        bool result;

        if (NVGPodRotator.ActiveProfile == null)
        {
            result = false;
        }
        else
        {
            var activeSide = NVGPodRotator.StowedPod switch
            {
                NVGPodRotator.PodSide.Right => "left", // Right pod is stowed, left pod is active
                NVGPodRotator.PodSide.Left => "right", // Left pod is stowed, right pod is active
                _ => null
            };

            var itemId = GetWornNVGItemId();

            if (itemId == null || !_folderByItem.TryGetValue(itemId, out var folder) || activeSide == null)
            {
                result = false;
            }
            else
            {
                var lens = LoadTextureOrNull(Path.Combine(folder, $"lens_{activeSide}.png"));
                if (lens)
                {
                    lensTexture = lens!;
                }

                var mask = LoadTextureOrNull(Path.Combine(folder, $"mask_{activeSide}.png"));
                if (mask)
                {
                    maskTexture = mask!;
                }

                result = true;
            }
        }

        return result;
    }

    private static string? GetWornNVGItemId()
    {
        var player = Singleton<GameWorld>.Instance?.MainPlayer;
        return player?.NightVisionObserver?.Component?.Item?.StringTemplateId;
    }

    private static Texture2D? LoadTextureOrNull(string texturePath)
    {
        if (_textureCache.TryGetValue(texturePath, out var texture) && texture)
        {
            return texture;
        }

        if (_reportedMissing.Contains(texturePath))
        {
            return null;
        }

        if (!File.Exists(texturePath))
        {
            if (_reportedMissing.Add(texturePath))
            {
                Plugin.logger!.LogWarning($"[WTT-CAG] {texturePath} not found. Flipped pod will maintain Borkel's original texture.");
            }

            return null;
        }

        try
        {
            var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);

            if (!tex.LoadImage(File.ReadAllBytes(texturePath)))
            {
                UnityEngine.Object.Destroy(tex);

                if (_reportedMissing.Add(texturePath))
                {
                    Plugin.logger!.LogWarning($"[WTT-CAG] {texturePath} does not contain a readable PNG. Flipped pod will maintain Borkel's original texture.");
                }

                return null;
            }

            tex.name = Path.GetFileNameWithoutExtension(texturePath);
            tex.wrapMode = TextureWrapMode.Clamp;

            _textureCache[texturePath] = tex;

            return tex;
        }
        catch(Exception e)
        {
            if (_reportedMissing.Add(texturePath))
            {
                Plugin.logger!.LogWarning($"[WTT-CAG] Failed to load {texturePath}: {e.Message}. Flipped pod will maintain Borkel's original texture.");
            }

            return null;
        }
    }
}
