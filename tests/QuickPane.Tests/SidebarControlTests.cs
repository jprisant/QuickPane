using NUnit.Framework;
using QuickPane.UI;

namespace QuickPane.Tests
{
    /// <summary>Pins when the pane shows its profile row: only with a second profile to switch to, and
    /// never when the setting is off or the pane is collapsed to its strip.</summary>
    [TestFixture]
    public class SidebarControlTests
    {
        [Test]
        public void ProfileRow_OneProfile_IsHidden()
        {
            Assert.That(SidebarControl.ShouldShowProfileTabs(true, false, 1), Is.False);
        }

        [Test]
        public void ProfileRow_NoProfiles_IsHidden()
        {
            Assert.That(SidebarControl.ShouldShowProfileTabs(true, false, 0), Is.False);
        }

        [TestCase(2)]
        [TestCase(5)]
        public void ProfileRow_TwoOrMoreProfiles_Shows(int count)
        {
            Assert.That(SidebarControl.ShouldShowProfileTabs(true, false, count), Is.True);
        }

        [Test]
        public void ProfileRow_SettingOff_IsHiddenWhateverTheCount()
        {
            Assert.That(SidebarControl.ShouldShowProfileTabs(false, false, 3), Is.False);
        }

        [Test]
        public void ProfileRow_PaneCollapsed_IsHidden()
        {
            Assert.That(SidebarControl.ShouldShowProfileTabs(true, true, 3), Is.False);
        }
    }
}
