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
        /// <summary>
        /// Retrieves the value of a specified key from the AppSettings section of the configuration file.
        /// </summary>
        /// <param name="key">The key of the setting to retrieve from the AppSettings section.</param>
        /// <returns>
        /// The value associated with the specified key if it exists in the AppSettings section.
        /// </returns>
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
