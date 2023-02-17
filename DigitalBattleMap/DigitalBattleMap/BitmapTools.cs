using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public static class BitmapTools
    {
        private const int _width = 1920;
        private const int _height = 1080;

        public static Bitmap CreateGrid(int gridSize)
        {
            var gridBitMap = new Bitmap(_width, _height);
            DrawGrid(gridBitMap, gridSize);
            return gridBitMap;
        }

        public static void DrawGrid(Bitmap bitmap, int gridSize)
        {
            var xModulo = _width % gridSize;
            var yModulo = _height % gridSize;

            var startX = xModulo == 0 ? gridSize : xModulo / 2;
            var startY = yModulo == 0 ? gridSize : yModulo / 2;

            using (var graphics = Graphics.FromImage(bitmap))
            {
                Pen blackPen = new Pen(Color.Black, 1);

                for (int x = startX; x < _width; x += gridSize)
                {
                    graphics.DrawLine(blackPen, x, 0, x, _height);
                }

                for (int y = startY; y < _height; y += gridSize)
                {
                    graphics.DrawLine(blackPen, 0, y, _width, y);
                }
            }
        }
    }
}
