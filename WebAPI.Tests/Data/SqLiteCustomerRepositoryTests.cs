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
    /// created with the same schema as App_Data/SimpleDb.sqlite.
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

            Execute(
                "CREATE TABLE User (Id INTEGER PRIMARY KEY AUTOINCREMENT, Email VARCHAR, Password VARCHAR, CreatedAt DATETIME, ModifiedAt DATETIME)",
                "CREATE TABLE FuelDetail (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, MeterReading INTEGER, TotalPrice DOUBLE, AddedFuel DOUBLE, CreatedAt DATETIME, ModifiedAt DATETIME, Note VARCHAR)");
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
