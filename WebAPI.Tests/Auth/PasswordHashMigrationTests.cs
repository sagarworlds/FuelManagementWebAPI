using System.Linq;
using NUnit.Framework;
using WebAPI.Auth;
using WebAPI.Model;
using WebAPI.Tests.Fakes;

namespace WebAPI.Tests.Auth
{
    [TestFixture]
    public class PasswordHashMigrationTests
    {
        private readonly BCryptPasswordHasher hasher = new BCryptPasswordHasher(4);

        [Test]
        public void PlainTextPasswords_AreHashed_AndStillWork()
        {
            var repository = new FakeCustomerRepository();
            repository.Users.Add(new User { Id = 1, Email = "one@example.com", Password = "password-one" });

            var hashed = PasswordHashMigration.Run(repository, hasher);

            Assert.That(hashed, Is.EqualTo(1));
            var stored = repository.Users.Single().Password;
            Assert.That(hasher.IsHashed(stored), Is.True);
            Assert.That(hasher.Verify("password-one", stored), Is.True);
        }

        [Test]
        public void HashedAndEmptyPasswords_AreLeftAlone()
        {
            var existingHash = hasher.Hash("password-one");
            var repository = new FakeCustomerRepository();
            repository.Users.Add(new User { Id = 1, Email = "one@example.com", Password = existingHash });
            repository.Users.Add(new User { Id = 2, Email = "two@example.com", Password = null });

            var hashed = PasswordHashMigration.Run(repository, hasher);

            Assert.That(hashed, Is.EqualTo(0));
            Assert.That(repository.Users[0].Password, Is.EqualTo(existingHash));
            Assert.That(repository.Users[1].Password, Is.Null);
        }

        [Test]
        public void RunningTwice_HashesOnlyOnce()
        {
            var repository = new FakeCustomerRepository();
            repository.Users.Add(new User { Id = 1, Email = "one@example.com", Password = "password-one" });

            PasswordHashMigration.Run(repository, hasher);
            var afterFirstRun = repository.Users.Single().Password;

            Assert.That(PasswordHashMigration.Run(repository, hasher), Is.EqualTo(0));
            Assert.That(repository.Users.Single().Password, Is.EqualTo(afterFirstRun));
        }
    }
}
