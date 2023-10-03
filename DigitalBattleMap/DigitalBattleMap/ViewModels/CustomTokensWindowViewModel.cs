using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using static DigitalBattleMap.Utilities.FileManager;

namespace DigitalBattleMap.ViewModels;

public class CustomTokensWindowViewModel : ViewModelBase
{
    private static string _tokenFilePath = Path.Combine(Constants.TempDirectoryPath, "Token.json");
    private static string _tokenImageFilePath = Path.Combine(Constants.TempDirectoryPath, "Image.png");
    private static string _statblockFilePath = Path.Combine(Constants.TempDirectoryPath, "Markdown.md");
    private IWindowService _windowService;
    private Settings _settings;
    private List<Token> _monsterTokens;
    private List<Token> _tokenList = new();

    public CustomTokensWindowViewModel()
    {
        Initialize();
    }

    public CustomTokensWindowViewModel(IWindowService windowService, Settings settings, List<Token> monsterTokens)
    {
        Initialize();

        _windowService = windowService;
        _settings = settings;
        _monsterTokens = monsterTokens;

        foreach (var token in _settings.CustomTokens.OrderBy(t => t.Name))
        {
            _tokenList.Add(token);
        }
        FilterTokenList();

        foreach (var group in _settings.TokenGroups.OrderBy(t => t.Name))
        {
            GroupList.Add(group);
        }
    }

    private void Initialize()
    {
        SearchText = "";
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

    public ObservableCollection<Token> TokenList { get; set; } = new();
    public ObservableCollection<TokenGroup> GroupList { get; set; } = new();
    public ObservableCollection<string> GroupTokensList { get; set; } = new();
    public Token SelectedToken { get => Get<Token>(); set => Set(value); }
    public List<Token> SelectedTokens { get; set; }
    public TokenGroup SelectedGroup { get => Get<TokenGroup>(); set => Set(value, RefreshGroupTokensListview); }
    public string SelectedGroupToken { get => Get<string>(); set => Set(value); }
    public string SearchText { get => Get<string>(); set => Set(value, FilterTokenList); }
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
        _settings.CustomTokens = _tokenList.ToList();
        _settings.Save();
        SearchText = "";
        FilterTokenList();
    }

    private void SaveTokenGroups()
    {
        _settings.TokenGroups = GroupList.ToList();
        _settings.Save();
    }

    private void AddToken()
    {
        var tokens = new List<Token>(_monsterTokens);
        tokens.AddRange(new List<Token>(_settings.CustomTokens));
        var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokens);
        _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

        if (createTokenWindowViewModel.Token != null)
        {
            _tokenList.Add(createTokenWindowViewModel.Token);
            OrderTokenList();
            SaveCustomTokens();
        }
    }

    private void RemoveToken()
    {
        if (IO.File.Exists(SelectedToken.ImagePath))
        {
            IO.File.Delete(SelectedToken.ImagePath);
        }

        if (SelectedToken.Statblock is MarkdownStatblock markdownStatblock)
        {
            IO.File.Delete(markdownStatblock.MarkdownPath);
        }

        _tokenList.Remove(SelectedToken);
        SaveCustomTokens();
    }

    private void EditToken()
    {
        var tokens = new List<Token>(_monsterTokens);
        tokens.AddRange(new List<Token>(_settings.CustomTokens));
        tokens.Remove(tokens.Single(t => t.Name == SelectedToken.Name));

        var createTokenWindowViewModel = new CreateTokenWindowViewModel(_windowService, tokens, SelectedToken);
        _windowService.ShowWindowDialog<CreateTokenWindow>(createTokenWindowViewModel);

        if (createTokenWindowViewModel.Token != null)
        {
            _tokenList.Remove(SelectedToken);
            _tokenList.Add(createTokenWindowViewModel.Token);
            OrderTokenList();
            SelectedToken = createTokenWindowViewModel.Token;
            SaveCustomTokens();
        }
    }

    private void OrderTokenList()
    {
        _tokenList.OrderCurrentBy(t => t.Name);
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
            if (SelectedTokens.Count == 1)
            {
                Export(path, SelectedToken);
            }
            else
            {
                foreach (var token in SelectedTokens)
                {
                    Export(Path.Combine(Path.GetDirectoryName(path), $"{token.Name}.token"), token);
                }
            }
        }
    }

    private void Export(string path, Token token)
    {
        using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);

        FileManager.SaveFile(token, _tokenFilePath);
        IO.File.Copy(token.ImagePath, _tokenImageFilePath);

        if (token.Statblock is MarkdownStatblock markdownStatblock)
        {
            IO.File.Copy(markdownStatblock.MarkdownPath, _statblockFilePath);
        }

        if (IO.File.Exists(path))
        {
            IO.File.Delete(path);
        }
        IO.ZipFile.CreateFromDirectory(Constants.TempDirectoryPath, path);
    }

    private void Import()
    {
        if (_windowService.ShowOpenFilesDialog(out List<string> paths, "(*.token)|*.token"))
        {
            foreach (var path in paths)
            {
                Import(path);
            }
        }
    }

    private void Import(string path)
    {
        using var tempDirectory = new TempDirectory(Constants.TempDirectoryPath);
        IO.ZipFile.ExtractToDirectory(path, Constants.TempDirectoryPath);

        if (FileManager.OpenFile(_tokenFilePath, new DerivedClassJsonConverter<Statblock>(), out Token token))
        {
            if (_tokenList.SingleOrDefault(t => t.Name == token.Name) == null)
            {
                var imagePath = Path.Combine(Constants.CustomTokensPath, $"{token.Name}.png");
                IO.File.Copy(_tokenImageFilePath, imagePath);
                token.ImagePath = imagePath;

                if (token.Statblock is MarkdownStatblock markdownStatblock)
                {
                    var statblockMarkdownPath = Path.Combine(Constants.CustomTokensPath, $"{token.Name}.md");
                    IO.File.Copy(_statblockFilePath, statblockMarkdownPath);
                    markdownStatblock.MarkdownPath = statblockMarkdownPath;
                }

                _tokenList.Add(token.Copy());
                SaveCustomTokens();
            }
        }
    }

    private void FilterTokenList()
    {
        TokenList.Clear();
        foreach (var token in _tokenList.OrderBy(t => t.Name))
        {
            if (token.Name.ToLower().Contains(SearchText.ToLower()))
            {
                TokenList.Add(token);
            }
        }
    }
}
