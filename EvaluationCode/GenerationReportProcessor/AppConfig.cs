using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace GenerationReportProcessor
{
    public class AppConfig
    {
        // Getting configuration values from appsettings
        public static string GetAppSettings(string key)
        {
            // Check if the specified key exists in the AppSettings section of the configuration file.
            if (ConfigurationManager.AppSettings[key] != null)
            {
                return (ConfigurationManager.AppSettings[key]).ToString();
            }
            else
            {
                throw new Exception($"Missing configuration for key: {key}");
            }

        }
    }
}
