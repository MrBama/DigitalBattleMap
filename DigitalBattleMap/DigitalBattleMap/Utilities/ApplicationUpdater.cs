using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.ViewModels;
using DigitalBattleMap.Views;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace DigitalBattleMap.Utilities;

public class ApplicationUpdater
{
    public static readonly string ApplicationVersion = "26.6.5";

    private static readonly string _user = "MrBama";
    private static readonly string _repository = "DigitalBattleMap";

    private Thread _thread;
    private IWindowService _windowService;
    private Settings _settings;

    public ApplicationUpdater(IWindowService windowService, Settings settings)
    {
        _windowService = windowService;
        _settings = settings;
    }

    public void CheckForUpdates()
    {
        var releaseInfo = GitHub.GetLatestReleaseInfo(_user, _repository);
        if (IsUpdateAvailable(releaseInfo))
        {
            if (ConfirmUpdate(releaseInfo))
            {
                DownloadAndInstallUpdate(releaseInfo);
            }
        }
        else
        {
            var confirmationWindowViewModel = new ConfirmationWindowViewModel
            {
                Content = "The latest version is already installed!",
                IsLeftButtonVisible = false,
                IsRightButtonVisible = false,
                IsMiddleButtonVisible = true
            };
            _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);
        }
    }

    public void CheckForUpdatesInBackground()
    {        
        if(_settings.IsAutoUpdateEnabled)
        {
            _thread = new Thread(Update);
            _thread.Start();
        }
    }

    private void Update()
    {
        try
        {
            var releaseInfo = GitHub.GetLatestReleaseInfo(_user, _repository);
            if (IsUpdateAvailable(releaseInfo) && ConfirmUpdate(releaseInfo))
            {
                DownloadAndInstallUpdate(releaseInfo);
            }
        }
        catch (Exception)
        {
            return;
        }
    }

    private bool IsUpdateAvailable(GithubReleaseInfo releaseInfo)
    {
        return releaseInfo.tag_name.Split("_v")[1] != ApplicationVersion;
    }

    private bool ConfirmUpdate(GithubReleaseInfo releaseInfo)
    {
        var confirmed = false;

        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = $"There is a new version available, do you want to update?" +
            $"\n\n{ApplicationVersion} -> {releaseInfo.tag_name.Split("_v")[1]}" +
            $"\n<a href=\"{releaseInfo.html_url}\">Release notes</a>",
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
