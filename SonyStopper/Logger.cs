using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SonyStopper {
    class Logger {

        private static string logFileName = null;
        private static Settings settings = null;
        //------------------------------------------------------------------------------
        public static void init(Settings stngs) {

            if (logFileName == null) {
                settings = stngs;
                logFileName = "log_" + DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss", CultureInfo.InvariantCulture);
            }       
        }
        //------------------------------------------------------------------------------
        public static void logLine(string txt, Settings.LogLevel level= Settings.LogLevel.NORMAL) {

            txt = DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss", CultureInfo.InvariantCulture) + txt + '\n';

            if (logFileName == null) {
                return;

            }

            if (settings.logLevel >= level) {
                Console.Write(txt);
                File.AppendAllText (logFileName, txt);
            }

        }
        //------------------------------------------------------------------------------

    }
}
