using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

namespace DigitalBattleMap;

public static class BitmapTools
{
    private static readonly ConditionIcons _conditionIcons = new();

    public static Bitmap CreateGrid(int gridSize)
    {
        var gridBitMap = CreateEmptyBitmap();
        DrawGrid(gridBitMap, gridSize);
        return gridBitMap;
    }

    public static Bitmap CreateEmptyBitmap()
    {
        return new(Constants.BitmapSize.Width, Constants.BitmapSize.Height);
    }

    public static Bitmap CreateColorButton(Brush brush, bool addSelectionIndicator)
    {
        var bitmap = new Bitmap(70, 70);
        using var graphics = Graphics.FromImage(bitmap);
        var borderPen = new Pen(Color.Gray, 4);

        graphics.FillEllipse(brush, 9, 9, 50, 50);

        if (addSelectionIndicator)
        {
            graphics.DrawEllipse(borderPen, 4, 4, 60, 60);
        }

        return bitmap;
    }

    public static Bitmap CreateEraserButton(bool addSelectionIndicator)
    {
        var bitmap = new Bitmap(70, 70);
        using var graphics = Graphics.FromImage(bitmap);
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

        using var graphics = Graphics.FromImage(bitmap);
        var brush = new SolidBrush(Color.Black);
        graphics.FillPolygon(brush, points);
        return bitmap;
    }

    public static Bitmap CreateZoomButton(bool isZoomInButton)
    {
        var bitmap = new Bitmap(70, 70);

        using var graphics = Graphics.FromImage(bitmap);
        var brush = new SolidBrush(Color.Black);
        graphics.FillRectangle(brush, 9, 30, 50, 8);

        if (isZoomInButton)
        {
            graphics.FillRectangle(brush, 30, 9, 8, 50);
        }

        return bitmap;
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
        return ResizeBitmap(bitmap, Constants.BitmapSize);
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

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(bitmap, destinationRectangle, 0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, wrapMode);
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
            resizedTokenImage.RotateFlip(tokenListItem.Token.GetOrientation());
            DrawImageOnBitmap(bitmap, resizedTokenImage, drawingPosition);
            DrawTokenConditions(bitmap, tokenListItem, drawingPosition, tokenSize);
            DrawTokenId(bitmap, tokenId, drawingPosition, tokenSize);
        }
    }

    public static void DrawTokenSelection(Bitmap bitmap, double tokenSizeFactor, Point<int> tokenPosition, int gridSize)
    {
        (var drawingPosition, var tokenSize) = CalculateTokenDrawingPositionAndSize(tokenSizeFactor, tokenPosition, gridSize);

        if (IsTokenVisible(drawingPosition, gridSize))
        {
            using var graphics = Graphics.FromImage(bitmap);
            var pen = new Pen(Color.Blue, 4);
            graphics.DrawEllipse(pen, drawingPosition.X, drawingPosition.Y, tokenSize.Width, tokenSize.Height);
        }
    }

    public static Bitmap CreateTokenBitmap(Bitmap image)
    {
        if (image.Width == image.Height)
        {
            return new(image);
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
            return CreateEmptyBitmap();
        }
    }

    public static Bitmap CreateFogOfWarBitmap(Rectangle area, List<FogOfWarArea> removedAreas)
    {
        var bitmap = new Bitmap(area.Width, area.Height);

        using var graphics = Graphics.FromImage(bitmap);
        var imageSize = new Rectangle(0, 0, area.Width, area.Height);
        graphics.FillRectangle(Brushes.Black, imageSize);

        if (removedAreas.Count > 0)
        {
            // Image is 0 based while area is not
            foreach (var removedArea in removedAreas)
            {
                var points = new List<PointF>();
                foreach (var point in removedArea.Points)
                {
                    points.Add(new PointF((float)point.X - area.X, (float)point.Y - area.Y));
                }
                graphics.FillPolygon(Brushes.White, points.ToArray());
            }

            bitmap.MakeTransparent(Color.White);
        }

        return bitmap;
    }

    public static void DrawShapes(Bitmap bitmap, List<DrawingShape> shapes, Size<double> canvasSize)
    {
        using var graphics = Graphics.FromImage(bitmap);        
        foreach (var shape in shapes)
        {
            var points = new List<Point<float>>();
            var brush = new SolidBrush(Color.FromArgb(shape.Color.A, shape.Color.R, shape.Color.G, shape.Color.B));
            var penSizeF = (float)shape.PenSize;

            foreach (var point in shape.Points)
            {
                var halfSize = shape.PenSizeCanvas / 2;
                var middleOfPoint = new Point<double>(point.X - halfSize, point.Y - halfSize);

                var resizedX = (float)middleOfPoint.X.Map(0, canvasSize.Width, 0, Constants.BitmapSize.Width);
                var resizedY = (float)middleOfPoint.Y.Map(0, canvasSize.Height, 0, Constants.BitmapSize.Height);
                points.Add(new Point<float>(resizedX, resizedY));
            }

            SmoothLine(points, penSizeF);

            foreach (var point in points)
            {
                graphics.FillEllipse(brush, point.X, point.Y, penSizeF, penSizeF);
            }
        }
    }

    private static bool IsTokenVisible(Point<int> drawingPosition, int gridSize)
    {
        var isVisible = true;

        if (drawingPosition.X + gridSize < 0)
        {
            isVisible = false;
        }
        else if (drawingPosition.X - gridSize > Constants.BitmapSize.Width)
        {
            isVisible = false;
        }
        else if (drawingPosition.Y + gridSize < 0)
        {
            isVisible = false;
        }
        else if (drawingPosition.Y - gridSize > Constants.BitmapSize.Height)
        {
            isVisible = false;
        }

        return isVisible;
    }

    private static void DrawTokenId(Bitmap bitmap, string tokenId, Point<int> drawingPosition, Size<int> tokenSize)
    {
        if (tokenId != null && tokenId != "")
        {
            using var graphics = Graphics.FromImage(bitmap);
            var textSize = Math.Max(tokenSize.Width / 6, 1);

            StringFormat stringFormat = new()
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            var drawingPositionDouble = Point<float>.Create(drawingPosition);
            var tokenSizeDouble = Size<float>.Create(tokenSize);

            var textPosition = new Point<float>
            {
                X = drawingPositionDouble.X + tokenSizeDouble.Width / 2,
                Y = drawingPositionDouble.Y + tokenSizeDouble.Height / 2
            };

            // Text and background require a aligned offset
            const float alignedTextSize = 100; //Set GridSize to 610
            const float alignedEllipseOffset = 15;
            const float alignedTextOffset = 6;

            // Draw ellipse background
            var ellipseOffset = (textSize * alignedEllipseOffset) / alignedTextSize;
            var x = textPosition.X - (textSize / 2) - ellipseOffset;
            var y = textPosition.Y - textSize / 2 - ellipseOffset;
            graphics.FillEllipse(Brushes.White, x, y, textSize + (ellipseOffset * 2), textSize + (ellipseOffset * 2));

            // Draw text
            var textOffset = (textSize * alignedTextOffset) / alignedTextSize;
            graphics.DrawString(tokenId, new Font("", textSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Black, textPosition.X, textPosition.Y + textOffset, stringFormat);
        }
    }

    private static void DrawTokenConditions(Bitmap bitmap, TokenListItem tokenListItem, Point<int> tokenDrawingPosition, Size<int> tokenSize)
    {
        /* Position index:
         * 
         *     3
         *      
         * 1       2
         *      
         *     0
         */

        var conditionPositions = new List<Point<double>>()
        {
            new Point<double>(0.5, 1.0),
            new Point<double>(0.0, 0.5), 
            new Point<double>(1.0, 0.5), 
            new Point<double>(0.5, 0.0) 
        };

        int orientationAngle = tokenListItem.Token.GetOrientationAngle();
        var orientationOrigin = new Point<double>(0.5, 0.5);
        conditionPositions = conditionPositions.Select(c => c.Rotate(orientationOrigin, orientationAngle)).ToList();

        var conditionSize = new Size<double>(tokenSize.Width / 2.5, tokenSize.Height / 2.5);

        // Only draw conditions when size is atleast 1
        if(conditionSize.Width >= 1 && conditionSize.Height >= 1)
        {
            for (int i = 0; i < 4 && i < tokenListItem.Conditions.Count; i++)
            {
                var resizedConditionImage = ResizeBitmap(_conditionIcons.GetConditionIcon(tokenListItem.Conditions[i]), Size<int>.Create(conditionSize));
                resizedConditionImage.RotateFlip(tokenListItem.Token.GetOrientation());
                var drawingPosition = Point<double>.Create(tokenDrawingPosition);

                drawingPosition.X += (tokenSize.Width * conditionPositions[i].X) - (conditionSize.Width * conditionPositions[i].X);
                drawingPosition.Y += (tokenSize.Height * conditionPositions[i].Y) - (conditionSize.Height * conditionPositions[i].Y);

                DrawImageOnBitmap(bitmap, resizedConditionImage, Point<int>.Create(drawingPosition));
            }
        }
    }

    private static (Point<int>, Size<int>) CalculateTokenDrawingPositionAndSize(double tokenSizeFactor, Point<int> tokenPosition, int gridSize)
    {
        var gridStart = Mathematics.CalculateGridOffset(gridSize);
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

        // Make sure that tokenSize is not not negative because of subtracting the margin
        tokenSize.Width = Math.Max(tokenSize.Width, 1);
        tokenSize.Height = Math.Max(tokenSize.Height, 1);

        return (drawingPosition, tokenSize);
    }

    private static void DrawImageOnBitmap(Bitmap bitmap, Bitmap image, Point<int> position)
    {
        using var graphics = Graphics.FromImage(bitmap);
        graphics.DrawImage(image, position.X, position.Y);
    }

    private static void DrawGrid(Bitmap bitmap, int gridSize)
    {
        var gridOffset = Mathematics.CalculateGridOffset(gridSize);

        using var graphics = Graphics.FromImage(bitmap);
        Pen blackPen = new(Color.Black, 1);

        for (int x = gridOffset.X; x < Constants.BitmapSize.Width; x += gridSize)
        {
            graphics.DrawLine(blackPen, x, 0, x, Constants.BitmapSize.Height);
        }

        for (int y = gridOffset.Y; y < Constants.BitmapSize.Height; y += gridSize)
        {
            graphics.DrawLine(blackPen, 0, y, Constants.BitmapSize.Width, y);
        }
    }

    private static void SmoothLine(List<Point<float>> points, float penSize)
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
                if (dist > (penSize / 5))
                {
                    var newPoint = new Point<float>((coord1.X + coord2.X) / 2, (coord1.Y + coord2.Y) / 2);
                    points.Insert(i + 1, newPoint);
                    smoothRequired = true;
                }
            }
        }
    }
}
