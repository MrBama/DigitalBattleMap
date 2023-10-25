using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class CampaignControllerViewModel : ViewModelBase, IPlayerJoiner
{
    private IWindowService _windowService;
    private IMonsterTokens _monsterTokens;
    private Settings _settings;

    public CampaignControllerViewModel()
    {
    }

    public CampaignControllerViewModel(IWindowService windowService, IMonsterTokens monsterTokens, Settings settings)
    {
        _windowService = windowService;
        _monsterTokens = monsterTokens;
        _settings = settings;
        Campaigns = new(settings.Campaigns.Clone().OrderBy(c => c.Name));
        CurrentCampaign = Campaigns.SingleOrDefault(c => string.Equals(c.Name, settings.CurrentCampaingName, StringComparison.CurrentCultureIgnoreCase));
    }

    protected override void InitializeCommands()
    {
        RemoveCampaignCommand = new RelayCommand(p => RemoveCampaign());
        EditCampaignCommand = new RelayCommand(p => EditCampaign());
        AddCampaignCommand = new RelayCommand(p => AddCampaign());
        RemovePlayerCommand = new RelayCommand(p => RemovePlayer());
        EditPlayerCommand = new RelayCommand(p => EditPlayer());
        AddPlayerCommand = new RelayCommand(p => AddPlayer());
        RemoveTokenCommand = new RelayCommand(p => RemoveToken());
        AddTokenCommand = new RelayCommand(p => AddToken());
        SaveCampaignsCommand = new RelayCommand(p => SaveCampaigns());
    }

    public ObservableCollection<Campaign> Campaigns { get; set; } = new();
    public Campaign CurrentCampaign { get => Get<Campaign>(); set => Set(value, CurrentCampaignChanged); }
    public Campaign SelectedCampaign { get => Get<Campaign>(); set => Set(value); }
    public Player SelectedPlayer { get => Get<Player>(); set => Set(value); }
    public TokenIndentifier SelectedToken { get => Get<TokenIndentifier>(); set => Set(value); }
    public bool CampaignListUpdated { get => Get<bool>(); set => Set(value); }

    public ICommand RemoveCampaignCommand { get; set; }
    public ICommand EditCampaignCommand { get; set; }
    public ICommand AddCampaignCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }
    public ICommand EditPlayerCommand { get; set; }
    public ICommand AddPlayerCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand AddTokenCommand { get; set; }
    public ICommand SaveCampaignsCommand { get; set; }

    public void AddTokenToPlayer(TokenIndentifier tokenIndentifier)
    {
        if(CurrentCampaign != null)
        {
            var listSelectionWindowViewModel = new ListSelectionWindowViewModel<Player>(CurrentCampaign.Players);
            _windowService.ShowWindowDialog<ListSelectionWindow>(listSelectionWindowViewModel);
            if(listSelectionWindowViewModel.Success)
            {
                listSelectionWindowViewModel.SelectedItem.TokenIdentifiers.Add(tokenIndentifier);
                CampaignListChanged();
            }
        }
    }

    private void RemoveCampaign()
    {
        if (SelectedCampaign == CurrentCampaign && Campaigns.Count > 1)
        {
            Campaigns.Remove(SelectedCampaign);
            CurrentCampaign = Campaigns.First();
        }
        else
        {
            Campaigns.Remove(SelectedCampaign);
        }

        CampaignListChanged();
    }

    private void EditCampaign()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Campaign name", SelectedCampaign.Name, new ValidateStringInputDelegate(ValidateCampaignNameEdit));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            var currentCampaign = CurrentCampaign;
            SelectedCampaign.Name = stringInputWindowViewModel.Input;
            var selectedCampaign = SelectedCampaign;
            Campaigns.OrderCurrentBy(c => c.Name);
            SelectedCampaign = selectedCampaign;
            CurrentCampaign = currentCampaign;
        }
        CampaignListChanged();
    }

    private void AddCampaign()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Campaign name", new ValidateStringInputDelegate(ValidateCampaignName));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            var currentCampaign = CurrentCampaign;
            var campaign = new Campaign(stringInputWindowViewModel.Input);
            Campaigns.Add(campaign);
            Campaigns.OrderCurrentBy(c => c.Name);
            SelectedCampaign = campaign;
            CurrentCampaign = currentCampaign;

            if (CurrentCampaign == null)
            {
                CurrentCampaign = Campaigns.First();
            }
        }
        CampaignListChanged();
    }

    private void RemovePlayer()
    {
        SelectedCampaign.Players.Remove(SelectedPlayer);
        CampaignListChanged();
    }

    private void EditPlayer()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Player name", SelectedPlayer.Name, new ValidateStringInputDelegate(ValidatePlayerNameEdit));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            SelectedPlayer.Name = stringInputWindowViewModel.Input;
            var selectedPlayer = SelectedPlayer;
            SelectedCampaign.Players.OrderCurrentBy(p => p.Name);
            SelectedPlayer = selectedPlayer;
        }
        CampaignListChanged();
    }

    private void AddPlayer()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Player name", new ValidateStringInputDelegate(ValidatePlayerName));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            var player = new Player(stringInputWindowViewModel.Input);
            SelectedCampaign.Players.Add(player);
            SelectedCampaign.Players.OrderCurrentBy(p => p.Name);
            SelectedPlayer = player;
        }
        CampaignListChanged();
    }

    private bool ValidateCampaignName(string name, out string errorMessage)
    {
        return ValidateName(name, Campaigns.Select(p => p.Name).ToList(), out errorMessage);
    }

    private bool ValidatePlayerName(string name, out string errorMessage)
    {
        return ValidateName(name, SelectedCampaign.Players.Select(p => p.Name).ToList(), out errorMessage);
    }

    private bool ValidateName(string name, List<string> existingNames, out string errorMessage)
    {
        errorMessage = "";
        if (name == null || name == "")
        {
            errorMessage = "Name cannot be empty";
            return false;
        }

        if (existingNames.SingleOrDefault(n => string.Equals(n, name, StringComparison.CurrentCultureIgnoreCase)) != null)
        {
            errorMessage = "Name already exists";
            return false;
        }

        return true;
    }

    private bool ValidatePlayerNameEdit(string name, out string errorMessage)
    {
        return ValidateNameEdit(name, SelectedCampaign.Players.Select(p => p.Name).ToList(), SelectedPlayer.Name, out errorMessage);
    }

    private bool ValidateCampaignNameEdit(string name, out string errorMessage)
    {
        return ValidateNameEdit(name, Campaigns.Select(p => p.Name).ToList(), SelectedCampaign.Name, out errorMessage);
    }

    private bool ValidateNameEdit(string name, List<string> existingNames, string selectedName, out string errorMessage)
    {
        errorMessage = "";
        if (name == null || name == "")
        {
            errorMessage = "Name cannot be empty";
            return false;
        }

        var existingName = existingNames.SingleOrDefault(n => string.Equals(n, name, StringComparison.CurrentCultureIgnoreCase));
        if (existingName != null && !string.Equals(existingName, selectedName, StringComparison.CurrentCultureIgnoreCase))
        {
            errorMessage = "Name already exists";
            return false;
        }

        return true;
    }

    private void RemoveToken()
    {
        SelectedPlayer.TokenIdentifiers.Remove(SelectedToken);
        CampaignListChanged();
    }

    private void AddToken()
    {
        var tokens = new List<Token>(_monsterTokens.GetTokens().Clone());
        tokens.AddRange(_settings.CustomTokens.Clone());

        var selectTokenWindowViewModel = new SelectTokenWindowViewModel(tokens)
        {
            SearchTokenNameOnly = true
        };
        _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

        if (selectTokenWindowViewModel.AddedTokens.Count == 1)
        {
            var token = selectTokenWindowViewModel.AddedTokens.First();
            var tokenIdentifier = new TokenIndentifier(token.Name);
            SelectedPlayer.TokenIdentifiers.Add(tokenIdentifier);
            SelectedToken = tokenIdentifier;
        }

        CampaignListChanged();
    }

    private void SaveCampaigns()
    {
        _settings.Campaigns = new(Campaigns.Clone());
        _settings.CurrentCampaingName = CurrentCampaign?.Name;
        _settings.Save();
        CampaignListChanged();
    }

    private void CampaignListChanged()
    {
        CampaignListUpdated = !Campaigns.EqualsList(_settings.Campaigns) || !(string.Equals(CurrentCampaign?.Name, _settings.CurrentCampaingName, StringComparison.CurrentCultureIgnoreCase));
    }

    private void CurrentCampaignChanged()
    {
        CampaignListChanged();
    }

    // AddToSaveFile (use SaveFile name exclusive?)
    // OpenSaveFile
    // Clear
    // Push names to webserver after changes current campaign
    // Push names to player when editing token listbox
    // Remove PlayerControl
}
