using System;
using System.Configuration;
using System.Globalization;

namespace WebAPI.Controllers
{
    public static class AppSettings
    {
        public static string SMSIsTest { get { return Setting<string>("SMSIsTest"); } }
        public static string ToEmail { get { return Setting<string>("ToEmail"); } }
        public static string SMSAPIKey { get { return Setting<string>("SMSAPIKey"); } }
        public static string FromEmail { get { return Setting<string>("FromEmail"); } }
        public static string FromEmailPassword { get { return Setting<string>("FromEmailPassword"); } }
        public static string JWTSecretKey { get { return Setting<string>("JWTSecretKey"); } }


        

        private static T Setting<T>(string name)
        {
            string value = ConfigurationManager.AppSettings[name];

            if (value == null)
            {
                throw new Exception(String.Format("Could not find setting '{0}',", name));
            }

            return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
        }
    }
}