using WebAPI.Auth;
using WebAPI.Data;

namespace WebAPI
{
    public static class DatabaseConfig
    {
        /// <summary>
        /// Prepares the database before the first request: creates it if it's missing, and hashes any
        /// passwords still stored as plain text. Failures stop the app from starting rather than
        /// leaving it serving requests against a missing or unmigrated database.
        /// </summary>
        public static void Initialize()
        {
            SqLiteSchema.EnsureCreated();
            PasswordHashMigration.Run(new SqLiteCustomerRepository(), new BCryptPasswordHasher());
        }
    }
}
