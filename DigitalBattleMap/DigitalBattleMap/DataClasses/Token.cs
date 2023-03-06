using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class Token
    {
        public string Name { get; set; } = "";
        public TokenSize Size { get; set; }
        public string ImagePath { get; set; } = "";

        public Token Copy()
        {
            return new Token
            {
                Name = Name,
                Size = Size,
                ImagePath = ImagePath
            };
        }

        public Token Copy(TokenSize newSize)
        {
            return new Token
            {
                Name = Name,
                Size = newSize,
                ImagePath = ImagePath
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class TokenListItem
    {
        private Bitmap _bitmap;

        public Token Token { get; set; }
        public Point<int> Position { get; set; } = new Point<int>();
        public int Id { get; set; }

        public Bitmap GetBitmap()
        {
            if(_bitmap == null)
            {
                using (var bitmap = new Bitmap(Token.ImagePath))
                {
                    _bitmap = new Bitmap(bitmap);
                }
            }

            return _bitmap;
        }
    }
}
