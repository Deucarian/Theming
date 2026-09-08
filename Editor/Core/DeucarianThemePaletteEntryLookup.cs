using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

using BuiltinRoleDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.BuiltinRoleDefinition;
using ThemeFamilyPresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemeFamilyPresetDefinition;
using ThemePresetDefinition = Deucarian.Theming.Editor.DeucarianDefaultThemeAssetFactory.ThemePresetDefinition;

namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemePaletteEntryLookup
    {
        internal static bool TryGetPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            out DeucarianColorEntry matchingEntry)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if (entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                {
                    matchingEntry = entry;
                    return true;
                }
            }

            matchingEntry = null;
            return false;
        }

        internal static bool PaletteHasEntryForRole(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            return TryGetPaletteEntry(palette, role, out _);
        }

        internal static bool PaletteRoleColorIsMissing(DeucarianColorPalette palette, DeucarianColorRole role)
        {
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                if (entry == null || entry.Role == null)
                {
                    continue;
                }

                if ((entry.Role == role || string.Equals(entry.Role.Id, role.Id, StringComparison.Ordinal))
                    && IsPackageMissingColor(entry.Color))
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - Color.magenta.r) <= tolerance
                && Mathf.Abs(color.g - Color.magenta.g) <= tolerance
                && Mathf.Abs(color.b - Color.magenta.b) <= tolerance
                && Mathf.Abs(color.a - Color.magenta.a) <= tolerance;
        }
    }
}
