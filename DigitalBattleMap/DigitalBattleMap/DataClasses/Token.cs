using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DigitalBattleMap
{
    public class Token : PropertyHandler
    {
        public event EventHandler SizeChanged;

        public string Name { get; set; } = "";
        public TokenSize Size { get => Get<TokenSize>(); set => Set(value, NotifySizeChanged); }
        public string ImagePath { get; set; } = "";
        public bool PlayerControl { get => Get<bool>(); set => Set(value); }

        public Token Copy()
        {
            return new Token
            {
                Name = Name,
                Size = Size,
                ImagePath = ImagePath,
                PlayerControl = PlayerControl
            };
        }

        public double GetSizeFactor()
        {
            switch (Size)
            {
                case TokenSize.Tiny:
                    return 0.5;
                case TokenSize.Small:
                    return 0.75;
                case TokenSize.Medium:
                    return 1;
                case TokenSize.Large:
                    return 2;
                case TokenSize.Huge:
                    return 3;
                case TokenSize.Gargantuan:
                    return 4;
                default:
                    return 1;
            }
        }

        public override string ToString()
        {
            return Name;
        }

        private void NotifySizeChanged()
        {
            SizeChanged?.Invoke(this, new EventArgs());
        }
    }

    public class TokenListItem
    {
        private Bitmap _bitmap;

        public TokenListItem()
        {
            TokenSizeChangedCommand = new RelayCommand(p => TokenSizeChanged((string)p));
            PlayerControlCommand = new RelayCommand(p => PlayerControlToggled());
        }

        public Token Token { get; set; }
        public Point<int> Position { get; set; } = new Point<int>();
        public int Id { get; set; }

        [JsonIgnore]
        public ICommand TokenSizeChangedCommand { get; set; }
        [JsonIgnore]
        public ICommand PlayerControlCommand { get; set; }

        public Bitmap GetBitmap()
        {
            if(_bitmap == null)
            {
                _bitmap = BitmapTools.LoadBitmap(Token.ImagePath);
            }

            return _bitmap;
        }

        private void TokenSizeChanged(string size)
        {
            Token.Size = Enum.Parse<TokenSize>(size);
        }

        private void PlayerControlToggled()
        {
            Token.PlayerControl = !Token.PlayerControl;
        }
    }
}
