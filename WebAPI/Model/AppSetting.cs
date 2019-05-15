using System;
using System.Configuration;
using System.Globalization;

namespace WebAPI.Model
{
    public static class AppSettings
    {
        public static string ToEmail { get { return Setting<string>("ToEmail"); } }
        public static string DbConnection { get { return Setting<string>("DbConnection"); } }
        

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