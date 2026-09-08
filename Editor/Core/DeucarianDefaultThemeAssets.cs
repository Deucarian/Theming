using System;
using System.Collections.Generic;
using System.IO;
using Deucarian.Editor;
using Deucarian.Theming;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Theming.Editor
{
    /// <summary>
    /// Result object returned by theme preset asset creation.
    /// </summary>
    public sealed class DeucarianDefaultThemeAssets
    {
        private readonly List<DeucarianColorRole> roles = new List<DeucarianColorRole>();
        private readonly List<DeucarianThemeStyle> styles = new List<DeucarianThemeStyle>();

        public DeucarianColorRoleLibrary RoleLibrary { get; internal set; }
        public DeucarianThemeFamily ThemeFamily { get; internal set; }
        public DeucarianColorPalette LightPalette { get; internal set; }
        public DeucarianColorPalette DarkPalette { get; internal set; }
        public DeucarianTheme LightTheme { get; internal set; }
        public DeucarianTheme DarkTheme { get; internal set; }

        /// <summary>
        /// Backward-compatible primary palette. Paired workflows expose the dark palette here.
        /// </summary>
        public DeucarianColorPalette Palette { get; internal set; }

        /// <summary>
        /// Backward-compatible primary theme. Paired workflows expose the dark theme here.
        /// </summary>
        public DeucarianTheme Theme { get; internal set; }
        public DeucarianThemeStyle DefaultStyle { get; internal set; }
        public IReadOnlyList<DeucarianColorRole> Roles => roles;
        public IReadOnlyList<DeucarianThemeStyle> Styles => styles;

        internal void AddRole(DeucarianColorRole role)
        {
            if (role != null)
            {
                roles.Add(role);
            }
        }

        internal void AddStyle(DeucarianThemeStyle style)
        {
            if (style != null)
            {
                styles.Add(style);
            }
        }
    }
}
