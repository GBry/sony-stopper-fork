using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;

namespace SonyStopper {
    /// <summary>
    /// 
    /// </summary>
    class ContextMenus {

        readonly string AppName = "SonyStopper";

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns>ContextMenuStrip</returns>
        public ContextMenuStrip Create() {
            // Add the default menu options.
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem item;

            // Startup
            item = new ToolStripMenuItem();
            item.Text = "Auto start with Windows";
            item.Click += new System.EventHandler(Autorun_Click);
            menu.Items.Add(item);


            // load startup
            try {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                        ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);

                object regVal = rk.GetValue(AppName);

                if ((regVal != null) && (regVal.ToString() == Application.ExecutablePath))
                    (menu.Items[0] as ToolStripMenuItem).Checked = true;
                else
                    (menu.Items[0] as ToolStripMenuItem).Checked = false;

            } catch {
                // do nothing, default walues will be used
            }


            // Exit.
            item = new ToolStripMenuItem();
            item.Text = "Exit";
            item.Click += new System.EventHandler(Exit_Click);
            menu.Items.Add(item);

            return menu;
        }

        void Autorun_Click(object sender, EventArgs e) {
            var item = sender as ToolStripMenuItem;
            item.Checked = !item.Checked;

            try {
                // register if we want to
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (item.Checked)
                    rk.SetValue(AppName, Application.ExecutablePath);
                else
                    rk.DeleteValue(AppName, false);
            } catch {
                MessageBox.Show("Cannot set the auto-start option\r\n\r\nDo you have administrator privileges?");

                item.Checked = !item.Checked;
            }

        }

        void Exit_Click(object sender, EventArgs e) {
            // Quit without further ado.
            Application.Exit();
        }


    }
}