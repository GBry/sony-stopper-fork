using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonyStopper {
    class Settings {

        public enum LogLevel {
            NONE = 0,
            NORMAL,
            VERBOSE
        }

        public LogLevel logLevel { get; private set; }
        public string ipAddr { get; private set; }
        public string authPSK { get; private set; }

        public bool init() {
            // when auto started the actual path seems to be the c:/windows/system32
            // so read from the directory of the exe
            string settingsTxt = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "settings.json");

            if (String.IsNullOrEmpty(settingsTxt) == false) {
                try {
                    dynamic json = JObject.Parse(settingsTxt);

                    if (json.ipAddr != null) {
                        this.ipAddr = json.ipAddr;
                    } else {
                        return false;
                    }

                    if (json.authPSK != null) {
                        this.authPSK = json.authPSK;
                    } else {
                        return false;
                    }

                    if (json.logLevel != null) {
                        this.logLevel = (LogLevel)Enum.Parse(typeof(LogLevel), (string)(json.logLevel));
                    } else {
                        return false;
                    }

                } catch (Exception ex) {
#if DEBUG
                    Console.WriteLine("Settings() -> " + ex.Message);
#endif        
                    return false;

                }
                return true;
            }
            return false;
        }



    }
}
