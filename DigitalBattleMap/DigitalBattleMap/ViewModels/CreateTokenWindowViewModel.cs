using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class CreateTokenWindowViewModel : PropertyHandler
    {
        private IWindowService _windowService;
        private Bitmap _tokenBitmap = new Bitmap(256, 256);
        private bool _tokenImageSelected = false;
        private string _originalTokenImagePath = "";

        public CreateTokenWindowViewModel()
        {
            InitializeProperties();
        }

        public CreateTokenWindowViewModel(IWindowService windowService, List<string> tokenNames)
        {
            _windowService = windowService;
            ExistingTokenNames = tokenNames;
            InitializeProperties();
        }

        public CreateTokenWindowViewModel(IWindowService windowService, List<string> tokenNames, Token editToken)
        {
            _windowService = windowService;
            ExistingTokenNames = tokenNames;
            InitializeProperties();

            TokenName = editToken.Name;
            SelectedTokenSize = editToken.Size;
            _tokenBitmap = BitmapTools.LoadBitmap(editToken.ImagePath);
            _originalTokenImagePath = editToken.ImagePath;

            _tokenImageSelected = true;
            NotifyPropertyChange(nameof(TokenBitmapSource));
            NotifyPropertyChange(nameof(IsOkButtonEnabled));
        }

        public TokenSize SelectedTokenSize { get => Get<TokenSize>(); set => Set(value); }
        public string TokenName { get => Get<string>(); set => Set(value, TokenNameChanged); }
        public System.Windows.Media.Brush NameBorderBrush { get => Get<System.Windows.Media.Brush>(); set => Set(value); }
        public string ToolTip { get => Get<string>(); set => Set(value); }
        public bool IsOkButtonEnabled { get => AllInformationAvailable(); }
        public ICommand SelectImageCommand { get; set; }
        public ICommand OkCommand { get; set; }
        public BitmapSource TokenBitmapSource { get => _tokenBitmap.ToBitmapImage(); }
        public List<string> ExistingTokenNames { get; set; } = new List<string>();

        public Token Token { get; set; }

        public void SelectImage()
        {
            if (_windowService.ShowOpenFileDialog(out var path))
            {
                _tokenBitmap = BitmapTools.CreateTokenBitmap(BitmapTools.LoadBitmap(path));
                _tokenImageSelected = true;
                NotifyPropertyChange(nameof(TokenBitmapSource));
                NotifyPropertyChange(nameof(IsOkButtonEnabled));
            }
        }

        private void InitializeProperties()
        {
            SelectedTokenSize = TokenSize.Medium;
            NameBorderBrush = System.Windows.Media.Brushes.Transparent;
            SelectImageCommand = new RelayCommand(p => SelectImage());
            OkCommand = new RelayCommand(p => OkButton());
        }

        private bool AllInformationAvailable()
        {
            return TokenName != null && TokenName != "" && _tokenImageSelected && ExistingTokenNames.SingleOrDefault(t => t.ToLower() == TokenName.ToLower()) == null;
        }

        private void OkButton()
        {
            if (File.Exists(_originalTokenImagePath))
            {
                File.Delete(_originalTokenImagePath);
            }

            var imagePath = Path.Combine(Constants.CustomTokensPath, $"{TokenName}.png");
            _tokenBitmap.Save(imagePath);

            var token = new Token();
            token.Name = TokenName;
            token.ImagePath = imagePath;
            token.Size = SelectedTokenSize;

            Token = token;            
        }

        private void TokenNameChanged()
        {
            if (ExistingTokenNames.SingleOrDefault(t => t.ToLower() == TokenName.ToLower()) == null)
            {
                NameBorderBrush = System.Windows.Media.Brushes.Transparent;
                ToolTip = null;
            }
            else
            {
                NameBorderBrush = System.Windows.Media.Brushes.Red;
                ToolTip = "A token with this name already exists";
            }

            NotifyPropertyChange(nameof(IsOkButtonEnabled));
        }
    }
}
