using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;


namespace Deucarian.Theming.Editor
{
    internal static class DeucarianThemePackPaletteRepair
    {
        internal static bool RepairPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            bool overwriteExisting)
        {
            return RepairPaletteEntry(
                palette,
                role,
                definition,
                definition != null ? definition.DefaultColor : default(Color),
                overwriteExisting);
        }

        internal static bool RepairPaletteEntry(
            DeucarianColorPalette palette,
            DeucarianColorRole role,
            DeucarianThemePackRole definition,
            Color desiredColor,
            bool overwriteExisting)
        {
            if (palette == null || role == null || definition == null)
            {
                return false;
            }

            if (overwriteExisting
                || !TryGetPaletteEntry(palette, role.Id, out DeucarianColorRole entryRole, out Color entryColor)
                || entryRole == null
                || IsPackageMissingColor(entryColor))
            {
                palette.SetColor(role, desiredColor, definition.Description);
                return true;
            }

            if (entryRole != role)
            {
                palette.SetColor(role, entryColor, definition.Description);
                return true;
            }

            return false;
        }

        internal static bool TryGetPaletteEntry(
            DeucarianColorPalette palette,
            string roleId,
            out DeucarianColorRole role,
            out Color color)
        {
            role = null;
            color = DeucarianColorPalette.MissingColor;
            if (palette == null)
            {
                return false;
            }

            string normalizedId = DeucarianColorRole.NormalizeId(roleId);
            IReadOnlyList<DeucarianColorEntry> entries = palette.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                DeucarianColorEntry entry = entries[i];
                DeucarianColorRole entryRole = entry != null ? entry.Role : null;
                if (entryRole == null || !string.Equals(entryRole.Id, normalizedId, StringComparison.Ordinal))
                {
                    continue;
                }

                role = entryRole;
                color = entry.Color;
                return true;
            }

            return false;
        }

        internal static bool IsPackageMissingColor(Color color)
        {
            const float tolerance = 0.0001f;
            return Mathf.Abs(color.r - DeucarianColorPalette.MissingColor.r) <= tolerance
                && Mathf.Abs(color.g - DeucarianColorPalette.MissingColor.g) <= tolerance
                && Mathf.Abs(color.b - DeucarianColorPalette.MissingColor.b) <= tolerance
                && Mathf.Abs(color.a - DeucarianColorPalette.MissingColor.a) <= tolerance;
        }
    }
}
