using NUnit.Framework;
using WebAPI.Auth;

namespace WebAPI.Tests.Auth
{
    [TestFixture]
    public class SessionStampTests
    {
        private const string Hash = "$2a$04$abcdefghijklmnopqrstuuq8kR4o8i6FXV0zUe5yU2ulOQ1n3xFvG";

        [Test]
        public void SameHash_GivesTheSameStamp_WithoutRevealingIt()
        {
            var stamp = SessionStamp.For(Hash);

            Assert.That(SessionStamp.For(Hash), Is.EqualTo(stamp));
            Assert.That(stamp, Has.Length.EqualTo(22));
            Assert.That(Hash, Does.Not.Contain(stamp));
        }

        [Test]
        public void ChangedHash_NoLongerMatches()
        {
            var stamp = SessionStamp.For(Hash);

            Assert.That(SessionStamp.Matches(stamp, Hash), Is.True);
            Assert.That(SessionStamp.Matches(stamp, Hash + "x"), Is.False);
        }

        [TestCase(null, Hash)]
        [TestCase("stamp", null)]
        public void MissingValues_DoNotMatch(string stamp, string hash)
        {
            Assert.That(SessionStamp.Matches(stamp, hash), Is.False);
        }
    }
}
