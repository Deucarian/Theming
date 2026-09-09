using NUnit.Framework;
using UnityEngine.UIElements;
using Deucarian.Editor;
using Deucarian.Theming.Editor;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianThemeManagerToolbarTests
    {
        [TestCase(0, false, false)]
        [TestCase(1, false, true)]
        [TestCase(1, true, false)]
        public void PendingChangesExposeDiscardOnlyWhenNeeded(int count, bool playing, bool enabled)
        {
            using (var workspace = new DeucarianEditorWorkspace(new VisualElement(), "Test"))
            {
                var toolbar = Create(workspace);
                toolbar.SetPendingChanges(count, playing);
                var discard = workspace.PageActions.Q<Button>("deucarian-theme-manager-discard-changes");
                Assert.AreEqual(enabled, discard.enabledSelf);
                Assert.AreEqual(count > 0 ? DisplayStyle.Flex : DisplayStyle.None, discard.style.display.value);
            }
        }

        [Test]
        public void ActiveStateRetainsActionIdentityAndCanSwitchRepeatedly()
        {
            using (var workspace = new DeucarianEditorWorkspace(new VisualElement(), "Test"))
            {
                var toolbar = Create(workspace);
                var action = workspace.PageActions.Q<Button>("deucarian-theme-manager-toolbar-primary");
                toolbar.ShowActive();
                Assert.AreSame(workspace.PageActions, action.parent);
                Assert.AreEqual("Active in project", action.text);
                Assert.IsFalse(action.enabledSelf);
                toolbar.SetPrimary("Activate", false, "Choose a theme first.");
                Assert.AreSame(action, workspace.PageActions.Q<Button>("deucarian-theme-manager-toolbar-primary"));
                Assert.AreEqual("Apply to project", action.text);
                Assert.AreEqual("Choose a theme first.", action.tooltip);
                toolbar.SetSelection(true, false, false, false);
                Assert.IsFalse(workspace.Tabs.Q<Button>("deucarian-theme-manager-view-style").enabledSelf);
            }
        }

        private static DeucarianThemeManagerToolbar Create(DeucarianEditorWorkspace workspace)
            => new DeucarianThemeManagerToolbar(workspace, () => { }, () => { }, () => { }, () => { }, () => { }, () => { });
    }
}
