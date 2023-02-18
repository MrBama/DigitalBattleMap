using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Windows.Controls;
using System.Windows.Ink;

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

        public static Bitmap CreateEmptyBitmap()
        {
            return new Bitmap(_width, _height);
        }

        public static Bitmap CreateMap(Bitmap grid, StrokeCollection strokes, int inkCanvasWidth, int inkCanvasHeight)
        {
            var bitmap = new Bitmap(grid);
            DrawStrokes(bitmap, strokes, inkCanvasWidth, inkCanvasHeight);
            return bitmap;
        }

        private static void DrawGrid(Bitmap bitmap, int gridSize)
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

        private static void DrawStrokes(Bitmap bitmap, StrokeCollection strokes, int canvasWidth, int canvasHeight)
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                for (int strokeIndex = 0; strokeIndex < strokes.Count; strokeIndex++)
                {
                    var points = new List<PointF>();
                    var drawingAttributs = strokes[strokeIndex].DrawingAttributes;
                    var mediaColor = drawingAttributs.Color;
                    var brush = new SolidBrush(Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B));
                    var penSize = (int)drawingAttributs.Width; // We always use the same width and height

                    for (int pointIndex = 0; pointIndex < strokes[strokeIndex].StylusPoints.Count; pointIndex++)
                    {
                        var point = new PointF();
                        point.X = (float)strokes[strokeIndex].StylusPoints[pointIndex].X;
                        point.Y = (float)strokes[strokeIndex].StylusPoints[pointIndex].Y;

                        var resizedX = Map(point.X, 0, canvasWidth, 0, _width) - penSize;
                        var resizedY = Map(point.Y, 0, canvasHeight, 0, _height) - penSize;
                        points.Add(new PointF(resizedX, resizedY));
                    }

                    SmoothLine(points, penSize);

                    foreach (var point in points)
                    {
                        var resizedPenSize = Map(penSize, 0, canvasWidth, 0, _width);
                        graphics.FillEllipse(brush, point.X, point.Y, resizedPenSize, resizedPenSize);
                    }
                }
            }
        }

        private static float Map(float input, float inMin, float inMax, float outMin, float outMax)
        {
            return (input - inMin) * (outMax - outMin) / (inMax - inMin) + outMin;
        }

        private static void SmoothLine(List<PointF> points, int penSize)
        {
            bool smoothRequired = true;

            while (smoothRequired)
            {
                smoothRequired = false;
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var coord1 = points[i];
                    var coord2 = points[i + 1];

                    var dist = Math.Sqrt(Math.Pow(coord1.X - coord2.X, 2) + Math.Pow(coord1.Y - coord2.Y, 2));

                    double penSize1 = (double)penSize;
                    penSize1 /= 1;

                    if (dist > penSize1)
                    {
                        var newPoint = new PointF((coord1.X + coord2.X) / 2, (coord1.Y + coord2.Y) / 2);
                        points.Insert(i + 1, newPoint);
                        smoothRequired = true;
                    }
                }
            }
        }
    }
}
