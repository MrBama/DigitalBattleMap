using DigitalBattleMap.Common;
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

public class CampaignControllerViewModel : ViewModelBase, IPlayers
{
    private IWindowService _windowService;
    private IWebCommunication _webCommunication;
    private IMonsterTokens _monsterTokens;
    private Settings _settings;
    private bool _isInternalUpdate = false;

    public CampaignControllerViewModel()
    {
        MouseCanvas = new MouseCanvasViewModel(); // do nothing with the mouse
    }

    public CampaignControllerViewModel(IWindowService windowService, IWebCommunication webCommunication, IMonsterTokens monsterTokens, Settings settings)
    {
        MouseCanvas = new MouseCanvasViewModel(); // do nothing with the mouse
        _windowService = windowService;
        _webCommunication = webCommunication;
        _monsterTokens = monsterTokens;
        _settings = settings;
        _settings.OnSettingChanged += SettingChanged;
        _webCommunication.OnConnected += OnWebCommunicationConnected;
        _webCommunication.OnSetOrientation += SetOrientation;
        Campaigns = new(settings.Campaigns.Clone().OrderBy(c => c.Name));
        CurrentCampaign = Campaigns.SingleOrDefault(c => string.Equals(c.Name, settings.CurrentCampaignName, StringComparison.CurrentCultureIgnoreCase));
        ExpandCurrentCampaign();
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
        CurrentCampaignChangedCommand = new RelayCommand(p => CurrentCampaignChanged((System.Windows.Controls.SelectionChangedEventArgs)p));
    }

    public event EventHandler<TokensOrientationChangedEventArgs> OnOrientationChanged;

    public ObservableCollection<Campaign> Campaigns { get; set; } = new();
    public Campaign CurrentCampaign { get => Get<Campaign>(); set => Set(value); }
    public Campaign SelectedCampaign { get => Get<Campaign>(); set => Set(value, SelectedCampaignChanged); }
    public Player SelectedPlayer { get => Get<Player>(); set => Set(value); }
    public TokenIdentifier SelectedToken { get => Get<TokenIdentifier>(); set => Set(value); }
    public MouseCanvasViewModel MouseCanvas { get => Get<MouseCanvasViewModel>(); private set => Set(value); }

    public ICommand RemoveCampaignCommand { get; set; }
    public ICommand EditCampaignCommand { get; set; }
    public ICommand AddCampaignCommand { get; set; }
    public ICommand RemovePlayerCommand { get; set; }
    public ICommand EditPlayerCommand { get; set; }
    public ICommand AddPlayerCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand AddTokenCommand { get; set; }
    public ICommand CurrentCampaignChangedCommand { get; set; }

    public void AddTokenToPlayer(TokenIdentifier tokenIdentifier)
    {
        if (CurrentCampaign != null)
        {
            var listSelectionWindowViewModel = new ListSelectionWindowViewModel<Player>(CurrentCampaign.Players);
            _windowService.ShowWindowDialog<ListSelectionWindow>(listSelectionWindowViewModel);
            if (listSelectionWindowViewModel.Success)
            {
                var player = listSelectionWindowViewModel.SelectedItem;
                var tempTokenIdentifier = new TempTokenIdentifier(tokenIdentifier);
                player.TokenIdentifiers.Add(tempTokenIdentifier);
                _webCommunication.SendMessage(new TokensMessage { Player = player.Name, Tokens = player.TokenIdentifiers.ToStringList() });
            }
        }
    }

    public bool IsTokenControlledByPlayer(TokenIdentifier tokenIdentifier)
    {
        if (CurrentCampaign != null)
        {
            foreach (var player in CurrentCampaign.Players)
            {
                if (player.TokenIdentifiers.Contains(tokenIdentifier))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public string GetPlayerName(TokenIdentifier tokenIdentifier)
    {
        if (CurrentCampaign != null)
        {
            foreach (var player in CurrentCampaign.Players)
            {
                if (player.TokenIdentifiers.Contains(tokenIdentifier))
                {
                    return player.Name;
                }
            }
        }

        return "";
    }

    public bool TryGetOrientation(TokenIdentifier tokenIdentifier, out TokenOrientation orientation)
    {
        orientation = TokenOrientation.West;

        if (CurrentCampaign != null)
        {
            if (IsTokenControlledBySinglePlayer(tokenIdentifier))
            {
                var player = CurrentCampaign.Players.Single(p => p.TokenIdentifiers.Contains(tokenIdentifier));
                orientation = ConvertToTokenOrientation(player.Orientation);
                return true;
            }
        }

        return false;
    }

    private void RemoveCampaign()
    {
        var confirmed = false;
        var confirmationWindowViewModel = new ConfirmationWindowViewModel
        {
            Content = $"Are you sure you want to remove the \"{SelectedCampaign.Name}\" campaign?",
            LeftButtonAction = () => { confirmed = true; }
        };
        _windowService.ShowWindowDialog<ConfirmationWindow>(confirmationWindowViewModel);

        if(confirmed)
        {
            if (SelectedCampaign.Equals(CurrentCampaign) && Campaigns.Count > 1)
            {
                Campaigns.Remove(SelectedCampaign);
                CurrentCampaign = Campaigns.First();
                _webCommunication.SendMessage(new CampaignMessage());
            }
            else
            {
                Campaigns.Remove(SelectedCampaign);
            }

            Save();
        }
    }

    private void EditCampaign()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Campaign name", SelectedCampaign.Name, new ValidateStringInputDelegate(ValidateCampaignNameEdit));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            SelectedCampaign.Name = stringInputWindowViewModel.Input;
            var campaign = SelectedCampaign;
            OrderCampaigns();
            SelectedCampaign = campaign;
            NotifyPropertyChange(nameof(Campaigns));
            Save();
        }
    }

    private void AddCampaign()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Campaign name", new ValidateStringInputDelegate(ValidateCampaignName));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            var campaign = new Campaign(stringInputWindowViewModel.Input);
            Campaigns.Add(campaign);
            OrderCampaigns();
            SelectedCampaign = campaign;

            if (CurrentCampaign == null)
            {
                CurrentCampaign = Campaigns.First();
                _webCommunication.SendMessage(new CampaignMessage { Players = new List<Player>(Campaigns.First().Players.Clone()) });
            }

            Save();
        }
    }

    private void RemovePlayer()
    {
        if (SelectedCampaign.Equals(CurrentCampaign))
        {
            _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name });
        }

        SelectedCampaign.Players.Remove(SelectedPlayer);

        Save();
    }

    private void EditPlayer()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Player name", SelectedPlayer.Name, new ValidateStringInputDelegate(ValidatePlayerNameEdit));
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            if (SelectedCampaign.Equals(CurrentCampaign))
            {
                _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name });
            }

            SelectedPlayer.Name = stringInputWindowViewModel.Input;
            var selectedPlayer = SelectedPlayer;
            SelectedCampaign.Players.OrderCurrentBy(p => p.Name);
            SelectedPlayer = selectedPlayer;

            if (SelectedCampaign.Equals(CurrentCampaign))
            {
                _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
            }

            Save();
        }
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

            Save();
        }
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
        var isTempTokenIdentifier = SelectedToken is TempTokenIdentifier;
        SelectedPlayer.TokenIdentifiers.Remove(SelectedToken);

        if (SelectedCampaign.Equals(CurrentCampaign))
        {
            _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
        }

        // Temporary tokens are not saved
        if (!isTempTokenIdentifier)
        {
            Save();
        }
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
            var tokenIdentifier = new TokenIdentifier(token.Name);
            SelectedPlayer.TokenIdentifiers.Add(tokenIdentifier);
            SelectedToken = tokenIdentifier;
            if (SelectedCampaign.Equals(CurrentCampaign))
            {
                _webCommunication.SendMessage(new TokensMessage { Player = SelectedPlayer.Name, Tokens = SelectedPlayer.TokenIdentifiers.ToStringList() });
            }

            Save();
        }
    }

    private void Save()
    {
        _isInternalUpdate = true;
        var campaigns = Campaigns.Clone();

        // Remove temporarily added token identifiers
        foreach (var campaign in campaigns)
        {
            foreach (var player in campaign.Players)
            {
                player.TokenIdentifiers.RemoveAll(t => t is TempTokenIdentifier);
            }
        }

        _settings.Campaigns = [.. campaigns];
        _settings.CurrentCampaignName = CurrentCampaign?.Name;
        _settings.Save();
        _isInternalUpdate = false;
    }

    private void CurrentCampaignChanged(System.Windows.Controls.SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if(selectionChangedEventArgs.AddedItems.Count == 1 && selectionChangedEventArgs.RemovedItems.Count == 1)
        {
            _isInternalUpdate = true;
            if (CurrentCampaign != null)
            {
                _webCommunication.SendMessage(new CampaignMessage { Players = [.. CurrentCampaign.Players.Clone()] });
            }
            Save();
            _isInternalUpdate = false;
        }
    }

    private void OnWebCommunicationConnected(object? sender, EventArgs e)
    {
        if (CurrentCampaign != null)
        {
            _webCommunication.SendMessage(new CampaignMessage { Players = new List<Player>(CurrentCampaign.Players.Clone()) });
        }
    }

    private void SetOrientation(object? sender, SetOrientationEventArgs e)
    {
        if (CurrentCampaign != null)
        {
            if (TryGetPlayer(e.Player, out var player))
            {
                if (player.Orientation != e.Orientation)
                {
                    player.Orientation = e.Orientation;
                    InvokeOrientationUpdate(player);
                }
            }
        }
    }

    private void ExpandCurrentCampaign()
    {
        if (CurrentCampaign != null)
        {
            SelectedCampaign = CurrentCampaign;
            if (CurrentCampaign.Players.Count > 0)
            {
                SelectedPlayer = CurrentCampaign.Players.First();
                if (SelectedPlayer.TokenIdentifiers.Count > 0)
                {
                    SelectedToken = SelectedPlayer.TokenIdentifiers.First();
                }
            }
        }
    }

    private bool TryGetPlayer(string playerName, out Player player)
    {
        foreach (var p in CurrentCampaign.Players)
        {
            if (string.Equals(playerName, p.Name, StringComparison.CurrentCultureIgnoreCase))
            {
                player = p;
                return true;
            }
        }

        player = null;
        return false;
    }

    private void InvokeOrientationUpdate(Player player)
    {
        var eventArgs = new TokensOrientationChangedEventArgs();

        foreach (var tokenIdentifier in player.TokenIdentifiers)
        {
            if (IsTokenControlledBySinglePlayer(tokenIdentifier))
            {
                eventArgs.TokenIdentifiers.Add(tokenIdentifier);
            }
        }

        if (eventArgs.TokenIdentifiers.Any())
        {
            eventArgs.Orientation = ConvertToTokenOrientation(player.Orientation);
            OnOrientationChanged?.Invoke(this, eventArgs);
        }
    }

    private TokenOrientation ConvertToTokenOrientation(Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.Up:
                return TokenOrientation.North;
            case Orientation.Left:
                return TokenOrientation.East;
            case Orientation.Down:
                return TokenOrientation.South;
            case Orientation.Right:
                return TokenOrientation.West;
            default:
                throw new NotImplementedException();
        }
    }

    private bool IsTokenControlledBySinglePlayer(TokenIdentifier tokenIdentifier)
    {
        var players = CurrentCampaign.Players.Where(p => p.TokenIdentifiers.Contains(tokenIdentifier));
        return players.Count() == 1;
    }

    private void SettingChanged(object? sender, SettingChangedEventArgs e)
    {
        // This is triggered by an import of a campaign file
        if (e.SettingName == nameof(Settings.Campaigns) && !_isInternalUpdate)
        {
            var tempTokenIdentiefiers = GetTempTokenIdentifiers();
            var currentCampaign = CurrentCampaign!.Name;

            CurrentCampaign = null;
            Campaigns = new(_settings.Campaigns.Clone().OrderBy(c => c.Name));
            CurrentCampaign = Campaigns.Single(c => c.Name == currentCampaign);
            AddTempTokenIdentifiers(tempTokenIdentiefiers);

            NotifyPropertyChange(nameof(Campaigns));
        }
    }

    private void OrderCampaigns()
    {
        var currentCampaign = CurrentCampaign;
        Campaigns.OrderCurrentBy(c => c.Name);
        CurrentCampaign = currentCampaign;
    }

    private List<(string, string, TokenIdentifier)> GetTempTokenIdentifiers()
    {
        var tempTokensMap = new List<(string, string, TokenIdentifier)>();
        foreach (var campaign in Campaigns)
        {
            foreach (var player in campaign.Players)
            {
                foreach (var token in player.TokenIdentifiers)
                {
                    if (token is TempTokenIdentifier)
                    {
                        tempTokensMap.Add((campaign.Name, player.Name, token));
                    }
                }
            }
        }
        return tempTokensMap;
    }

    private void AddTempTokenIdentifiers(List<(string campaign, string player, TokenIdentifier token)> tempTokenIdentifiers)
    {
        foreach (var campaign in Campaigns)
        {
            foreach (var player in campaign.Players)
            {
                var tempTokens = tempTokenIdentifiers.Where(t => t.campaign == campaign.Name && t.player == player.Name);
                foreach (var tempToken in tempTokens)
                {
                    player.TokenIdentifiers.Add(tempToken.token);
                }
            }
        }
    }

    private void SelectedCampaignChanged()
    {
        if(SelectedCampaign != null && SelectedCampaign.Players.Count != 0)
        {
            SelectedPlayer = SelectedCampaign.Players.First();

            if(SelectedPlayer.TokenIdentifiers.Count != 0)
            {
                SelectedToken = SelectedPlayer.TokenIdentifiers.First();
            }
        }
    }

    private class TempTokenIdentifier : TokenIdentifier
    {
        public TempTokenIdentifier(TokenIdentifier tokenIdentifier) : base(tokenIdentifier.Name, tokenIdentifier.Id)
        { 
        }
    }
}
