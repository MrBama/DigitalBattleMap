using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Utilities;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace DigitalBattleMap.ViewModels;

public class SelectTokenWindowViewModel : ViewModelBase
{
    private List<Token> _tokens = new();
    private List<TokenGroup> _groups = new();

    public SelectTokenWindowViewModel()
    {
        InitializeProperties();
    }

    public SelectTokenWindowViewModel(List<Token> tokens, List<TokenGroup> tokenGroups)
    {
        InitializeProperties();
        _tokens = tokens.OrderBy(t => t.Name).ToList();
        _groups = tokenGroups.OrderBy(t => t.Name).ToList();

        foreach (var token in _tokens)
        {
            TokenList.Add(token);
        }

        foreach (var group in _groups)
        {
            GroupList.Add(group);
        }

        SelectedToken = TokenList.FirstOrDefault();
        SelectedGroup = GroupList.FirstOrDefault();
    }

    public SelectTokenWindowViewModel(List<Token> tokens)
    {
        InitializeProperties();

        _tokens = tokens.OrderBy(t => t.Name).ToList();

        foreach (var token in _tokens)
        {
            TokenList.Add(token);
        }

        SelectedToken = TokenList.FirstOrDefault();
    }

    protected override void InitializeCommands()
    {
        AddCommand = new RelayCommand(p => AddButton());
        KeyDownCommand = new RelayCommand(p => KeyDown((KeyEventArgs)p));
    }

    public string SearchText { get => Get<string>(); set => Set(value); }
    public Token SelectedToken { get => Get<Token>(); set => Set(value, OnSelectedTokenChanged); }
    public TokenGroup SelectedGroup { get => Get<TokenGroup>(); set => Set(value); }
    public int NumberOfTokens { get => Get<int>(); set => Set(value); }
    public int SelectedTabIndex { get => Get<int>(); set => Set(value); }
    public bool SearchTokenNameOnly { get => Get<bool>(); set => Set(value); }
    public TokenSize SelectedTokenSize { get => Get<TokenSize>(); set => Set(value); }
    public ObservableCollection<Token> TokenList { get; set; } = new ObservableCollection<Token>();
    public ObservableCollection<TokenGroup> GroupList { get; set; } = new ObservableCollection<TokenGroup>();
    public bool IsTokenSelected { get => AreTokensSelected(); }
    public List<Token> AddedTokens { get; set; } = new();
    public List<object> FilteredTokenList { get; set; } = new();
    public List<object> FilteredGroupList { get; set; } = new();

    public ICommand AddCommand { get; set; }
    public ICommand KeyDownCommand { get; set; }

    private void InitializeProperties()
    {
        RegisterPropertyChangedWatcher(nameof(IsTokenSelected), new List<string> { nameof(SelectedToken), nameof(SelectedGroup), nameof(SelectedTabIndex) });

        SearchText = "";
        NumberOfTokens = 1;
        SelectedTokenSize = TokenSize.Medium;
    }

    private void AddButton()
    {
        if (SelectedTabIndex == 0)
        {
            for (int i = 0; i < NumberOfTokens; i++)
            {
                var copy = SelectedToken.Clone<Token>();
                copy.Size = SelectedTokenSize;
                AddedTokens.Add(copy);
            }
        }
        else
        {
            foreach (var tokenName in SelectedGroup.TokenNames)
            {
                var token = _tokens.SingleOrDefault(t => t.Name == tokenName);
                if (token != null)
                {
                    AddedTokens.Add(token.Clone<Token>());
                }
            }
        }
    }

    private void OnSelectedTokenChanged()
    {
        NumberOfTokens = 1;
        if (SelectedToken != null)
        {
            SelectedTokenSize = SelectedToken.Size;
        }
        else
        {
            SelectedTokenSize = TokenSize.Medium;
        }
    }

    private bool AreTokensSelected()
    {
        if (SelectedTabIndex == 0)
        {
            return SelectedToken != null;
        }
        else
        {
            return SelectedGroup != null;
        }
    }

    private void KeyDown(KeyEventArgs keyEventArgs)
    {
        if (SelectedTabIndex == 0)
        {
            if (keyEventArgs.Key == Key.Down)
            {
                var index = FilteredTokenList.IndexOf(SelectedToken);
                if (index != -1 && index < FilteredTokenList.Count - 1)
                {
                    SelectedToken = (Token)FilteredTokenList[index + 1];
                }
            }

            if (keyEventArgs.Key == Key.Up)
            {
                var index = FilteredTokenList.IndexOf(SelectedToken);
                if (index != -1 && index > 0)
                {
                    SelectedToken = (Token)FilteredTokenList[index - 1];
                }
            }
        }
        else
        {
            if (keyEventArgs.Key == Key.Down)
            {
                var index = FilteredGroupList.IndexOf(SelectedGroup);
                if (index != -1 && index < FilteredGroupList.Count - 1)
                {
                    SelectedGroup = (TokenGroup)FilteredGroupList[index + 1];
                }
            }

            if (keyEventArgs.Key == Key.Up)
            {
                var index = FilteredGroupList.IndexOf(SelectedGroup);
                if (index != -1 && index > 0)
                {
                    SelectedGroup = (TokenGroup) FilteredGroupList[index - 1];
                }
            }
        }
    }
}
