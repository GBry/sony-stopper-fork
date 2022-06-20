using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;

namespace SonyStopper {
    /// <summary>
    /// 
    /// </summary>
    class ProcessIcon : IDisposable {
        /// <summary>
        /// The NotifyIcon object.
        /// </summary>
        NotifyIcon ni = null;
        Timer tmr = null;
        IPAccess IpAccess = null;
        Settings settings = null;
        const int NOTIFY_TIMEOUT_ms = 2000; // windows usually clmamps the timeout between 10-30s
        const string VERSION = "1.0.3";
     
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessIcon"/> class.
        /// </summary>
        public ProcessIcon() {
            // Instantiate the NotifyIcon object.
            ni = new NotifyIcon();
            tmr = new Timer();
            settings = new Settings();
            Logger.init(settings);
            IpAccess = new IPAccess(settings);
        }

        /// <summary>
        /// Displays the icon in the system tray.
        /// </summary>
        public void Display() {
            // Put the icon in the system tray and allow it react to mouse clicks.			
            ni.MouseClick += new MouseEventHandler(ni_MouseClick);
            ni.Icon = SonyStopper.Properties.Resources.sysIcon;
            ni.Text = "SONY Stopper";
            ni.Visible = true;

            // Attach a context menu.
            ni.ContextMenuStrip = new ContextMenus().Create();

            bool success = settings.init();
            if (success == false) {
                ni.ShowBalloonTip(NOTIFY_TIMEOUT_ms, "", "SONY Stopper failed to start", ToolTipIcon.Error);
                return;
            }

            // start timer
            tmr.Interval = 10 * 1000;
            tmr.Tick += new EventHandler(tmr_Tick);
            tmr.Enabled = true;

            ni.ShowBalloonTip(NOTIFY_TIMEOUT_ms, "", "SONY Stopper " + VERSION + " is running", ToolTipIcon.None);
        }

        public void Dispose() {
            // When the application closes, this will remove the icon from the system tray immediately.
            ni.Dispose();
        }

        // check comports every time and report differences 
        const uint DEBOUNCE_CNT_LIMIT = 3; // 3*10s 
        uint debounceVal = 0;
        void tmr_Tick(object sender, EventArgs e) {

            #region Main Logic

            bool isOn = IpAccess.isTvOn();
            bool isExternal = false;
            bool isActive = true;

            if (isOn) {
                string hdmiInUse;
                isExternal = IpAccess.isExternalHDMIInputSelected(out hdmiInUse);

                if (isExternal) {
                    isActive = IpAccess.isSignalPresent(hdmiInUse);
                }
            }

            if (isOn && isExternal && !isActive) {
                debounceVal++;
                Console.WriteLine("Increasing debounce: " + debounceVal);

                if (debounceVal >= DEBOUNCE_CNT_LIMIT) {
                    IpAccess.turnTVOff();
                }
            } else {
                Console.WriteLine("Clearing debounce -> " + "isOn: " + isOn + " | isExternal: " + isExternal + " | isActive: " + isActive);
                debounceVal = 0;
            }
            #endregion

        }

        // open up the context menu even on left click
        // for details read https://stackoverflow.com/questions/2208690/invoke-notifyicons-context-menu
        void ni_MouseClick(object sender, MouseEventArgs e) {
            // Handle mouse button clicks.
            if (e.Button == MouseButtons.Left) {
                MethodInfo mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                mi.Invoke(ni, null);
            }
        }
        //----------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------
        //----------------------------------------------------------------------------------------------------
    }
}