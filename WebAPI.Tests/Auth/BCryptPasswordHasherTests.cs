using System;
using NUnit.Framework;
using WebAPI.Auth;

namespace WebAPI.Tests.Auth
{
    [TestFixture]
    public class BCryptPasswordHasherTests
    {
        // Lowest bcrypt cost, to keep the tests fast.
        private readonly BCryptPasswordHasher hasher = new BCryptPasswordHasher(4);

        [Test]
        public void Hash_IsNotThePassword_AndVerifies()
        {
            var hash = hasher.Hash("correct horse");

            Assert.That(hash, Does.Not.Contain("correct horse"));
            Assert.That(hasher.IsHashed(hash), Is.True);
            Assert.That(hasher.Verify("correct horse", hash), Is.True);
        }

        [Test]
        public void WrongPassword_DoesNotVerify()
        {
            Assert.That(hasher.Verify("wrong horse", hasher.Hash("correct horse")), Is.False);
        }

        [Test]
        public void SamePassword_HashesDifferentlyEachTime()
        {
            Assert.That(hasher.Hash("correct horse"), Is.Not.EqualTo(hasher.Hash("correct horse")));
        }

        [Test]
        public void CharactersBeyondBcrypts72ByteLimit_StillCount()
        {
            var prefix = new string('a', 72);

            Assert.That(hasher.Verify(prefix + "X", hasher.Hash(prefix + "Y")), Is.False);
        }

        [Test]
        public void UnknownUser_DoesNotVerify()
        {
            Assert.That(hasher.Verify("anything", null), Is.False);
        }

        [TestCase("plain-text-password")]
        [TestCase("")]
        public void PlainTextStoredValue_IsNotAHash_AndDoesNotVerify(string stored)
        {
            Assert.That(hasher.IsHashed(stored), Is.False);
            Assert.That(hasher.Verify(stored, stored), Is.False);
        }

        [Test]
        public void NullPassword_DoesNotVerify()
        {
            Assert.That(hasher.Verify(null, hasher.Hash("correct horse")), Is.False);
        }

        [TestCase(3)]
        [TestCase(32)]
        public void OutOfRangeWorkFactor_IsRefused(int workFactor)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BCryptPasswordHasher(workFactor));
        }
    }
}
