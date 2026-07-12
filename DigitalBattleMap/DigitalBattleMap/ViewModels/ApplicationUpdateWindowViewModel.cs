
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace DigitalBattleMap.ViewModels;

public class ApplicationUpdateWindowViewModel : ViewModelBase
{
    private static readonly string CompressedUpdateFilePath = Path.Combine(Constants.UpdateDirectoryPath, "DigitalBattleMap.7z");
    private static readonly string ExtractedUpdatePath = Path.Combine(Constants.UpdateDirectoryPath, "DigitalBattleMap");

    public ApplicationUpdateWindowViewModel()
    {
        Initialize();
    }

    private void Initialize()
    {
        UpdateLog = "";
    }

    protected override void InitializeCommands()
    {
    }

    public string UpdateLog { get => Get<string>(); set => Set(value); }
    public int MaximumProgress { get => Get<int>(); set => Set(value); }
    public int CurrentProgress { get => Get<int>(); set => Set(value); }

    public void UpdateApplication(GithubReleaseInfo releaseInfo)
    {
        try
        {
            var updateSteps = new List<UpdateStep> { 
                new UpdateStep(CreateDirectory, 1),
                new UpdateStep(() => DownloadNewVersion(releaseInfo), 2),
                new UpdateStep(ExtractFiles, 2),
                new UpdateStep(StartUpdateScript, 1),
            };

            // Make sure that the progress bar is started before executing anything
            // This Looks better for the user, otherwise the first step is executed with 0 progress
            MaximumProgress = updateSteps.Sum(s => s.ProgressWeight) + 1;
            CurrentProgress += 1;

            foreach (var step in updateSteps)
            {
                step.Execute();
                CurrentProgress += step.ProgressWeight;
            }
        }
        catch (Exception e)
        {
            LogOutputMessage("Update failed!");
            LogOutputMessage($"Exception: {e.Message}");
            if (IO.Directory.Exists(Constants.UpdateDirectoryPath))
            {
                IO.Directory.Delete(Constants.UpdateDirectoryPath, true);
            }
            return;
        }
    }

    private void CreateDirectory()
    {
        LogOutputMessage("Creating update directory");
        if(IO.Directory.Exists(Constants.UpdateDirectoryPath))
        {
            IO.Directory.Delete(Constants.UpdateDirectoryPath, true);
        }

        IO.Directory.CreateDirectory(Constants.UpdateDirectoryPath);
    }

    private void DownloadNewVersion(GithubReleaseInfo releaseInfo)
    {
        LogOutputMessage($"Downloading release {releaseInfo.tag_name}");
        var asset = releaseInfo.assets.SingleOrDefault(a => a.name.Contains("DigitalBattleMap_"));
        if (asset != null)
        {
            asset.Download(CompressedUpdateFilePath);
        }
    }

    private void ExtractFiles()
    {
        LogOutputMessage("Extracting files");
        IO.ZipFile.ExtractToDirectory(CompressedUpdateFilePath, ExtractedUpdatePath);
        IO.File.Delete(CompressedUpdateFilePath);
    }

    private void StartUpdateScript()
    {
        // Script:
        // Close application
        // Copy files over old files
        // Start application with --update_successfull flag

        LogOutputMessage("Start update script");
        var scriptPath = Path.Combine(Constants.UpdateDirectoryPath, "Update.bat");
        var executablePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string batchScript = $@"@echo off
echo Closing DigitalBattleMap
taskkill /f /im DigitalBattleMap.exe

echo waiting for application to close
timeout /t 1 /nobreak

echo Copying files
robocopy ""{ExtractedUpdatePath}"" ""{executablePath}"" /E /IS

echo Starting DigitalBattleMap
start /d ""{executablePath}"" DigitalBattleMap.exe --update";

        IO.File.WriteAllText(scriptPath, batchScript);
        Process.Start(scriptPath);
    }

    private void LogOutputMessage(string message)
    {
        if(UpdateLog != "")
        {
            UpdateLog += "\n";
        }

        UpdateLog += DateTime.Now.ToString("yyyy-dd-MM HH:mm:ss: ") + message;
    }

    private class UpdateStep
    {
        private Action _action;

        public UpdateStep(Action action, int progressWeight)
        {
            _action = action;
            ProgressWeight = progressWeight;
        }

        public int ProgressWeight { get; }

        public void Execute()
        {
            _action!();
        }
    }
}
