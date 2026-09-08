using NUnit.Framework;
using UnityEngine.UIElements;
using Deucarian.Theming.Editor;

namespace Deucarian.Theming.Tests
{
    public sealed class DeucarianThemeManagerToolbarTests
    {
        [TestCase(0, false, false)]
        [TestCase(1, false, true)]
        [TestCase(1, true, false)]
        public void PendingChangesControlDiscardWithoutChangingReservedLayout(int count, bool playing, bool enabled)
        {
            var root = new VisualElement();
            var toolbar = new DeucarianThemeManagerToolbar(root, () => { }, () => { }, () => { },
                () => { }, () => { }, () => { });
            toolbar.SetPendingChanges(count, playing);
            Button discard = root.Q<Button>("deucarian-theme-manager-discard-changes");
            Assert.That(discard.enabledSelf, Is.EqualTo(enabled));
            Assert.That(discard.parent, Is.Not.Null);
            Assert.That(discard.tooltip, Is.Not.Empty);
        }

        [Test]
        public void ActiveStatusAndActionShareOneSlotAndCanSwitchRepeatedly()
        {
            var root = new VisualElement();
            var toolbar = new DeucarianThemeManagerToolbar(root, () => { }, () => { }, () => { },
                () => { }, () => { }, () => { });
            Button action = root.Q<Button>("deucarian-theme-manager-toolbar-primary");
            VisualElement slot = action.parent;
            toolbar.ShowActive();
            Assert.That(action.parent, Is.Null);
            Assert.That(slot.childCount, Is.EqualTo(1));
            Assert.That(slot[0].name, Is.EqualTo("deucarian-theme-manager-toolbar-primary-status"));
            toolbar.SetPrimary("Activate", false, "Choose a theme first.");
            Assert.That(action.parent, Is.SameAs(slot));
            Assert.That(slot.childCount, Is.EqualTo(1));
            Assert.That(action.enabledSelf, Is.False);
            Assert.That(action.tooltip, Is.EqualTo("Choose a theme first."));
            toolbar.SetSelection(true, false, false, false);
            Assert.That(root.Q<Button>("deucarian-theme-manager-view-style").enabledSelf, Is.False);
        }
    }
}
