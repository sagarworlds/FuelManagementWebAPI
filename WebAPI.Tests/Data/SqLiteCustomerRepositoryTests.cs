using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using NUnit.Framework;
using WebAPI.Data;
using WebAPI.Model;

namespace WebAPI.Tests.Data
{
    /// <summary>
    /// Runs the real repository against a fresh SQLite database (the DbConnection setting in App.config),
    /// created by <see cref="SqLiteSchema.EnsureCreated"/> as on the API's first start.
    /// </summary>
    [TestFixture]
    public class SqLiteCustomerRepositoryTests
    {
        private SqLiteCustomerRepository repository;

        [SetUp]
        public void CreateEmptyDatabase()
        {
            var file = SqLiteBaseRepository.DbFile;
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            SqLiteSchema.EnsureCreated();
            repository = new SqLiteCustomerRepository();
        }

        [Test]
        public void SavedDates_AreReadBackAsTheSameUtcInstant()
        {
            var createdAt = new DateTime(2026, 1, 14, 18, 30, 0, DateTimeKind.Utc);
            var modifiedAt = new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc);

            var saved = repository.Save(new FuelDetail
            {
                UserId = 1, MeterReading = 1000, TotalPrice = 500, AddedFuel = 5, CreatedAt = createdAt, ModifiedAt = modifiedAt
            });

            Assert.That(saved.CreatedAt, Is.EqualTo(createdAt));
            Assert.That(saved.CreatedAt.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(saved.ModifiedAt, Is.EqualTo(modifiedAt));
        }

        [Test]
        public void DatesStoredByEarlierVersions_AreReadAsUtc()
        {
            // Both formats exist in the committed database: ISO text with "Z" and plain text without an offset.
            Execute(
                "INSERT INTO FuelDetail (UserId, CreatedAt) VALUES (1, '2019-07-16T17:27:51.687Z')",
                "INSERT INTO FuelDetail (UserId, CreatedAt) VALUES (1, '2019-10-04 00:00:00')");

            var dates = repository.GetListFuelDetailByUserId(new FuelDetail { UserId = 1 })
                .OrderBy(f => f.Id).Select(f => f.CreatedAt).ToArray();

            Assert.That(dates[0], Is.EqualTo(new DateTime(2019, 7, 16, 17, 27, 51, 687, DateTimeKind.Utc)));
            Assert.That(dates[1], Is.EqualTo(new DateTime(2019, 10, 4, 0, 0, 0, DateTimeKind.Utc)));
            Assert.That(dates.Select(d => d.Kind), Is.All.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void Entries_AreListedPerUser()
        {
            repository.Save(new FuelDetail { UserId = 1, MeterReading = 1000, CreatedAt = DateTime.UtcNow });
            repository.Save(new FuelDetail { UserId = 2, MeterReading = 2000, CreatedAt = DateTime.UtcNow });

            var entries = repository.GetListFuelDetailByUserId(new FuelDetail { UserId = 2 });

            Assert.That(entries.Select(e => e.MeterReading), Is.EqualTo(new[] { 2000 }));
        }

        [Test]
        public void EnsureCreated_KeepsExistingData()
        {
            repository.Save(new FuelDetail { UserId = 1, MeterReading = 1000, CreatedAt = DateTime.UtcNow });

            SqLiteSchema.EnsureCreated();

            Assert.That(repository.GetListFuelDetailByUserId(new FuelDetail { UserId = 1 }), Has.Length.EqualTo(1));
        }

        [Test]
        public void UsersAreFoundByEmailIgnoringCase_AndById()
        {
            var saved = repository.Save(new User { Email = "one@example.com", Password = "hash", CreatedAt = DateTime.UtcNow });

            Assert.That(repository.GetUserByEmail("ONE@example.com").Id, Is.EqualTo(saved.Id));
            Assert.That(repository.GetUserByEmail("two@example.com"), Is.Null);
            Assert.That(repository.GetUserById(saved.Id).Email, Is.EqualTo("one@example.com"));
            Assert.That(repository.GetUserById(saved.Id + 1), Is.Null);
        }

        [Test]
        public void UpdatePassword_ReplacesOnlyThatUsersPassword()
        {
            var one = repository.Save(new User { Email = "one@example.com", Password = "old-one", CreatedAt = DateTime.UtcNow });
            var two = repository.Save(new User { Email = "two@example.com", Password = "old-two", CreatedAt = DateTime.UtcNow });
            var modifiedAt = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

            repository.UpdatePassword(one.Id, "new-one", modifiedAt);

            Assert.That(repository.GetUserById(one.Id).Password, Is.EqualTo("new-one"));
            Assert.That(repository.GetUserById(one.Id).ModifiedAt, Is.EqualTo(modifiedAt));
            Assert.That(repository.GetUserById(two.Id).Password, Is.EqualTo("old-two"));
        }

        [Test]
        public void EmailExists_IgnoresLetterCase()
        {
            repository.Save(new User { Email = "one@example.com", Password = "password-one", CreatedAt = DateTime.UtcNow });

            Assert.That(repository.EmailExists("ONE@Example.com"), Is.True);
            Assert.That(repository.EmailExists("two@example.com"), Is.False);
        }

        private static void Execute(params string[] statements)
        {
            using (var connection = SqLiteBaseRepository.SimpleDbConnection())
            {
                connection.Open();
                foreach (var statement in statements)
                {
                    using (var command = new SQLiteCommand(statement, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
