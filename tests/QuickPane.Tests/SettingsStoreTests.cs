using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using NUnit.Framework;
using QuickPane.Models;
using QuickPane.Services;

namespace QuickPane.Tests
{
    /// <summary>
    /// Exercises SettingsStore entirely inside a throwaway temp directory, using the internal
    /// (string dir) constructor added for tests. Never uses the public default constructor, which
    /// hard-codes the real %APPDATA%\QuickPane. GroupsPath and the one profile's GroupsPath both point
    /// inside the temp dir, because Load() creates that folder and LoadActiveToTop() copies the active
    /// profile's path over the top-level one.
    /// </summary>
    [TestFixture]
    public class SettingsStoreTests
    {
        private static void WriteSettingsFile(string dir, string groupsPath)
        {
            var settings = AppSettings.CreateDefault();
            settings.GroupsPath = groupsPath;
            settings.Profiles = new List<Profile>
            {
                new Profile { Name = "Profile 1", GroupsPath = groupsPath, RecentsMaxCount = 15, SidebarWidthPx = 220 }
            };

            var file = Path.Combine(dir, "settings.json");
            using (var fs = File.Create(file))
            {
                var ser = new DataContractJsonSerializer(typeof(AppSettings));
                ser.WriteObject(fs, settings);
            }
        }

        [Test]
        public void Load_ReadsGroupsPathFromTheActiveProfile()
        {
            using (var temp = new TempDir())
            {
                var groupsPath = Path.Combine(temp.Path, "Groups");
                WriteSettingsFile(temp.Path, groupsPath);

                var store = new SettingsStore(temp.Path);
                store.Load();

                Assert.That(store.Current.GroupsPath, Is.EqualTo(groupsPath));
                Assert.That(Directory.Exists(groupsPath), Is.True); // Load() materializes it
            }
        }

        [Test]
        public void SaveThenLoad_RoundTripsAChangedValue()
        {
            using (var temp = new TempDir())
            {
                var groupsPath = Path.Combine(temp.Path, "Groups");
                WriteSettingsFile(temp.Path, groupsPath);

                var store = new SettingsStore(temp.Path);
                store.Load();
                store.Current.RecentsMaxCount = 33;
                store.Save();

                var reloaded = new SettingsStore(temp.Path);
                reloaded.Load();

                Assert.That(reloaded.Current.RecentsMaxCount, Is.EqualTo(33));
            }
        }
    }
}
