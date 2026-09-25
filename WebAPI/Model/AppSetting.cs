using System;
using System.Configuration;
using System.Globalization;

namespace WebAPI.Model
{
    public static class AppSettings
    {
        public static string DbConnection { get { return Setting<string>("DbConnection"); } }
        /// <summary>Secret that signs login tokens; kept in the git-ignored Secrets.config.</summary>
        public static string JwtSecret { get { return Setting<string>("JwtSecret"); } }
        /// <summary>Comma-separated browser origins allowed to call the API across origins (CORS).</summary>
        public static string AllowedOrigins { get { return Setting<string>("AllowedOrigins"); } }
        /// <summary>How long a login token stays valid, in minutes.</summary>
        public static int JwtLifetimeMinutes { get { return Setting<int>("JwtLifetimeMinutes"); } }
        

        private static T Setting<T>(string name)
        {
            string value = ConfigurationManager.AppSettings[name];

            if (value == null)
            {
                throw new Exception(String.Format("Could not find setting '{0}' in the appSettings of Web.config or Secrets.config.", name));
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}