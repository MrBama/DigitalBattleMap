using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;

namespace DigitalBattleMap;

public static class Startup
{
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    public static void PerformStartupChecks()
    {
        var settings = Settings.Load();
        // Check if this is the initial startup
        if (!settings.IsSoftwareInstalled)
        {
            // Create directories
            IO.Directory.CreateDirectory(Constants.WebExtensionsPath);
            IO.Directory.CreateDirectory(Constants.MonsterTokensPath);
            IO.Directory.CreateDirectory(Constants.CustomTokensPath);
            IO.Directory.CreateDirectory(Constants.AutoSavesPath);

            // Assisiate the .dbm file extension with the application.
            AssosiateFileExtension();

            // Create default settings
            settings.IsSoftwareInstalled = true;
            settings.Version = SettingsUpdater.SettingsVersion;
            settings.Save();
        }
        // Check if the settings need to be updated
        else if (SettingsUpdater.IsUpdateRequired(settings.Version))
        {
            SettingsUpdater.Update(settings.Version);
            settings = Settings.Load();
            settings.Version = SettingsUpdater.SettingsVersion;
            settings.Save();
        }
    }

    private static void AssosiateFileExtension()
    {
        bool hasAdminPermissions = IsElevated();
        bool wantsFileAssociation = true;
        if (!hasAdminPermissions)
        {
            wantsFileAssociation = MessageBox.Show(
                "Do you want to associate the .dbm file extension with the application?",
                "Digital Battle Map",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        if (wantsFileAssociation)
        {
            var dataDirectoryPath = Path.Combine(Constants.SettingsPath, "Data");
            var saveFileIconFileName = "SaveFileIcon.ico";
            var saveFileIconFilePath = Path.Combine(dataDirectoryPath, saveFileIconFileName);

            // Extract SaveFileIcon.ico to disk
            IO.Directory.CreateDirectory(dataDirectoryPath);
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream($"DigitalBattleMap.Resources.{saveFileIconFileName}"))
            {
                using var file = new FileStream(Path.Combine(saveFileIconFilePath), FileMode.Create);
                resource?.CopyTo(file);
            }

            // Associate extension with icon
            if (hasAdminPermissions)
            {
                var extensionKey = Registry.ClassesRoot.CreateSubKey(".dbm", true);
                var defaultIconKey = extensionKey.CreateSubKey("DefaultIcon", true);
                defaultIconKey.SetValue("", saveFileIconFilePath);
            }
            else
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "reg.exe",
                    Arguments = $"add \"HKCR\\.dbm\\DefaultIcon\" /ve /t REG_SZ /d \"{saveFileIconFilePath}\" /f",
                    UseShellExecute = false,
                    Verb = "runas",
                };
                Process.Start(psi)?.WaitForExit();
            }

            // Refresh icon cache
            // HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST
            SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
        }
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);

        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
}
