using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media.TextFormatting;

namespace DigitalBattleMap
{
    public static class BitmapTools
    {
        private const int _width = 1920;
        private const int _height = 1080;
        private static ConditionIcons _conditionIcons = new ConditionIcons();

        public static Bitmap LoadBitmap(string path)
        {
            using (var tempBitmap = new Bitmap(path))
            {
                return new Bitmap(tempBitmap);
            }
        }

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

        public static Size<int> GetBitmapSize()
        {
            return new Size<int>(_width, _height);
        }

        public static Bitmap CreateGridAndStrokesBitmap(Bitmap grid, StrokeCollection strokes, Size<int> inkCanvasSize)
        {
            var bitmap = new Bitmap(grid);
            DrawStrokes(bitmap, strokes, inkCanvasSize);
            return bitmap;
        }

        public static Bitmap CreateColorButton(Color color, bool addSelectionIndicator)
        {
            var bitmap = new Bitmap(70, 70);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var brush = new SolidBrush(color);
                var borderPen = new Pen(Color.Gray, 4);

                graphics.FillEllipse(brush, 9, 9, 50, 50);

                if (addSelectionIndicator)
                {
                    graphics.DrawEllipse(borderPen, 4, 4, 60, 60);
                }

                return bitmap;
            }
        }

        public static Bitmap CreateEraserButton(bool addSelectionIndicator)
        {
            var bitmap = new Bitmap(70, 70);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                var yellowBrush = new SolidBrush(Color.Yellow);
                var pinkBrush = new SolidBrush(Color.Pink);

                graphics.FillRectangle(yellowBrush, 19, 14, 30, 40);
                graphics.FillRectangle(pinkBrush, 19, 14, 30, 12);

                if (addSelectionIndicator)
                {
                    var borderPen = new Pen(Color.Gray, 4);
                    graphics.DrawEllipse(borderPen, 4, 4, 60, 60);
                }

                return bitmap;
            }
        }

        public static Bitmap CreateArrowButton(ArrowDirection direction)
        {
            var bitmap = new Bitmap(70, 70);
            var points = new PointF[3];

            switch (direction)
            {
                case ArrowDirection.Up:
                    points = new PointF[] { new PointF(9, 59), new PointF(59, 59), new PointF(34, 9) };
                    break;
                case ArrowDirection.Down:
                    points = new PointF[] { new PointF(9, 9), new PointF(59, 9), new PointF(34, 59) };
                    break;
                case ArrowDirection.Left:
                    points = new PointF[] { new PointF(59, 9), new PointF(59, 59), new PointF(9, 34) };
                    break;
                case ArrowDirection.Right:
                    points = new PointF[] { new PointF(9, 9), new PointF(9, 59), new PointF(59, 34) };
                    break;
            }

            using (var graphics = Graphics.FromImage(bitmap))
            {
                var brush = new SolidBrush(Color.Black);
                graphics.FillPolygon(brush, points);
                return bitmap;
            }
        }

        public static Bitmap CreateZoomButton(bool isZoomInButton)
        {
            var bitmap = new Bitmap(70, 70);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                var brush = new SolidBrush(Color.Black);
                graphics.FillRectangle(brush, 9, 30, 50, 8);

                if (isZoomInButton)
                {
                    graphics.FillRectangle(brush, 30, 9, 8, 50);
                }

                return bitmap;
            }
        }

        public static Bitmap CropBitmap(Bitmap bitmap, Rectangle rectangle)
        {
            var croppedBitmap = new Bitmap(rectangle.Width, rectangle.Height);
            using (var graphics = Graphics.FromImage(croppedBitmap))
            {
                graphics.DrawImage(bitmap, new Rectangle(0, 0, croppedBitmap.Width, croppedBitmap.Height), rectangle, GraphicsUnit.Pixel);
            }
            return croppedBitmap;
        }

        public static Bitmap ResizeBitmap(Bitmap bitmap)
        {
            return ResizeBitmap(bitmap, new Size<int>(_width, _height));
        }

        public static Bitmap ResizeBitmap(Bitmap bitmap, Size<int> size)
        {
            var destinationRectangle = new Rectangle(0, 0, size.Width, size.Height);
            var resizedBitmap = new Bitmap(size.Width, size.Height);

            resizedBitmap.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (var graphics = Graphics.FromImage(resizedBitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(bitmap, destinationRectangle, 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return resizedBitmap;
        }

        public static void DrawToken(Bitmap bitmap, TokenListItem tokenListItem, string tokenId, int gridSize)
        {
            (var drawingPosition, var tokenSize) = CalculateTokenDrawingPositionAndSize(tokenListItem.Token.GetSizeFactor(), tokenListItem.Position, gridSize);

            // Resize and draw token
            if (IsTokenVisible(drawingPosition, gridSize))
            {
                var resizedTokenImage = ResizeBitmap(tokenListItem.GetBitmap(), tokenSize);
                DrawImageOnBitmap(bitmap, resizedTokenImage, drawingPosition);
                DrawTokenConditions(bitmap, tokenListItem.Conditions, drawingPosition, tokenSize);
                DrawTokenId(bitmap, tokenId, drawingPosition, tokenSize);
            }
        }

        public static void DrawTokenSelection(Bitmap bitmap, double tokenSizeFactor, Point<int> tokenPosition, int gridSize)
        {
            (var drawingPosition, var tokenSize) = CalculateTokenDrawingPositionAndSize(tokenSizeFactor, tokenPosition, gridSize);

            if (IsTokenVisible(drawingPosition, gridSize))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    var pen = new Pen(Color.Blue, 4);
                    graphics.DrawEllipse(pen, drawingPosition.X, drawingPosition.Y, tokenSize.Width, tokenSize.Height);
                }
            }
        }

        public static Bitmap CreateTokenBitmap(Bitmap image)
        {
            if (image.Width == image.Height)
            {
                return new Bitmap(image);
            }

            var size = Math.Max(image.Width, image.Height);
            var bitmap = new Bitmap(size, size);
            var position = new Point<int>();

            if (size == image.Width)
            {
                position.Y = (size - image.Height) / 2;
            }
            else
            {
                position.X = (size - image.Width) / 2;
            }

            DrawImageOnBitmap(bitmap, image, position);
            return bitmap;
        }

        public static Bitmap MergeBitmaps(List<Bitmap> bitmaps)
        {
            if (bitmaps.Count > 0)
            {
                var bitmap = new Bitmap(bitmaps.First());
                for (int i = 1; i < bitmaps.Count; i++)
                {
                    DrawImageOnBitmap(bitmap, bitmaps[i], new Point<int>());
                }
                return bitmap;
            }
            else
            {
                return new Bitmap(_width, _height);
            }
        }

        public static Point<int> CalculateGridOffset(int gridSize)
        {
            var middleGridCellX = (_width / 2) - (gridSize / 2);
            var middleGridCellY = (_height / 2) - (gridSize / 2);

            var xModulo = middleGridCellX % gridSize;
            var yModulo = middleGridCellY % gridSize;

            var startX = xModulo == 0 ? 0 : xModulo;
            var startY = yModulo == 0 ? 0 : yModulo;

            return new Point<int>(startX, startY);
        }

        private static bool IsTokenVisible(Point<int> drawingPosition, int gridSize)
        {
            var isVisible = true;

            if (drawingPosition.X + gridSize < 0)
            {
                isVisible = false;
            }
            else if (drawingPosition.X - gridSize > _width)
            {
                isVisible = false;
            }
            else if (drawingPosition.Y + gridSize < 0)
            {
                isVisible = false;
            }
            else if (drawingPosition.Y - gridSize > _height)
            {
                isVisible = false;
            }

            return isVisible;
        }

        private static void DrawTokenId(Bitmap bitmap, string tokenId, Point<int> drawingPosition, Size<int> tokenSize)
        {
            if (tokenId != null && tokenId != "")
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    var brush = new SolidBrush(Color.White);
                    var textSize = Math.Max(tokenSize.Width / 6, 1);

                    StringFormat stringFormat = new StringFormat();
                    stringFormat.LineAlignment = StringAlignment.Center;
                    stringFormat.Alignment = StringAlignment.Center;

                    var textPosition = new Point<int>();
                    textPosition.X = drawingPosition.X + tokenSize.Width / 2;
                    textPosition.Y = drawingPosition.Y + tokenSize.Height / 2;

                    graphics.DrawString(tokenId, new Font("", textSize, FontStyle.Bold), brush, textPosition.X, textPosition.Y, stringFormat);
                }
            }
        }

        private static void DrawTokenConditions(Bitmap bitmap, List<Condition> conditions, Point<int> tokenDrawingPosition, Size<int> tokenSize)
        {
            /* Position index:
             * 
             *     3
             *      
             * 1       2
             *      
             *     0
             */

            var xFactor = new double[] { 0.5, 0.0, 1.0, 0.5 };
            var yFactor = new double[] { 1.0, 0.5, 0.5, 0.0,};
            var conditionSize = new Size<double>(tokenSize.Width / 2.5, tokenSize.Height / 2.5);

            for (int i = 0; i < 4 && i < conditions.Count; i++)
            {
                var resizedConditionImage = ResizeBitmap(_conditionIcons.GetConditionIcon(conditions[i]), conditionSize.ToSizeInt());
                
                var drawingPosition = tokenDrawingPosition.ToPointDouble();
                drawingPosition.X += (tokenSize.Width * xFactor[i]) - (conditionSize.Width * xFactor[i]);
                drawingPosition.Y += (tokenSize.Height * yFactor[i]) - (conditionSize.Height * yFactor[i]);
                
                DrawImageOnBitmap(bitmap, resizedConditionImage, drawingPosition.ToPointInt());
            }
        }

        private static (Point<int>, Size<int>) CalculateTokenDrawingPositionAndSize(double tokenSizeFactor, Point<int> tokenPosition, int gridSize)
        {
            var gridStart = CalculateGridOffset(gridSize);
            var margin = 4;

            // Calculate grid cell
            var unsnappedGridCellX = (tokenPosition.X - gridStart.X) / (double)gridSize;
            var unsnappedGridCellY = (tokenPosition.Y - gridStart.Y) / (double)gridSize;
            int gridCellX = (int)Math.Floor(unsnappedGridCellX);
            int gridCellY = (int)Math.Floor(unsnappedGridCellY);

            // Calculate token offset
            // E.g. if size factor is 0.5 then token needs an offset to be centerd in the grid cell
            var preciseTokenSize = gridSize * tokenSizeFactor;
            var tokenGridSize = gridSize * Math.Ceiling(tokenSizeFactor);
            double tokenOffset = (tokenGridSize - preciseTokenSize) / 2;

            // Calculate drawing position using the calculated grid cell, token offset and margin
            var drawingPosition = new Point<int>(gridStart);
            drawingPosition.X += (int)Math.Round((gridCellX * gridSize) + tokenOffset);
            drawingPosition.Y += (int)Math.Round((gridCellY * gridSize) + tokenOffset);
            drawingPosition.X += margin;
            drawingPosition.Y += margin;

            // Calcualte the size of the token using the token size, size factor and margin
            var tokenSize = new Size<int>((int)preciseTokenSize, (int)preciseTokenSize);
            tokenSize.Width -= 2 * margin;
            tokenSize.Height -= 2 * margin;

            return (drawingPosition, tokenSize);
        }

        private static void DrawImageOnBitmap(Bitmap bitmap, Bitmap image, Point<int> position)
        {
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(image, position.X, position.Y);
            }
        }

        private static void DrawGrid(Bitmap bitmap, int gridSize)
        {
            var gridOffset = CalculateGridOffset(gridSize);

            using (var graphics = Graphics.FromImage(bitmap))
            {
                Pen blackPen = new Pen(Color.Black, 1);

                for (int x = gridOffset.X; x < _width; x += gridSize)
                {
                    graphics.DrawLine(blackPen, x, 0, x, _height);
                }

                for (int y = gridOffset.Y; y < _height; y += gridSize)
                {
                    graphics.DrawLine(blackPen, 0, y, _width, y);
                }
            }
        }

        private static void DrawStrokes(Bitmap bitmap, StrokeCollection strokes, Size<int> canvasSize)
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
                        point.X -= penSize / 2;
                        point.Y -= penSize / 2;

                        var resizedX = point.X.Map(0, canvasSize.Width, 0, _width);
                        var resizedY = point.Y.Map(0, canvasSize.Height, 0, _height);
                        points.Add(new PointF(resizedX, resizedY));
                    }

                    SmoothLine(points, penSize);

                    foreach (var point in points)
                    {
                        var resizedPenSize = penSize.Map(0, canvasSize.Width, 0, _width);
                        graphics.FillEllipse(brush, point.X, point.Y, resizedPenSize, resizedPenSize);
                    }
                }
            }
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
                    if (dist > (penSize / 3))
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
