using System.IO;
using Dapper;

namespace WebAPI.Data
{
    /// <summary>
    /// Creates the SQLite database on first start, now that it is no longer committed to the repository.
    /// </summary>
    public static class SqLiteSchema
    {
        /// <summary>
        /// Creates the database file (at the DbConnection setting) and its tables if they don't exist.
        /// Safe to run on every start: existing tables and data are left untouched.
        /// </summary>
        public static void EnsureCreated()
        {
            var directory = Path.GetDirectoryName(SqLiteBaseRepository.DbFile);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var cnn = SqLiteBaseRepository.SimpleDbConnection())
            {
                cnn.Open();
                cnn.Execute(
                    @"CREATE TABLE IF NOT EXISTS User (Id INTEGER PRIMARY KEY AUTOINCREMENT, Email VARCHAR, Password VARCHAR, CreatedAt DATETIME, ModifiedAt DATETIME);
                    CREATE TABLE IF NOT EXISTS FuelDetail (Id INTEGER PRIMARY KEY AUTOINCREMENT, UserId INTEGER, MeterReading INTEGER, TotalPrice DOUBLE, AddedFuel DOUBLE, CreatedAt DATETIME, ModifiedAt DATETIME, Note VARCHAR);");
            }
        }
    }
}
