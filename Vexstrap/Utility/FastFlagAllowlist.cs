using System;
using System.Collections.Generic;

namespace Vexstrap.Utility
{
    /// <summary>
    /// Flag names from Roblox's DevForum post "Allowlist for local client configuration via Fast Flags"
    /// (September 2025). Roblox says this list can change at any time, so this is a reference, not a guarantee.
    /// Only player flags are covered. Roblox Studio flags are not affected by the allowlist.
    /// </summary>
    internal static class FastFlagAllowlist
    {
        public const string SourceUrl = "https://devforum.roblox.com/t/allowlist-for-local-client-configuration-via-fast-flags/3966569";
        public const string LastChecked = "2026-10-09";

        private static readonly HashSet<string> _names = new(StringComparer.Ordinal)
        {
            "DFIntCSGLevelOfDetailSwitchingDistance",
            "DFIntCSGLevelOfDetailSwitchingDistanceL12",
            "DFIntCSGLevelOfDetailSwitchingDistanceL23",
            "DFIntCSGLevelOfDetailSwitchingDistanceL34",
            "FFlagHandleAltEnterFullscreenManually",
            "DFFlagTextureQualityOverrideEnabled",
            "DFIntTextureQualityOverride",
            "FIntDebugForceMSAASamples",
            "DFFlagDisableDPIScale",
            "FFlagDebugGraphicsPreferD3D11",
            "FFlagDebugSkyGray",
            "DFFlagDebugPauseVoxelizer",
            "DFIntDebugFRMQualityLevelOverride",
            "FIntFRMMaxGrassDistance",
            "FIntFRMMinGrassDistance",
            "FFlagDebugGraphicsPreferVulkan",
            "FFlagDebugGraphicsPreferOpenGL",
            "FIntGrassMovementReducedMotionFactor"
        };

        public static bool IsListed(string name) => !string.IsNullOrEmpty(name) && _names.Contains(name);

        // Heuristic: Studio flags are outside the allowlist, so these are skipped in the check.
        public static bool IsStudioFlag(string flagName)
        {
            if (string.IsNullOrEmpty(flagName)) return false;
            return flagName.StartsWith("FFlagStudio", StringComparison.OrdinalIgnoreCase)
                || flagName.StartsWith("DFFlagStudio", StringComparison.OrdinalIgnoreCase)
                || flagName.StartsWith("FStudio", StringComparison.OrdinalIgnoreCase);
        }

        public static string GetCompatibilityNote(string flagName)
        {
            if (IsListed(flagName))
                return $"{flagName} matches Roblox's published Fast Flag allowlist (last checked {LastChecked}).";

            return $"{flagName} isn't on that list and may be ignored.";
        }
    }
}