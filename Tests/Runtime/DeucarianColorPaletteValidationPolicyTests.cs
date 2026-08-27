using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianColorPaletteValidationPolicyTests
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
        public void DuplicateRoleIdsAreReportedOnceInFirstDuplicateOrder()
        {
            DeucarianColorRole firstRole = CreateRole("deucarian.test.first", "First");
            DeucarianColorRole secondRole = CreateRole("deucarian.test.second", "Second");
            DeucarianColorRole firstDuplicate = CreateRole("deucarian.test.first", "First Duplicate");
            DeucarianColorRole secondDuplicate = CreateRole("deucarian.test.second", "Second Duplicate");
            DeucarianColorRole repeatedFirstDuplicate = CreateRole(
                "deucarian.test.first",
                "Repeated First Duplicate");
            List<DeucarianColorEntry> entries = new List<DeucarianColorEntry>
            {
                new DeucarianColorEntry(firstRole, Color.red),
                new DeucarianColorEntry(secondRole, Color.green),
                new DeucarianColorEntry(firstDuplicate, Color.blue),
                new DeucarianColorEntry(secondDuplicate, Color.yellow),
                new DeucarianColorEntry(repeatedFirstDuplicate, Color.cyan)
            };

            List<string> duplicateIds =
                DeucarianColorPaletteValidationPolicy.GetDuplicateRoleIds(entries);

            CollectionAssert.AreEqual(
                new[]
                {
                    "deucarian.test.first",
                    "deucarian.test.second"
                },
                duplicateIds);
        }

        [Test]
        public void WarningsPreserveEntryOrderAndAppendDuplicateWarnings()
        {
            DeucarianColorRole invalidRole = CreateRole("Deucarian.Test.Invalid", "Invalid Role");
            DeucarianColorRole duplicateRole = CreateRole("deucarian.test.duplicate", "Duplicate");
            DeucarianColorRole repeatedRole = CreateRole("deucarian.test.duplicate", "Repeated");
            List<DeucarianColorEntry> entries = new List<DeucarianColorEntry>
            {
                null,
                new DeucarianColorEntry(null, Color.white),
                new DeucarianColorEntry(invalidRole, Color.red),
                new DeucarianColorEntry(duplicateRole, Color.green),
                new DeucarianColorEntry(repeatedRole, Color.blue)
            };

            List<string> warnings =
                DeucarianColorPaletteValidationPolicy.GetWarnings(entries);

            CollectionAssert.AreEqual(
                new[]
                {
                    "Entry 0 is null.",
                    "Entry 1 has no role.",
                    "Invalid Role: Role ID should use lowercase-friendly stable identifiers.",
                    "Duplicate palette entry for role ID: deucarian.test.duplicate"
                },
                warnings);
        }

        [Test]
        public void NullEntryCollectionHasNoFindings()
        {
            CollectionAssert.IsEmpty(
                DeucarianColorPaletteValidationPolicy.GetDuplicateRoleIds(null));
            CollectionAssert.IsEmpty(
                DeucarianColorPaletteValidationPolicy.GetWarnings(null));
        }

        private DeucarianColorRole CreateRole(string id, string name)
        {
            DeucarianColorRole role = ScriptableObject.CreateInstance<DeucarianColorRole>();
            role.name = name;
            role.Configure(id, name, "Tests", string.Empty, Color.white, false);
            createdObjects.Add(role);
            return role;
        }
    }
}
