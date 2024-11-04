using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class WebExtensionsViewModel : ViewModelBase
{
    private Dictionary<string, string> _webExtensionVersions;
    private IWebExtension _uBlockOriginWebExtension = new UBlockOriginWebExtension();
    private IWindowService _windowService;
    private ConfirmationWindowViewModel _confirmationWindowViewModel = new ConfirmationWindowViewModel
    {
        IsLeftButtonVisible = false,
        IsRightButtonVisible = false,
        IsMiddleButtonVisible = true,
        MiddleButtonContent = "Ok"
    };

    public WebExtensionsViewModel()
    {
    }

    public WebExtensionsViewModel(Dictionary<string, string> webExtensionVersions, IWindowService windowService)
    {
        _webExtensionVersions = webExtensionVersions;
        _windowService = windowService;

        if(webExtensionVersions.ContainsKey(_uBlockOriginWebExtension.Name))
        {
            IsUBlockOriginInstalled = true;
            UBlockOriginVersion = _webExtensionVersions[_uBlockOriginWebExtension.Name];
        }

        // This will add the extensions back if the caching directory for WebView was removed.
        AddExtensionsToWebView();
    }

    protected override void InitializeCommands()
    {
        InstallUBlockOriginCommand = new RelayCommand(p => InstallUBlockOrigin());
        UpdateUBlockOriginCommand = new RelayCommand(p => UpdateUBlockOrigin());
    }

    public bool InstalledOrUpdatedExtension { get; set; }

    public ICommand InstallUBlockOriginCommand { get; set; }
    public ICommand UpdateUBlockOriginCommand { get; set; }

    public bool IsUBlockOriginInstalled { get => Get<bool>(); set => Set(value); }
    public string UBlockOriginVersion { get => Get<string>(); set => Set(value); }

    // This is a workaround. For some reason it's only possible to add extensions to WebView2 from the loaded event.
    // The UI contains a ListView which is connected to this list. Everytime an item is added, the loaded event is called.
    public ObservableCollection<Dictionary<string, string>> WebExtensionVersionsList { get; set; } = new();

    private void InstallUBlockOrigin()
    {

        if (_uBlockOriginWebExtension.Install())
        {
            _webExtensionVersions[_uBlockOriginWebExtension.Name] = _uBlockOriginWebExtension.Version;
            UBlockOriginVersion = _uBlockOriginWebExtension.Version;
            IsUBlockOriginInstalled = true;
            InstalledOrUpdatedExtension = true;
            AddExtensionsToWebView();
        }
        else
        {
            _confirmationWindowViewModel.Content = $"Failed to install: {_uBlockOriginWebExtension.Name}";
            _windowService.ShowWindowDialog<ConfirmationWindow>(_confirmationWindowViewModel);
        }
    }

    private void UpdateUBlockOrigin()
    {
        if(_uBlockOriginWebExtension.IsUpdateAvailable(_webExtensionVersions[_uBlockOriginWebExtension.Name]))
        {
            if (_uBlockOriginWebExtension.Update(_webExtensionVersions[_uBlockOriginWebExtension.Name]))
            {
                _webExtensionVersions[_uBlockOriginWebExtension.Name] = _uBlockOriginWebExtension.Version;
                UBlockOriginVersion = _uBlockOriginWebExtension.Version;
                IsUBlockOriginInstalled = true;
                InstalledOrUpdatedExtension = true;
            }
            else
            {
                _confirmationWindowViewModel.Content = $"Failed to update: {_uBlockOriginWebExtension.Name}";
                _windowService.ShowWindowDialog<ConfirmationWindow>(_confirmationWindowViewModel);
            }
        }
        else
        {
            _confirmationWindowViewModel.Content = $"The latest version is already installed.";
            _windowService.ShowWindowDialog<ConfirmationWindow>(_confirmationWindowViewModel);
        }
    }

    private void AddExtensionsToWebView()
    {
        WebExtensionVersionsList.Clear();
        WebExtensionVersionsList.Add(new Dictionary<string, string>(_webExtensionVersions));
    }
}
