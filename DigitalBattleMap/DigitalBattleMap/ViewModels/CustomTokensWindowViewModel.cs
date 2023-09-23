using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class CustomTokensWindowViewModel : ViewModelBase
{
    private static string _tokenFilePath = Path.Combine(Constants.TempDirectoryPath, "Token.json");
    private static string _tokenImageFilePath = Path.Combine(Constants.TempDirectoryPath, "Image.png");
    private IWindowService _windowService;
    private Settings _settings;
    private List<Token> _monsterTokens;

    public CustomTokensWindowViewModel()
    {
    }

    public CustomTokensWindowViewModel(IWindowService windowService, Settings settings, List<Token> monsterTokens)
    {
        _windowService = windowService;
        _settings = settings;
        _monsterTokens = monsterTokens;

        foreach (var token in _settings.CustomTokens.OrderBy(t => t.Name))
        {
            TokenList.Add(token);
        }

        foreach (var group in _settings.TokenGroups.OrderBy(t => t.Name))
        {
            GroupList.Add(group);
        }
    }

    protected override void InitializeCommands()
    {
        AddTokenCommand = new RelayCommand(p => AddToken());
        RemoveTokenCommand = new RelayCommand(p => RemoveToken());
        EditTokenCommand = new RelayCommand(p => EditToken());
        AddGroupCommand = new RelayCommand(p => AddGroup());
        RemoveGroupCommand = new RelayCommand(p => RemoveGroup());
        EditGroupCommand = new RelayCommand(p => EditGroup());
        AddGroupTokenCommand = new RelayCommand(p => AddGroupToken());
        RemoveGroupTokenCommand = new RelayCommand(p => RemoveGroupToken());
        ExportCommand = new RelayCommand(p => Export());
        ImportCommand = new RelayCommand(p => Import());
    }

    public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
    public ObservableCollection<TokenGroup> GroupList { get; set; } = new ObservableCollection<TokenGroup>();
    public ObservableCollection<string> GroupTokensList { get; set; } = new ObservableCollection<string>();
    public Token SelectedToken { get => Get<Token>(); set => Set(value); }
    public TokenGroup SelectedGroup { get => Get<TokenGroup>(); set => Set(value, RefreshGroupTokensListview); }
    public string SelectedGroupToken { get => Get<string>(); set => Set(value); }
    public ICommand AddTokenCommand { get; set; }
    public ICommand RemoveTokenCommand { get; set; }
    public ICommand EditTokenCommand { get; set; }
    public ICommand AddGroupCommand { get; set; }
    public ICommand RemoveGroupCommand { get; set; }
    public ICommand EditGroupCommand { get; set; }
    public ICommand AddGroupTokenCommand { get; set; }
    public ICommand RemoveGroupTokenCommand { get; set; }
    public ICommand ExportCommand { get; set; }
    public ICommand ImportCommand { get; set; }

    private void SaveCustomTokens()
    {
        _settings.CustomTokens = TokenList.ToList();
        _settings.Save();
    }

    private void SaveTokenGroups()
    {
        _settings.TokenGroups = GroupList.ToList();
        _settings.Save();
    }

    private void AddToken()
    {
        var tokenNames = _monsterTokens.Select(t => t.Name).ToList();
        tokenNames.AddRange(_settings.CustomTokens.Select(t => t.Name));
        var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokenNames);
        _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

        if (createTokenWindowViewModel.Token != null)
        {
            TokenList.Add(createTokenWindowViewModel.Token);
            OrderTokenList();
            SaveCustomTokens();
        }
    }

    private void RemoveToken()
    {
        IO.File.Delete(SelectedToken.ImagePath);
        TokenList.Remove(SelectedToken);
        SaveCustomTokens();
    }

    private void EditToken()
    {
        var tokenNames = _monsterTokens.Select(t => t.Name).ToList();
        tokenNames.AddRange(_settings.CustomTokens.Select(t => t.Name));
        tokenNames.Remove(SelectedToken.Name);

        var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokenNames, SelectedToken);
        _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

        if (createTokenWindowViewModel.Token != null)
        {
            TokenList.Remove(SelectedToken);
            TokenList.Add(createTokenWindowViewModel.Token);
            OrderTokenList();
            SelectedToken = createTokenWindowViewModel.Token;
            SaveCustomTokens();
        }
    }

    private void OrderTokenList()
    {
        TokenList.OrderCurrentBy(t => t.Name);
    }

    private void OrderGroupList()
    {
        GroupList.OrderCurrentBy(t => t.Name);
    }

    private void AddGroup()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Group name", (p) => p != "");
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            GroupList.Add(new TokenGroup { Name = stringInputWindowViewModel.Input });
            OrderGroupList();
            SaveTokenGroups();
        }
    }

    private void RemoveGroup()
    {
        GroupList.Remove(SelectedGroup);
        SaveTokenGroups();
    }

    private void EditGroup()
    {
        var stringInputWindowViewModel = new StringInputWindowViewModel("Group name", SelectedGroup.Name, (p) => p != "");
        _windowService.ShowWindowDialog<StringInputWindow>(stringInputWindowViewModel);

        if (stringInputWindowViewModel.Success)
        {
            SelectedGroup.Name = stringInputWindowViewModel.Input;
            OrderGroupList();
            SelectedGroup = GroupList.Single(g => g.Name == stringInputWindowViewModel.Input);
            SaveTokenGroups();
        }
    }

    private void AddGroupToken()
    {
        var tokens = new List<Token>(_monsterTokens);
        tokens.AddRange(_settings.CustomTokens);

        var selectTokenWindowViewModel = new SelectTokenWindowViewModel(tokens, _settings.TokenGroups);
        _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

        if (selectTokenWindowViewModel.AddedTokens.Count > 0)
        {
            foreach (var token in selectTokenWindowViewModel.AddedTokens)
            {
                SelectedGroup.TokenNames.Add(token.Name);
            }
            RefreshGroupTokensListview();
            SaveTokenGroups();
        }
    }

    private void RemoveGroupToken()
    {
        SelectedGroup.TokenNames.Remove(SelectedGroupToken);
        RefreshGroupTokensListview();
        SaveTokenGroups();
    }

    private void RefreshGroupTokensListview()
    {
        GroupTokensList.Clear();
        if (SelectedGroup != null)
        {
            var orderedGroupTokens = SelectedGroup.TokenNames.OrderBy(t => t).ToList();
            foreach (var tokenName in orderedGroupTokens)
            {
                GroupTokensList.Add(tokenName);
            }
        }
    }

    private void Export()
    {
        if (_windowService.ShowSaveFileDialog(out string path, SelectedToken.Name, "(*.token)|*.token"))
        {
            using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);

            FileManager.SaveFile(SelectedToken, _tokenFilePath);
            IO.File.Copy(SelectedToken.ImagePath, _tokenImageFilePath);

            if (IO.File.Exists(path))
            {
                IO.File.Delete(path);
            }
            IO.ZipFile.CreateFromDirectory(Constants.TempDirectoryPath, path);
        }
    }

    private void Import()
    {
        if (_windowService.ShowOpenFileDialog(out string path, "(*.token)|*.token"))
        {
            using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);
            IO.ZipFile.ExtractToDirectory(path, Constants.TempDirectoryPath);

            if (FileManager.OpenFile(_tokenFilePath, out Token token))
            {
                if (TokenList.SingleOrDefault(t => t.Name == token.Name) == null)
                {
                    var imagePath = Path.Combine(Constants.CustomTokensPath, $"{token.Name}.png");
                    IO.File.Copy(_tokenImageFilePath, imagePath);
                    token.ImagePath = imagePath;
                    TokenList.Add(token.Copy());
                    SaveCustomTokens();
                }
            }
        }
    }
}
