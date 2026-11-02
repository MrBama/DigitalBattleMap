using DigitalBattleMap.DataClasses;
using DigitalBattleMap.ViewModels;
using DigitalBattleMap.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DigitalBattleMap.Utilities;

public static class CrashDumpCollector
{
    [Flags]
    private enum DumpType : uint
    {
        Normal = 0x00000000,
        WithDataSegs = 0x00000001,
        WithFullMemory = 0x00000002,
        WithHandleData = 0x00000004,
        WithUnloadedModules = 0x00000020,
        WithFullMemoryInfo = 0x00000800,
        WithThreadInfo = 0x00001000,
        WithTokenInformation = 0x00040000,
    }

    [DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(
        IntPtr hProcess,
        uint processId,
        SafeHandle hFile,
        DumpType dumpType,
        IntPtr exceptionParam,
        IntPtr userStreamParam,
        IntPtr callbackParam);

    public static void CreateDump(Exception exception)
    {               
        // Show exception pop-up and ask for confirmation
        var createCrashDump = false;
        var viewModel = new ConfirmationWindowViewModel()
        {
            Content = "An unexpected exception has occured!\n\nCreating crash dump...\nThe application might freeze for a bit, wait for the completion pop-up.",
            IsLeftButtonVisible = false,
            IsRightButtonVisible = false,
            IsMiddleButtonVisible = true,
            MiddleButtonContent = "Continue",
            MiddleButtonAction = () => createCrashDump = true
        };
        var windowBefore = new ConfirmationWindow
        {
            DataContext = viewModel
        };
        windowBefore.ShowDialog();

        // Create crash dump
        if(createCrashDump)
        {
            using var tempDirectory = new TempDirectory();

            // Gather exception log and last auto save
            CreateExceptionLog(exception, tempDirectory.Path);
            CopyLastAutoSave(tempDirectory.Path);

            // Create mini dump
            CreateMiniDump(tempDirectory.Path, viewModel);

            // Zip crash dump
            CreateCrashDumpZip(tempDirectory.Path);

            // Show completion pop-up
            viewModel.Content = "Crash dump has been created!";
            viewModel.MiddleButtonContent = "Ok";
            var windowAfter = new ConfirmationWindow
            {
                DataContext = viewModel
            };
            windowAfter.ShowDialog();
        }
        else
        {
            // Only log the exception when the crash dump is cancelled (by closing the window)
            exception.Log();
        }
    }

    private static void CreateExceptionLog(Exception exception, string path)
    {
        exception.Log(path);
    }

    private static void CopyLastAutoSave(string path)
    {
        var files = IO.Directory.GetFiles(Constants.AutoSavesPath);
        if (files.Length > 1)
        {
            var filesOrderedByDate = files.OrderBy(IO.File.GetCreationTime);
            var lastSaveFilePath = filesOrderedByDate.Last();
            IO.File.Copy(filesOrderedByDate.Last(), Path.Combine(path, Path.GetFileName(lastSaveFilePath)));
        }
    }

    private static void CreateMiniDump(string path, ConfirmationWindowViewModel viewModel)
    {
        var process = Process.GetCurrentProcess();
        var miniDumpPath = Path.Combine(path, $"MiniDump_v{ApplicationUpdater.ApplicationVersion}.dmp");
        ArgumentNullException.ThrowIfNull(process);
        ArgumentException.ThrowIfNullOrEmpty(miniDumpPath);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(miniDumpPath));
        if (!string.IsNullOrEmpty(directory))
        {
            IO.Directory.CreateDirectory(directory);
        }

        using FileStream stream = new(miniDumpPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        // Full memory dump: entire address space (including the heap), handles, modules and thread state.
        bool success = MiniDumpWriteDump(
            process.Handle,
            (uint)process.Id,
            stream.SafeFileHandle,
            DumpType.WithFullMemory |
                DumpType.WithFullMemoryInfo |
                DumpType.WithDataSegs |
                DumpType.WithHandleData |
                DumpType.WithUnloadedModules |
                DumpType.WithThreadInfo |
                DumpType.WithTokenInformation,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero);

        if (!success)
        {
            viewModel.Content = "Failed to create mini dump.";
            viewModel.MiddleButtonContent = "Ok";
            var windowException = new ConfirmationWindow
            {
                DataContext = viewModel
            };
            windowException.ShowDialog();
        }
    }

    private static void CreateCrashDumpZip(string path)
    {
        var dateTime = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
        var crashDumpName = $"{dateTime} Crash dump.zip";
        IO.ZipFile.CreateFromDirectory(path, Path.Combine(Constants.SettingsPath, "Logs", crashDumpName));
    }
}
