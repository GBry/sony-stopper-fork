using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace SonyStopper {
    class IPAccess {

        Settings settings;

        public IPAccess(Settings settings) {
            this.settings = settings;
        }  

        enum IP_CMD_TYPE {
            GETPLAYINGCONTENTINFO,
            GETCURRENTEXTERNALINPUTSSTATUS,
            GETPOWERSTATUS,
            SETPOWERSTATUS_OFF
        }

        struct IPCmd {
            public IPCmd(string request, string path, bool isAuth) {
                this.request = request;
                this.path = path;
                this.isAuth = isAuth;
            }

            public readonly string request;
            public readonly string path;
            public readonly bool isAuth;
        }

        // https://pro-bravia.sony.net/develop/integrate/rest-api/spec/index.html

        Dictionary<IP_CMD_TYPE, IPCmd> ipCommands = new Dictionary<IP_CMD_TYPE, IPCmd>() {
            { IP_CMD_TYPE.GETPLAYINGCONTENTINFO,
                new IPCmd("{ \"method\": \"getPlayingContentInfo\", \"id\": 103, \"params\": [], \"version\": \"1.0\"}",
                "/avContent",
                true) },

             { IP_CMD_TYPE.GETCURRENTEXTERNALINPUTSSTATUS,
                new IPCmd("{ \"method\": \"getCurrentExternalInputsStatus\", \"id\": 105, \"params\": [], \"version\": \"1.1\"}",
                "/avContent",
                false) },

            { IP_CMD_TYPE.GETPOWERSTATUS,
                new IPCmd("{ \"method\": \"getPowerStatus\", \"id\": 50, \"params\": [], \"version\": \"1.0\"}",
                "/system",
                false) },

             { IP_CMD_TYPE.SETPOWERSTATUS_OFF,
                new IPCmd("{ \"method\": \"setPowerStatus\", \"id\": 55, \"params\": [{\"status\": false}], \"version\": \"1.0\"}",
                "/system",
                true) },
        };
        //----------------------------------------------------------------------------------------------------------------
        public bool isExternalHDMIInputSelected(out string hdmiInUse) {
            IPCmd cmd = ipCommands[IP_CMD_TYPE.GETPLAYINGCONTENTINFO];
            string response = webClPostSafe(getApiBase() + cmd.path, cmd.request, cmd.isAuth);

           Logger.logLine(response, Settings.LogLevel.VERBOSE);

            if (String.IsNullOrEmpty(response) == false) {
                try {
                    dynamic json = JObject.Parse(response);
                    if (json.result != null // in menu we get an error as response as technically there is no content
                        && json.result[0].source == "extInput:hdmi") {
                        hdmiInUse = json.result[0].uri;
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.logLine("isExternalHDMIInputSelected() -> " + ex.Message);
                }
            }
            hdmiInUse = null;
            return false;
        }
        //----------------------------------------------------------------------------------------------------------------
        public bool isSignalPresent(string inputUri) {
            IPCmd cmd = ipCommands[IP_CMD_TYPE.GETCURRENTEXTERNALINPUTSSTATUS];
            string response = webClPostSafe(getApiBase() + cmd.path, cmd.request, cmd.isAuth);
            bool wasInputFound = false;

            Logger.logLine(response, Settings.LogLevel.VERBOSE);

            if (String.IsNullOrEmpty(response) == false) {
                try {
                    dynamic json = JObject.Parse(response);

                    for (int i = 0; i < json.result[0].Count; ++i) {

                        if (json.result[0][i].uri == inputUri) {
                            wasInputFound = true;
                            return Convert.ToBoolean(json.result[0][i].status);
                        }

                    }
                } catch (Exception ex) {
                   Logger.logLine("isSignalPresent() -> " + ex.Message);
                }
            }

            if (wasInputFound == false) {
                Logger.logLine("Input " + inputUri + " was not found!", Settings.LogLevel.VERBOSE);
            }
            return false;
        }
        //----------------------------------------------------------------------------------------------------------------
        public bool isTvOn() {
            IPCmd cmd = ipCommands[IP_CMD_TYPE.GETPOWERSTATUS];
            string response = webClPostSafe(getApiBase() + cmd.path, cmd.request, cmd.isAuth);

            // the response after freshly turned off is 
            // { "result":[{ "status":"standby"}],"id":50}
            // so we might be able to turn it on before it goes to deep sleep

           Logger.logLine(response, Settings.LogLevel.VERBOSE);

            if (String.IsNullOrEmpty(response) == false) {
                try {
                    dynamic json = JObject.Parse(response);
                    if (json.result[0].status == "active") {
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.logLine("isTvOn() -> " + ex.Message);
                }
            }
          
            return false;
        }
        //----------------------------------------------------------------------------------------------------------------
        public bool turnTVOff() {
            IPCmd cmd = ipCommands[IP_CMD_TYPE.SETPOWERSTATUS_OFF];
            string response = webClPostSafe(getApiBase() + cmd.path, cmd.request, cmd.isAuth);

            Logger.logLine(response, Settings.LogLevel.VERBOSE);

            if (String.IsNullOrEmpty(response) == false) {
                try {
                    dynamic json = JObject.Parse(response);
                    if (json.result.Count == 0 && json.id == 55) {
                        return true;
                    }
                } catch (Exception ex) {
                    Logger.logLine("turnTVOff() -> " + ex.Message);
                }
            }

            return false;
        }
        //----------------------------------------------------------------------------------------------------------------
        private string getApiBase() {
            return "http://" + settings.ipAddr + "/sony";
        }
        //----------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------
        // this is spacial for the TV only 
        [DesignerCategory("")]
        private class CustWebClient : WebClient {
            public int Timeout_ms = 5 * 1000; // default 25sec

            protected override WebRequest GetWebRequest(Uri uri) {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = Timeout_ms;
                ((HttpWebRequest)w).KeepAlive = false;
                return w;
            }
        }
        //----------------------------------------------------------------------------------------------------------------
        CustWebClient webCl = new CustWebClient();
        //----------------------------------------------------------------------------------------------------------------
        private string webClPostSafe(string link, string data, bool isAuth) {
            string retVal = null;
            try {
                webCl.Headers[HttpRequestHeader.ContentType] = "application/json";
                if (isAuth) {
                    webCl.Headers["X-Auth-PSK"] = settings.authPSK;
                }
                retVal = webCl.UploadString(link, data);
#if DEBUG
                // Console.WriteLine("--------------" + link + "------------------------");
                // Console.WriteLine(webCl.ResponseHeaders);
#endif
            } catch (System.Net.WebException ex) {
                if (ex.Response != null) {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new System.IO.StreamReader(stream)) {
                        Logger.logLine("webCl: " + reader.ReadToEnd(), Settings.LogLevel.VERBOSE);
                    }
                } else if (ex.InnerException != null) {
                    Logger.logLine("webCl: " + ex.InnerException.Message, Settings.LogLevel.VERBOSE);

                    if (ex.InnerException.InnerException != null) {
                        Logger.logLine("webCl: " + ex.InnerException.InnerException.Message, Settings.LogLevel.VERBOSE);
                    }
                } else {
                    Logger.logLine("webCl: " + ex.Message, Settings.LogLevel.VERBOSE);
                }
            } catch (Exception ex) {
                Logger.logLine("webCl: " + ex.Message, Settings.LogLevel.VERBOSE);
            }

            return retVal;
        }
        //----------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------------------
    }
}
