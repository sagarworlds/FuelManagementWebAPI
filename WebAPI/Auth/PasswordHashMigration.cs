using System;
using System.Diagnostics;
using WebAPI.Data;

namespace WebAPI.Auth
{
    /// <summary>
    /// Replaces passwords stored as plain text by earlier versions with hashes.
    /// </summary>
    public static class PasswordHashMigration
    {
        /// <summary>
        /// Hashes every stored password that isn't hashed yet. Safe to run on every start: hashed
        /// passwords are left alone, and empty ones (which can't be used to log in) are skipped.
        /// </summary>
        /// <param name="repository">The user store.</param>
        /// <param name="hasher">Hashes the passwords.</param>
        /// <returns>How many passwords were hashed.</returns>
        /// <exception cref="ArgumentNullException">When either argument is null.</exception>
        public static int Run(ICustomerRepository repository, IPasswordHasher hasher)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }
            if (hasher == null)
            {
                throw new ArgumentNullException("hasher");
            }

            var hashed = 0;
            foreach (var user in repository.GetUser())
            {
                if (string.IsNullOrEmpty(user.Password) || hasher.IsHashed(user.Password))
                {
                    continue;
                }
                repository.UpdatePassword(user.Id, hasher.Hash(user.Password), DateTime.UtcNow);
                hashed++;
            }

            if (hashed > 0)
            {
                Trace.TraceInformation("Hashed {0} password(s) that were stored as plain text.", hashed);
            }
            return hashed;
        }
    }
}
