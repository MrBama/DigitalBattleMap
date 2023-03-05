using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public static class Startup
    {
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        public static void CheckForInitialStartup()
        {
            if (!Directory.Exists(Constants.SettingsPath))
            {
                var dataDirectoryPath = Path.Combine(Constants.SettingsPath, "Data");
                var saveFileIconFileName = "SaveFileIcon.ico";
                var saveFileIconFilePath = Path.Combine(dataDirectoryPath, saveFileIconFileName);

                // Create default settings
                var settings = new Settings();
                settings.Save();

                // Create token directories
                Directory.CreateDirectory(Constants.MonsterTokensPath);
                Directory.CreateDirectory(Constants.CustomTokensPath);

                // Extract SaveFileIcon.ico to disk
                Directory.CreateDirectory(dataDirectoryPath);
                using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.{saveFileIconFileName}"))
                {
                    using (var file = new FileStream(Path.Combine(saveFileIconFilePath), FileMode.Create))
                    {
                        resource?.CopyTo(file);
                    }
                }

                // Associate extension with icon
                var extensionKey = Registry.ClassesRoot.CreateSubKey(".dbm", true);
                var defaultIconKey = extensionKey.CreateSubKey("DefaultIcon", true);
                defaultIconKey.SetValue("", saveFileIconFilePath);
                
                // Refresh icon cache
                // HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
        }
    }
}
