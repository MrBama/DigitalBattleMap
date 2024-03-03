using DigitalBattleMap.DataClasses;
using DigitalBattleMap.Interfaces;
using DigitalBattleMap.Utilities;
using DigitalBattleMap.Views;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap.ViewModels;

public class CreateTokenWindowViewModel : ViewModelBase
{
    private IWindowService _windowService;
    private Bitmap _tokenBitmap = new(256, 256);
    private bool _tokenImageSelected = false;
    private string _originalTokenImagePath = "";
    private string _originalMarkdownStatblockPath;
    private Statblock _statblock;
    private List<Token> _tokens;

    public CreateTokenWindowViewModel()
    {
        InitializeProperties();
    }

    public CreateTokenWindowViewModel(IWindowService windowService, List<Token> tokens)
    {
        _windowService = windowService;
        _tokens = tokens;
        ExistingTokenNames = tokens.Select(t => t.Name).ToList();
        InitializeProperties();
    }

    public CreateTokenWindowViewModel(IWindowService windowService, List<Token> tokens, Token editToken)
    {
        InitializeProperties();

        _windowService = windowService;
        _tokenBitmap = IO.File.LoadBitmap(editToken.ImagePath);
        _originalTokenImagePath = editToken.ImagePath;
        _tokenImageSelected = true;
        _statblock = editToken.Statblock?.Clone<Statblock>();
        _tokens = tokens;

        ExistingTokenNames = tokens.Select(t => t.Name).ToList();
        TokenName = editToken.Name;
        SelectedTokenSize = editToken.Size;
        SelectedTokenOrientation = editToken.Orientation;
        Hp = editToken.Hp;

        if (_statblock != null)
        {
            if(_statblock is MarkdownStatblock markdownStatblock)
            {
                _originalMarkdownStatblockPath = markdownStatblock.MarkdownPath;
                markdownStatblock.GetMarkdown(); // Make sure the existing markdown is cached in memory
            }
            IsStatblockCreated = true;
        }

        if (Hp != null || IsStatblockCreated)
        {
            ToggleOptional();
        }

        NotifyPropertyChange(nameof(TokenBitmapSource));
        NotifyPropertyChange(nameof(IsOkButtonEnabled));
    }

    protected override void InitializeCommands()
    {
        SelectImageCommand = new RelayCommand(p => SelectImage());
        OkCommand = new RelayCommand(p => OkButton());
        ToggleOptionalCommand = new RelayCommand(p => ToggleOptional());
        CreateStatblockCommand = new RelayCommand(p => CreateStatblock());
        RemoveStatblockCommand = new RelayCommand(p => RemoveStatblock());
        EditStatblockCommand = new RelayCommand(p => EditStatblock());
        CopyStatblockCommand = new RelayCommand(p => CopyStatblock());
    }

    public TokenSize SelectedTokenSize { get => Get<TokenSize>(); set => Set(value); }
    public TokenOrientation SelectedTokenOrientation { get => Get<TokenOrientation>(); set => Set(value); }
    public string TokenName { get => Get<string>(); set => Set(value, TokenNameChanged); }
    public System.Windows.Media.Brush NameBorderBrush { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
    public string NameToolTip { get => Get<string>(); set => Set(value); }
    public string StatblockCopyName { get => Get<string>(); set => Set(value); }
    public string OptionalButtonText { get => Get<string>(); set => Set(value); }
    public bool IsOkButtonEnabled { get => AllInformationAvailable(); }
    public bool IsOptionalShown { get => Get<bool>(); set => Set(value); }
    public bool IsStatblockCreated { get => Get<bool>(); set => Set(value, UpdateIsStatBlockEditable); }
    public bool IsStatblockEditable { get => Get<bool>(); set => Set(value); }
    public bool ShowStatblockCopyName { get => Get<bool>(); set => Set(value); }
    public int? Hp { get => Get<int?>(); set => Set(value); }
    public BitmapSource TokenBitmapSource { get => _tokenBitmap.ToBitmapImage(); }
    public List<string> ExistingTokenNames { get; set; } = new List<string>();
    public Token Token { get; set; }

    public ICommand SelectImageCommand { get; set; }
    public ICommand OkCommand { get; set; }
    public ICommand ToggleOptionalCommand { get; set; }
    public ICommand CreateStatblockCommand { get; set; }
    public ICommand RemoveStatblockCommand { get; set; }
    public ICommand EditStatblockCommand { get; set; }
    public ICommand CopyStatblockCommand { get; set; }

    private void SelectImage()
    {
        if (_windowService.ShowOpenFileDialog(out var path))
        {
            _tokenBitmap = BitmapTools.CreateTokenBitmap(IO.File.LoadBitmap(path));
            _tokenImageSelected = true;
            NotifyPropertyChange(nameof(TokenBitmapSource));
            NotifyPropertyChange(nameof(IsOkButtonEnabled));
        }
    }

    private void InitializeProperties()
    {
        SelectedTokenSize = TokenSize.Medium;
        SelectedTokenOrientation = TokenOrientation.East;
        NameBorderBrush = System.Windows.Media.Brushes.Transparent;
        OptionalButtonText = "Show optional";
    }

    private bool AllInformationAvailable()
    {
        return TokenName != null && TokenName != "" && _tokenImageSelected && ExistingTokenNames.SingleOrDefault(t => string.Equals(t, TokenName, StringComparison.CurrentCultureIgnoreCase)) == null;
    }

    private void OkButton()
    {
        if (IO.File.Exists(_originalTokenImagePath))
        {
            IO.File.Delete(_originalTokenImagePath);
        }

        if (IO.File.Exists(_originalMarkdownStatblockPath))
        {
            IO.File.Delete(_originalMarkdownStatblockPath);
        }

        var imagePath = Path.Combine(Constants.CustomTokensPath, $"{TokenName}.png");
        _tokenBitmap.Save(imagePath);
        _statblock?.Persist(TokenName);

        var token = new Token
        {
            Name = TokenName,
            ImagePath = imagePath,
            Size = SelectedTokenSize,
            Orientation = SelectedTokenOrientation,
            Hp = Hp,
            Statblock = _statblock
        };

        Token = token;
    }

    private void TokenNameChanged()
    {
        if (ExistingTokenNames.SingleOrDefault(t => string.Equals(t, TokenName, StringComparison.CurrentCultureIgnoreCase)) == null)
        {
            NameBorderBrush = System.Windows.Media.Brushes.Transparent;
            NameToolTip = null;
        }
        else
        {
            NameBorderBrush = System.Windows.Media.Brushes.Red;
            NameToolTip = "A token with this name already exists";
        }

        NotifyPropertyChange(nameof(IsOkButtonEnabled));
    }

    private void ToggleOptional()
    {
        if (IsOptionalShown)
        {
            OptionalButtonText = "Show optional";
        }
        else
        {
            OptionalButtonText = "Hide optional";
        }

        IsOptionalShown = !IsOptionalShown;
    }

    private void CreateStatblock()
    {
        var createStatblockWindowViewModel = new CreateStatblockWindowViewModel();
        _windowService.ShowWindowDialog<CreateStatblockWindow>(createStatblockWindowViewModel);
        if (createStatblockWindowViewModel.Success)
        {
            _statblock = new MarkdownStatblock(createStatblockWindowViewModel.MarkdownText);
            IsStatblockCreated = true;
        }
        else
        {
            IsStatblockCreated = false;
        }
    }

    private void RemoveStatblock()
    {
        _statblock = null;
        IsStatblockCreated = false;
    }

    private void EditStatblock()
    {
        var markdownStatblock = _statblock as MarkdownStatblock;

        var createStatblockWindowViewModel = new CreateStatblockWindowViewModel(markdownStatblock!.GetMarkdown());
        _windowService.ShowWindowDialog<CreateStatblockWindow>(createStatblockWindowViewModel);
        if (createStatblockWindowViewModel.Success)
        {
            _statblock = new MarkdownStatblock(createStatblockWindowViewModel.MarkdownText);
        }
    }

    private void CopyStatblock()
    {
        var selectTokenWindowViewModel = new SelectTokenWindowViewModel(_tokens)
        {
            SearchTokenNameOnly = true
        };
        _windowService.ShowWindowDialog<SelectTokenWindow>(selectTokenWindowViewModel);

        if (selectTokenWindowViewModel.AddedTokens.Count == 1)
        {
            var token = selectTokenWindowViewModel.AddedTokens.First();
            _statblock = token.Statblock?.Clone<Statblock>();
            IsStatblockCreated = true;
        }
    }

    private void UpdateIsStatBlockEditable()
    {
        IsStatblockEditable = _statblock != null;
        StatblockCopyName = null;
        ShowStatblockCopyName = false;

        if (_statblock is SourceStatblock sourceStatblock)
        {
            IsStatblockEditable = false;
            ShowStatblockCopyName = true;
            StatblockCopyName = sourceStatblock.SourceName;
        }        
    }
}
