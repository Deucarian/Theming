using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianColorPaletteTests
    {
        private readonly List<UnityEngine.Object> createdObjects = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void PaletteReturnsExplicitColorForRole()
        {
            DeucarianColorRole role = CreateRole("deucarian.test.primary", Color.black);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.AddEntry(role, Color.red);

            Assert.AreEqual(Color.red, palette.GetColor(role));
        }

        [Test]
        public void PaletteFallsBackToRoleDefaultColor()
        {
            DeucarianColorRole role = CreateRole("deucarian.test.default", Color.green);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();

            Assert.IsTrue(palette.TryGetColor(role, out Color color));
            Assert.AreEqual(Color.green, color);
        }

        [Test]
        public void PairedRoleKeepsDarkAsLegacyDefault()
        {
            DeucarianColorRole role = CreatePairedRole(
                "deucarian.test.paired",
                Color.white,
                Color.black);

            Assert.IsTrue(role.HasPairedDefaultColors);
            Assert.AreEqual(Color.white, role.LightDefaultColor);
            Assert.AreEqual(Color.black, role.DarkDefaultColor);
            Assert.AreEqual(Color.black, role.DefaultColor);
            Assert.AreEqual(Color.white, role.GetDefaultColor(DeucarianThemeMode.Light));
            Assert.AreEqual(Color.black, role.GetDefaultColor(DeucarianThemeMode.Dark));
        }

        [Test]
        public void PaletteVariantUsesMatchingPairedRoleDefault()
        {
            DeucarianColorRole role = CreatePairedRole(
                "deucarian.test.variant",
                Color.white,
                Color.black);
            DeucarianColorRoleLibrary library = CreateAsset<DeucarianColorRoleLibrary>();
            library.AddRole(role);
            DeucarianColorPalette lightPalette = CreateAsset<DeucarianColorPalette>();
            DeucarianColorPalette darkPalette = CreateAsset<DeucarianColorPalette>();
            lightPalette.Configure("deucarian.test.palette.light", "Light", library, DeucarianThemeMode.Light);
            darkPalette.Configure("deucarian.test.palette.dark", "Dark", library, DeucarianThemeMode.Dark);

            Assert.IsTrue(lightPalette.HasThemeMode);
            Assert.AreEqual(DeucarianThemeMode.Light, lightPalette.ThemeMode);
            Assert.AreEqual(Color.white, lightPalette.GetColor(role));
            Assert.AreEqual(Color.black, darkPalette.GetColor(role));
            Assert.IsTrue(lightPalette.TryGetColorById(role.Id, out Color lightById));
            Assert.AreEqual(Color.white, lightById);
        }

        [Test]
        public void PaletteVariantResetAndAddMissingUseMatchingDefaults()
        {
            DeucarianColorRole role = CreatePairedRole(
                "deucarian.test.variant-tools",
                Color.yellow,
                Color.blue);
            DeucarianColorRoleLibrary library = CreateAsset<DeucarianColorRoleLibrary>();
            library.AddRole(role);
            DeucarianColorPalette lightPalette = CreateAsset<DeucarianColorPalette>();
            lightPalette.Configure("deucarian.test.palette.light", "Light", library, DeucarianThemeMode.Light);
            lightPalette.AddEntry(role, Color.red);

            Assert.IsTrue(lightPalette.ResetEntryToRoleDefault(0));
            Assert.AreEqual(Color.yellow, lightPalette.Entries[0].Color);

            lightPalette.ClearEntries();
            Assert.AreEqual(1, lightPalette.AddMissingRolesFromLibrary());
            Assert.AreEqual(Color.yellow, lightPalette.Entries[0].Color);
        }

        [Test]
        public void LegacyPaletteUsesDarkCompatibleDefaultForPairedRole()
        {
            DeucarianColorRole role = CreatePairedRole(
                "deucarian.test.legacy-palette",
                Color.white,
                Color.black);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.Configure("deucarian.test.palette", "Legacy", null);

            Assert.IsFalse(palette.HasThemeMode);
            Assert.AreEqual(DeucarianThemeMode.Dark, palette.ThemeMode);
            Assert.AreEqual(Color.black, palette.GetColor(role));
        }

        [Test]
        public void PaletteReturnsMagentaForNullRole()
        {
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();

            Assert.IsFalse(palette.TryGetColor(null, out Color color));
            Assert.AreEqual(DeucarianColorPalette.MissingColor, color);
            Assert.AreEqual(DeucarianColorPalette.MissingColor, palette.GetColor(null));
        }

        [Test]
        public void LookupByRoleIdWorks()
        {
            DeucarianColorRole role = CreateRole("deucarian.test.lookup", Color.gray);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.AddEntry(role, Color.cyan);

            Assert.IsTrue(palette.TryGetColorById("deucarian.test.lookup", out Color color));
            Assert.AreEqual(Color.cyan, color);
        }

        [Test]
        public void DuplicateHandlingIsDeterministic()
        {
            DeucarianColorRole role = CreateRole("deucarian.test.duplicate", Color.white);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            palette.AddEntry(role, Color.red);
            palette.AddEntry(role, Color.blue);

            Assert.AreEqual(Color.red, palette.GetColor(role));
            CollectionAssert.Contains(palette.GetDuplicateEntryRoleIds(), "deucarian.test.duplicate");
        }

        [Test]
        public void ThemeDelegatesColorLookupToPalette()
        {
            DeucarianColorRole role = CreateRole("deucarian.test.theme", Color.white);
            DeucarianColorPalette palette = CreateAsset<DeucarianColorPalette>();
            DeucarianTheme theme = CreateAsset<DeucarianTheme>();

            palette.AddEntry(role, Color.yellow);
            theme.Configure("deucarian.theme.test", "Test Theme", palette);

            Assert.AreEqual(Color.yellow, theme.GetColor(role));
        }

        private DeucarianColorRole CreateRole(string id, Color defaultColor)
        {
            DeucarianColorRole role = CreateAsset<DeucarianColorRole>();
            role.name = id;
            role.Configure(id, id, "Tests", string.Empty, defaultColor, false);
            return role;
        }

        private DeucarianColorRole CreatePairedRole(string id, Color lightColor, Color darkColor)
        {
            DeucarianColorRole role = CreateAsset<DeucarianColorRole>();
            role.name = id;
            role.Configure(id, id, "Tests", string.Empty, lightColor, darkColor, false);
            return role;
        }

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }
    }
}
