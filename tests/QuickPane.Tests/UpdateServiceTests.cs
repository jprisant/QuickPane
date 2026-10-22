using System;
using NUnit.Framework;
using QuickPane.Services;

namespace QuickPane.Tests
{
    /// <summary>Pins TryParseTag's contract: the version can sit anywhere in the tag string, and a
    /// tag with no recognizable version returns false rather than throwing.</summary>
    [TestFixture]
    public class UpdateServiceTests
    {
        [Test]
        public void TryParseTag_PlainVersionTag_Parses()
        {
            Version v;
            bool ok = UpdateService.TryParseTag("v3.9.0", out v);

            Assert.That(ok, Is.True);
            Assert.That(v, Is.EqualTo(new Version(3, 9, 0)));
        }

        [Test]
        public void TryParseTag_VersionBuriedInInstallerName_Parses()
        {
            Version v;
            bool ok = UpdateService.TryParseTag("QuickPaneSetup-3.7.0-release", out v);

            Assert.That(ok, Is.True);
            Assert.That(v, Is.EqualTo(new Version(3, 7, 0)));
        }

        [Test]
        public void TryParseTag_Junk_Fails()
        {
            Version v;
            Assert.That(UpdateService.TryParseTag("junk", out v), Is.False);
        }

        [Test]
        public void TryParseTag_Empty_Fails()
        {
            Version v;
            Assert.That(UpdateService.TryParseTag("", out v), Is.False);
        }

        [Test]
        public void TryParseTag_Null_Fails()
        {
            Version v;
            Assert.That(UpdateService.TryParseTag(null, out v), Is.False);
        }
    }
}
