using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;

namespace WTT_CAG.Client.Content.Thermals.Fusion;

// Raid countdown via reflection, so a renamed member logs once and blanks the clock instead of breaking the plugin.
public static class RaidClock
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    private static readonly Dictionary<string, MemberInfo?> Members = [];
    private static bool _loggedFailure;

    public static TimeSpan? Remaining
    {
        get
        {
            var game = Singleton<AbstractGame>.Instance;

            if (!game) { return null; }

            var timer = Read(game, "GameTimer");

            if (timer == null) { return null; }

            if (Read(timer, "SessionTime") is not TimeSpan session || Read(timer, "PastTime") is not TimeSpan past)
            {
                return null;
            }

            var remaining = session - past;

            return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
        }
    }

    private static object? Read(object target, string name)
    {
        var type = target.GetType();
        var key = $"{type.FullName}.{name}";

        if (!Members.TryGetValue(key, out var member))
        {
            member = (MemberInfo?)type.GetProperty(name, Flags) ?? type.GetField(name, Flags);
            Members[key] = member;

            if (member == null) { LogMissing(type, name); }
        }

        try
        {
            return member switch
            {
                PropertyInfo property => property.GetValue(target),
                FieldInfo field => field.GetValue(target),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static void LogMissing(Type type, string name)
    {
        if (_loggedFailure) { return; }

        _loggedFailure = true;

        var names = new List<string>();

        foreach (var member in type.GetMembers(Flags))
        {
            if (member is PropertyInfo or FieldInfo) { names.Add(member.Name); }
        }

        Plugin.logger!.LogError($"[WTT-CAG] Raid clock: '{name}' not found on {type.Name}. The mission time will stay blank. Members: {string.Join(", ", names)}");
    }
}
