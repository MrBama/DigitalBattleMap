using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class MapUpdate
    {
        public MapUpdate(Bitmap backgroundBitmap, Bitmap gridAndDrawingBitmap, Bitmap tokensBitmap)
        {
            BackgroundBitmap = new Bitmap(backgroundBitmap);
            GridAndDrawingBitmap = new Bitmap(gridAndDrawingBitmap);
            TokenBitmap = new Bitmap(tokensBitmap);
        }

        public Bitmap BackgroundBitmap { get; set; }
        public Bitmap GridAndDrawingBitmap { get; set; }
        public Bitmap TokenBitmap { get; set; }
    }
}
