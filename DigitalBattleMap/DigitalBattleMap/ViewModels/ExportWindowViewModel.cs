using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class ExportWindowViewModel : ViewModelBase
{
    private IWindowService _windowService;
    private Settings _settings;

    public ExportWindowViewModel()
    {
        InitializeProperties();
    }

    public ExportWindowViewModel(IWindowService windowService, Settings settings)
    {
        _windowService = windowService;
        _settings = settings;

        InitializeProperties();
    }

    private void InitializeProperties()
    {
        ExportCampaignChecked = true;
    }

    protected override void InitializeCommands()
    {
        ExportCommand = new RelayCommand(p => Export());
    }

    public ObservableCollection<ExportListItem> ExportItemList { get; set; } = new();
    public ExportListItem SelectedExportItem { get => Get<ExportListItem>(); set => Set(value); }
    public List<ExportListItem> SelectedExportItems { get; set; }
    public bool ExportCampaignChecked { get => Get<bool>(); set => Set(value, ExportCampaignToggled); }
    public bool ExportTokenGroupChecked { get => Get<bool>(); set => Set(value, ExportTokenGroupToggled); }
    public bool ExportTokenChecked { get => Get<bool>(); set => Set(value, ExportTokenToggled); }

    public ICommand ExportCommand { get; set; }

    private void ExportCampaignToggled()
    {
        if (ExportCampaignChecked && _settings != null)
        {
            ExportItemList.Clear();
            foreach (var campaign in _settings.Campaigns.OrderBy(t => t.Name))
            {
                ExportItemList.Add(new ExportListItem(campaign.Name, (string path) => ImportExport.Export(path, campaign, _settings.CustomTokens.ToList())));
            }
        }
    }

    private void ExportTokenGroupToggled()
    {
        if (ExportTokenGroupChecked && _settings != null)
        {
            ExportItemList.Clear();
            foreach (var tokenGroup in _settings.TokenGroups.OrderBy(t => t.Name))
            {
                ExportItemList.Add(new ExportListItem(tokenGroup.Name, (string path) => ImportExport.Export(path, tokenGroup, _settings.CustomTokens.ToList())));
            }
        }
    }

    private void ExportTokenToggled()
    {
        if (ExportTokenChecked && _settings != null)
        {
            ExportItemList.Clear();
            foreach (var token in _settings.CustomTokens.OrderBy(t => t.Name))
            {
                ExportItemList.Add(new ExportListItem(token.Name, (string path) => ImportExport.Export(path, token)));
            }
        }
    }

    private void Export()
    {
        if (_windowService.ShowSaveFileDialog(out string path, SelectedExportItem.Name, ""))
        {
            if (SelectedExportItems.Count == 1)
            {
                SelectedExportItem.Export(path);
            }
            else
            {
                foreach (var exportItem in SelectedExportItems)
                {
                    exportItem.Export(Path.Combine(Path.GetDirectoryName(path), $"{exportItem.Name}"));
                }
            }
        }
    }
}

public class ExportListItem
{
    private Action<string> _export;

    public ExportListItem(string name, Action<string> exportAction)
    {
        Name = name;
        _export = exportAction;
    }

    public string Name { get; set; }

    public void Export(string path)
    {
        _export(path);
    }
}
