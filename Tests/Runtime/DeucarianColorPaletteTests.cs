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

        private T CreateAsset<T>()
            where T : ScriptableObject
        {
            T asset = ScriptableObject.CreateInstance<T>();
            createdObjects.Add(asset);
            return asset;
        }
    }
}
