using DigitalBattleMap.Interfaces;
using DigitalBattleMap.ViewModels;
using DigitalBattleMap.Views;
using Microsoft.VisualBasic.ApplicationServices;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalBattleMap.Utilities;

public class ApplicationUpdater
{
    public static readonly string ApplicationVersion = "1.0.0";

    private Thread _thread;
    private IWindowService _windowService;

    public ApplicationUpdater(IWindowService windowService)
    {
        _windowService = windowService;
    }

    public void CheckForUpdates()
    {
        //TODO: check for auto update setting
        //TODO: add current version to settings window
        //TODO: manual update button in settings?
        //TODO: progress bar?
        //TODO: cancel button?
        _thread = new Thread(Update);
        _thread.Start();
    }

    private void Update()
    {
        try
        {
            string user = "gorhill";
            string repository = "uBlock";
            //var releaseInfo = GitHub.GetLatestReleaseInfo(user, repository);
            //if(IsUpdateAvailable(releaseInfo) && ConfirmUpdate(releaseInfo))
            //{
            //    DownloadAndInstallUpdate();
            //}
            DownloadAndInstallUpdate(null);
        }
        catch (Exception)
        {
            return;
        }
    }

    private bool IsUpdateAvailable(GithubReleaseInfo releaseInfo)
    {
        return releaseInfo.tag_name != ApplicationVersion;
    }

    private bool ConfirmUpdate(GithubReleaseInfo releaseInfo)
    {
        var confirmed = false;
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = $"There is a new version available, do you want to update?\n\n{ApplicationVersion} -> {releaseInfo.tag_name}",
            LeftButtonAction = () => { confirmed = true; }
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);
        });

        return confirmed;
    }

    private void DownloadAndInstallUpdate(GithubReleaseInfo releaseInfo)
    {
        var applicationUpdateWindowViewModel = new ApplicationUpdateWindowViewModel();
        // Start the update in a task because otherwise the UI won't be visible.
        // Since ShowWindowDialog is blocking, first showing the UI and
        // then triggering the update doesn't work either.
        Task.Run(() =>
        {
            applicationUpdateWindowViewModel.UpdateApplication(releaseInfo);
        });
        Application.Current.Dispatcher.Invoke(() =>
        {
            _windowService.ShowWindowDialog<ApplicationUpdateWindow>(applicationUpdateWindowViewModel);
        });
    }
}
