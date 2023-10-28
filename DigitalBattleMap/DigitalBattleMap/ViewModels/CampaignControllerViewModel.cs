using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class CampaignControllerViewModel : ViewModelBase, IPlayerJoiner
{
    private IWindowService _windowService;
    private IWebCommunication _webCommunication;
    private IMonsterTokens _monsterTokens;
    private Settings _settings;
    private bool _isCurrentCampaignUpdatedFromUI = true;

    public CampaignControllerViewModel()
    {
    }

    public CampaignControllerViewModel(IWindowService windowService, IWebCommunication webCommunication, IMonsterTokens monsterTokens, Settings settings)
    {
        _windowService = windowService;
        _webCommunication = webCommunication;
        _monsterTokens = monsterTokens;
        _settings = settings;
        _webCommunication.OnConnected += OnWebCommunicationConnected;
        _webCommunication.OnGetTokens += OnGetTokens;
        Campaigns = new(settings.Campaigns.Clone().OrderBy(c => c.Name));
        SetCurrentCampaign(Campaigns.SingleOrDefault(c => string.Equals(c.Name, settings.CurrentCampaignName, StringComparison.CurrentCultureIgnoreCase)));
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
        if (CurrentCampaign != null)
        {
            var listSelectionWindowViewModel = new ListSelectionWindowViewModel<Player>(CurrentCampaign.Players);
            _windowService.ShowWindowDialog<ListSelectionWindow>(listSelectionWindowViewModel);
            if (listSelectionWindowViewModel.Success)
            {
                var player = listSelectionWindowViewModel.SelectedItem;
                player.TokenIdentifiers.Add(tokenIndentifier);
                _webCommunication.SendMessage(new TokensMessage { Player = player.Name, Tokens = player.TokenIdentifiers.ToStringList() });
                CampaignListChanged();
            }
        }
    }

    private void RemoveCampaign()
    {
        if (SelectedCampaign == CurrentCampaign && Campaigns.Count > 1)
        {
            Campaigns.Remove(SelectedCampaign);
            SetCurrentCampaign(Campaigns.First());
            _webCommunication.SendMessage(new CampaignMessage());
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
            SetCurrentCampaign(currentCampaign);
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
            SetCurrentCampaign(currentCampaign);

            if (CurrentCampaign == null)
            {
                SetCurrentCampaign(Campaigns.First());
                _webCommunication.SendMessage(new CampaignMessage { Players = new List<Player>(Campaigns.First().Players.Clone()) });
            }
        }
        CampaignListChanged();
    }

    private void RemovePlayer()
    {
        _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name });
        SelectedCampaign.Players.Remove(SelectedPlayer);

        CampaignListChanged();
    }

    private void EditPlayer()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Player name", SelectedPlayer.Name, new ValidateStringInputDelegate(ValidatePlayerNameEdit));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name });

            SelectedPlayer.Name = stringInputWindowViewModel.Input;
            var selectedPlayer = SelectedPlayer;
            SelectedCampaign.Players.OrderCurrentBy(p => p.Name);
            SelectedPlayer = selectedPlayer;

            _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
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
            _webCommunication.SendMessage(new TokensMessage { Player = player.Name, Tokens = player.TokenIdentifiers.ToStringList() });
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
        _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
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
            _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
        }

        CampaignListChanged();
    }

    private void SaveCampaigns()
    {
        _settings.Campaigns = new(Campaigns.Clone());
        _settings.CurrentCampaignName = CurrentCampaign?.Name;
        _settings.Save();
        CampaignListChanged();
    }

    private void CampaignListChanged()
    {
        CampaignListUpdated = !Campaigns.EqualsList(_settings.Campaigns) || !(string.Equals(CurrentCampaign?.Name, _settings.CurrentCampaignName, StringComparison.CurrentCultureIgnoreCase));
    }

    private void CurrentCampaignChanged()
    {
        if (_isCurrentCampaignUpdatedFromUI)
        {
            if (CurrentCampaign != null)
            {
                _webCommunication.SendMessage(new CampaignMessage { Players = new List<Player>(CurrentCampaign.Players.Clone()) });
            }
            CampaignListChanged();
        }
    }

    private void SetCurrentCampaign(Campaign campaign)
    {
        _isCurrentCampaignUpdatedFromUI = false;
        CurrentCampaign = campaign;
        _isCurrentCampaignUpdatedFromUI = true;
    }

    private void OnWebCommunicationConnected(object? sender, EventArgs e)
    {
        if (CurrentCampaign != null)
        {
            _webCommunication.SendMessage(new CampaignMessage { Players = new List<Player>(CurrentCampaign.Players.Clone()) });
        }
    }

    private void OnGetTokens(object sender, GetTokensEventArgs e)
    {
        if (CurrentCampaign != null)
        {
            foreach (var player in CurrentCampaign.Players)
            {
                if(string.Equals(e.Player, player.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    _webCommunication.SendMessage(new TokensMessage { Player = e.Player, Tokens = player.TokenIdentifiers.ToStringList() });
                }
            }
        }
    }

    // AddToSaveFile (use SaveFile name exclusive?)
    // OpenSaveFile
    // Remove PlayerControl
    // What happens when there is no cookie
    // Combine grid tab with background tab
}
