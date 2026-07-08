using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Imaging;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;

using Color = System.Drawing.Color;

using IImage = DigitalBattleMap.Imaging.IImage;
using ImageFactory = DigitalBattleMap.Imaging.ImageFactory;
using ImagingColor = DigitalBattleMap.Imaging.Color;


namespace DigitalBattleMap;

public static class BitmapTools
{
    private static readonly ConditionIcons _conditionIcons = new();

    public static Bitmap CreateGrid(int gridSize)
    {
        var gridBitmap = CreateEmptyBitmap();
        var gridOrigin = new Point<int>(Constants.MapSize.Width / 2, Constants.MapSize.Height / 2);
        DrawGrid(gridBitmap, gridSize, gridOrigin, 1);
        return gridBitmap;
    }

    public static Bitmap CreateEmptyBitmap()
    {
        //return new(Constants.MapSize.Width, Constants.MapSize.Height);
        return ImageFactory.Create(Constants.MapSize.Width, Constants.MapSize.Height).ToDrawingBitmap();
    }

    public static Bitmap CreateBlackBitmap()
    {
        //var bitmap = new Bitmap(
        //    Constants.MapSize.Width,
        //    Constants.MapSize.Height,
        //    PixelFormat.Format32bppArgb);
        //using var gfx = Graphics.FromImage(bitmap);
        //gfx.Clear(Color.Black);
        //return bitmap;
        return ImageFactory.Create(Constants.MapSize.Width, Constants.MapSize.Height, ImagingColor.Black).ToDrawingBitmap();
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

    public static Bitmap CreateColorButton(ImagingColor color, bool addSelectionIndicator)
    {
        var image = ImageFactory.Create(70, 70);
        image.FillEllipse(color, 9, 9, 50, 50);

        if (addSelectionIndicator)
        {
            image.DrawEllipse(Color.Gray, 4, 4, 4, 60, 60);
        }

        return image.ToDrawingBitmap();
    }

    public static Bitmap CreateEraserButton(bool addSelectionIndicator)
    {
        //var bitmap = new Bitmap(70, 70);
        //using var graphics = Graphics.FromImage(bitmap);
        //var yellowBrush = new SolidBrush(Color.Yellow);
        //var pinkBrush = new SolidBrush(Color.Pink);

        //graphics.FillRectangle(yellowBrush, 19, 14, 30, 40);
        //graphics.FillRectangle(pinkBrush, 19, 14, 30, 12);

        //if (addSelectionIndicator)
        //{
        //    var borderPen = new Pen(Color.Gray, 4);
        //    graphics.DrawEllipse(borderPen, 4, 4, 60, 60);
        //}

        //return bitmap;

        var image = ImageFactory.Create(70, 70);
        image.FillRectangle(Color.Yellow, 19, 14, 30, 40);
        image.FillRectangle(Color.Pink, 19, 14, 30, 12);

        if(addSelectionIndicator)
        {
            image.DrawEllipse(Color.Gray, 4, 4, 4, 60, 60);
        }

        return image.ToDrawingBitmap();
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

    public static Bitmap CreateCropButton()
    {
        var bitmap = new Bitmap(70, 70);

        using var graphics = Graphics.FromImage(bitmap);
        var brush = new SolidBrush(Color.Black);
        graphics.FillRectangle(brush, 25, 35, 40, 30);

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
        return ResizeBitmap(bitmap, Constants.MapSize);
    }

    public static Bitmap ResizeToBitmap(Bitmap bitmap, Bitmap bitmapTo)
    {
        return ResizeBitmap(bitmap, new(bitmapTo.Width, bitmapTo.Height));
    }

    public static Bitmap ResizeBitmap(Bitmap bitmap, Size<int> size)
    {
        return OpenCV.Resize(bitmap, size);
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
            AddTokenHeightCondition(tokenListItem);
            DrawTokenConditions(bitmap, tokenListItem, drawingPosition, tokenSize);
            DrawTokenId(bitmap, tokenId, tokenListItem, drawingPosition, tokenSize);
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

    public static Bitmap MergeBitmaps(params Bitmap[] bitmaps)
    {
        if (bitmaps.Count() > 0)
        {
            var bitmap = new Bitmap(bitmaps.First());
            for (int i = 1; i < bitmaps.Count(); i++)
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

    public static Bitmap ConcateDigitBitmaps(List<Bitmap> bitmaps)
    {
        int width = 0;
        int height = 0;
        foreach (var map in bitmaps)
        {
            width = Math.Max(width, map.Width);
            height = Math.Max(height, map.Height);
        }

        Bitmap bitmap = new Bitmap(width, height);

        // Add 25% clearance for the image
        var widthPadding = bitmap.Width / 4;
        var heightPadding = bitmap.Height / 4;

        var imageWidth = bitmap.Width - (2 * widthPadding);
        var imageHeight = bitmap.Height - (2 * heightPadding);

        using (Graphics g = Graphics.FromImage(bitmap))
        {
            // Add white background
            g.FillEllipse(Brushes.White, 0, 0, bitmap.Width, bitmap.Height);

            for (int i = 0; i < bitmaps.Count; i++)
            {
                var digitPadding = DigitPadding(bitmaps.Count, i, bitmap.Width);
                g.DrawImage(bitmaps[i], widthPadding + digitPadding, heightPadding, imageWidth, imageHeight);
            }
        }
        return bitmap;
    }

    public static void RotateBitmap(Bitmap bitmap, BitmapRotation rotation)
    {
        switch (rotation)
        {
            case BitmapRotation.Rotate0:
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipNone);
                break;
            case BitmapRotation.Rotate90:
                bitmap.RotateFlip(RotateFlipType.Rotate90FlipNone);
                break;
            case BitmapRotation.Rotate180:
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipNone);
                break;
            case BitmapRotation.Rotate270:
                bitmap.RotateFlip(RotateFlipType.Rotate270FlipNone);
                break;
            default:
                break;
        }
    }

    private static int DigitPadding(int maps, int index, int width)
    {
        switch (maps)
        {
            case 1:
                return 0;
            case 2:
                var widthPadding2 = width / 5;
                return index == 0 ? -widthPadding2 : widthPadding2;
            case 3:
                if (index == 1)
                {
                    return 0;
                }
                var widthPadding3 = width / 3;
                return index == 0 ? -widthPadding3 : widthPadding3;
            default:
                return 0;
        }
    }

    public static Bitmap CreateFogOfWarBitmap(Rectangle area, List<SelectedArea> removedAreas)
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

    /**
     * First draws all transparent polygon aftewords the black polygons. 
     * This gives fog priority on overlapping shaps and aligns with the visuals in the UI.
     */
    public static void DrawFogShapes(Bitmap bitmap, List<FogShape> shapes, Size<double> canvasSize)
    {
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias; // fix for 1 pixel shift in map drawing vs dm overview

        foreach (var shape in shapes.OrderBy(s => s.IsFogEnabled))
        {
            FogPolygon(canvasSize, graphics, shape);
        }
    }

    private static void FogPolygon(Size<double> canvasSize, Graphics graphics, FogShape shape)
    {
        if (!shape.Points.Any())
        {
            return;
        }

        var pointsF = new List<PointF>();
        foreach (var point in shape.Points)
        {
            var halfSize = shape.PenSizeCanvas / 2;
            var middleOfPoint = new Point<double>(point.X - halfSize, point.Y - halfSize);

            var resizedX = (float)middleOfPoint.X.Map(0, canvasSize.Width, 0, Constants.MapSize.Width);
            var resizedY = (float)middleOfPoint.Y.Map(0, canvasSize.Height, 0, Constants.MapSize.Height);
            pointsF.Add(new PointF(resizedX, resizedY));
        }

        if (shape.IsFogEnabled)
        {
            graphics.FillPolygon(Brushes.Black, pointsF.ToArray());
        }
        else
        {
            // Use CompositingMode.SourceCopy to punch through to true transparency,
            // bypassing alpha blending — this kills the anti-alias fringe.
            var previousMode = graphics.CompositingMode;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            using var transparentBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
            graphics.FillPolygon(transparentBrush, pointsF.ToArray());
            graphics.CompositingMode = previousMode;
        }
    }

    public static void DrawShapes(Bitmap bitmap, List<DrawingShape> shapes, Size<double> canvasSize)
    {
        using var graphics = Graphics.FromImage(bitmap);
        foreach (var shape in shapes)
        {
            var pointsF = new List<Point<float>>();
            var brush = shape.Color.ToDrawingBrush();
            var penSizeF = (float)shape.PenSize;

            foreach (var point in shape.Points)
            {
                var halfSize = shape.PenSizeCanvas / 2;
                var middleOfPoint = new Point<double>(point.X - halfSize, point.Y - halfSize);

                var resizedX = (float)middleOfPoint.X.Map(0, canvasSize.Width, 0, Constants.MapSize.Width);
                var resizedY = (float)middleOfPoint.Y.Map(0, canvasSize.Height, 0, Constants.MapSize.Height);
                pointsF.Add(new Point<float>(resizedX, resizedY));
            }

            DrawShape(graphics, pointsF, brush, penSizeF);
        }
    }

    private static void DrawShape(Graphics graphics, List<Point<float>> points, System.Drawing.Brush brush, float penSizeF)
    {
        SmoothLine(points, penSizeF);
        foreach (var point in points)
        {
            graphics.FillEllipse(brush, point.X, point.Y, penSizeF, penSizeF);
        }
    }

    public static void DrawGrid(Bitmap bitmap, int gridSize, Point<int> gridOrigin, int penSize)
    {
        var gridOffset = Mathematics.CalculateGridOffset(gridSize, gridOrigin);

        using var graphics = Graphics.FromImage(bitmap);
        Pen blackPen = new(Color.Black, penSize);

        for (int x = gridOffset.X; x < bitmap.Width; x += gridSize)
        {
            graphics.DrawLine(blackPen, x, 0, x, bitmap.Height);
        }

        for (int y = gridOffset.Y; y < bitmap.Height; y += gridSize)
        {
            graphics.DrawLine(blackPen, 0, y, bitmap.Width, y);
        }
    }

    public static Bitmap CreateTokenOverviewBitmap(Dictionary<TokenListItem, Point<int>> tokenListWithNormilizedPositions, int gridSize)
    {
        // Helper lambda's to calculate token sizes
        var calculateTokenSize = (TokenListItem tokenListItem) => (int)Math.Round(gridSize * tokenListItem.Token.GetSizeFactor());
        var calculateHalfTokenSize = (TokenListItem tokenListItem) => (int)Math.Round(gridSize * tokenListItem.Token.GetSizeFactor() / 2);

        // Calculate the bounding box around the tokens
        var minTokenPositionX = tokenListWithNormilizedPositions.Min(kv => kv.Value.X - calculateHalfTokenSize(kv.Key));
        var maxTokenPositionX = tokenListWithNormilizedPositions.Max(kv => kv.Value.X + calculateHalfTokenSize(kv.Key));
        var minTokenPositionY = tokenListWithNormilizedPositions.Min(kv => kv.Value.Y - calculateHalfTokenSize(kv.Key));
        var maxTokenPositionY = tokenListWithNormilizedPositions.Max(kv => kv.Value.Y + calculateHalfTokenSize(kv.Key));

        var bitmap = new Bitmap(maxTokenPositionX - minTokenPositionX, maxTokenPositionY - minTokenPositionY);
        using Graphics graph = Graphics.FromImage(bitmap);

        foreach ((var tokenListItem, var position) in tokenListWithNormilizedPositions)
        {
            if (tokenListItem.Visible)
            {
                // Tokens positions can be negative but bitmap positions always start at 0
                var zeroBasedPosition = new Point<int>(position.X - minTokenPositionX, position.Y - minTokenPositionY);
                var topLeftCorner = new Point<int>(zeroBasedPosition.X - calculateHalfTokenSize(tokenListItem), zeroBasedPosition.Y - calculateHalfTokenSize(tokenListItem));
                var tokenImage = ResizeBitmap(tokenListItem.GetBitmap(), new Size<int>(calculateTokenSize(tokenListItem), calculateTokenSize(tokenListItem)));
                graph.DrawImage(tokenImage, topLeftCorner.X, topLeftCorner.Y);

                if (tokenListWithNormilizedPositions.Count(t => t.Key.Token.Name == tokenListItem.Token.Name) > 1)
                {
                    var tokenId = tokenListItem.Id.ToString();
                    var idSize = new Size<double>(Math.Max(calculateTokenSize(tokenListItem) / 5, 1), Math.Max(calculateTokenSize(tokenListItem) / 5, 1));
                    var idBitmap = _conditionIcons.GetDigitIcon(tokenId);
                    idBitmap = ResizeBitmap(idBitmap, Size<int>.Create(idSize));
                    var drawingPositionId = new Point<double>(zeroBasedPosition.X - (idSize.Width / 2.0), zeroBasedPosition.Y - (idSize.Height / 2.0));
                    DrawImageOnBitmap(bitmap, idBitmap, Point<int>.Create(drawingPositionId));
                }
            }
        }

        return bitmap;
    }

    public static Bitmap CreateShapeOverviewBitmap(List<Point<double>> points, System.Windows.Media.Color shapeColor, double penSize)
    {
        var penSizeF = (float)penSize;
        var pointsF = points.Select(p => Point<float>.Create(p)).ToList();

        // Calculate the bounding box around the shape
        var halfPenSize = penSizeF / 2;
        var minX = (int)Math.Round(points.Min(t => t.X) - halfPenSize);
        var maxX = (int)Math.Round(points.Max(t => t.X) + halfPenSize);
        var minY = (int)Math.Round(points.Min(t => t.Y) - halfPenSize);
        var maxY = (int)Math.Round(points.Max(t => t.Y) + halfPenSize);

        var bitmap = new Bitmap(maxX - minX, maxY - minY);
        using var shapeGraphics = Graphics.FromImage(bitmap);
        var brush = shapeColor.ToDrawingBrush();

        // Offset points into bitmap-local space
        var offsetPoints = pointsF.Select(p =>
            new Point<float>(
                p.X - minX - halfPenSize,
                p.Y - minY - halfPenSize))
            .ToList();

        DrawShape(shapeGraphics, offsetPoints, brush, penSizeF);
        return bitmap;
    }

    public static Bitmap CreateFogOverviewBitmap(List<FogOverviewBitmap> shapeOverviewBitmaps, bool isFillFogEnabled, int width, int height, Point<int> backgroundOffset)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        if (isFillFogEnabled)
        {
            graphics.Clear(Color.FromArgb(127, Color.Black));

            var previousMode = graphics.CompositingMode;
            graphics.CompositingMode = CompositingMode.SourceCopy;
            using var transparentBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
            foreach (var shape in shapeOverviewBitmaps.Where(s => !s.IsFogEnabled))
            {
                // Shift shape points from player-viewport space into background image space
                var shiftedPoints = shape.ScaledPoints
                    .Select(p => new PointF(
                        p.X - backgroundOffset.X,
                        p.Y - backgroundOffset.Y))
                    .ToArray();
                graphics.FillPolygon(transparentBrush, shiftedPoints);
            }
            graphics.CompositingMode = previousMode;
        }
        else
        {
            foreach (var shape in shapeOverviewBitmaps.Where(s => s.IsFogEnabled))
            {
                graphics.DrawImage(
                    shape.Bitmap,
                    shape.OffsetFromOrigin.X - backgroundOffset.X,
                    shape.OffsetFromOrigin.Y - backgroundOffset.Y);
            }
        }

        return bitmap;
    }

    public static Bitmap CreateFogShapeOverviewBitmap(List<Point<double>> points, FogShape shape, double penSize)
    {
        var penSizeF = (float)penSize;
        var pointsF = points.Select(p => Point<float>.Create(p)).ToList();

        var halfPenSize = penSizeF / 2;
        var minX = (int)Math.Round(points.Min(t => t.X) - halfPenSize);
        var maxX = (int)Math.Round(points.Max(t => t.X) + halfPenSize);
        var minY = (int)Math.Round(points.Min(t => t.Y) - halfPenSize);
        var maxY = (int)Math.Round(points.Max(t => t.Y) + halfPenSize);

        var offsetPoints = pointsF.Select(p =>
            new PointF(
                p.X - minX - halfPenSize,
                p.Y - minY - halfPenSize))
            .ToList();

        var bitmap = new Bitmap(maxX - minX, maxY - minY, PixelFormat.Format32bppArgb);
        using var shapeGraphics = Graphics.FromImage(bitmap);

        if (shape.IsFogEnabled)
        {
            var transparentBlack = Color.FromArgb(127, Color.Black);
            using var brush = new SolidBrush(transparentBlack);
            shapeGraphics.FillPolygon(brush, offsetPoints.ToArray());
        }
        else
        {
            // Punch to fully transparent — no white fringe survives
            var previousMode = shapeGraphics.CompositingMode;
            shapeGraphics.CompositingMode = CompositingMode.SourceCopy;
            using var transparentBrush = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
            shapeGraphics.FillPolygon(transparentBrush, offsetPoints.ToArray());
            shapeGraphics.CompositingMode = previousMode;
        }

        return bitmap;
    }

    public static Bitmap CreateShapesOverviewBitmap(List<OverviewBitmap> overviewBitmaps)
    {
        // Calculate the bounding box around all shapes
        var minX = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.X));
        var minY = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
        var maxX = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.X + l.Bitmap.Width));
        var maxY = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y + l.Bitmap.Height));

        var bitmap = new Bitmap(Math.Abs(maxX - minX), Math.Abs(maxY - minY), PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        var bitmapToOrigin = new Point<int>(-minX, -minY);
        foreach (var shapeOverviewBitmap in overviewBitmaps)
        {
            var offsetX = bitmapToOrigin.X + shapeOverviewBitmap.OffsetFromOrigin.X;
            var offsetY = bitmapToOrigin.Y + shapeOverviewBitmap.OffsetFromOrigin.Y;
            graphics.DrawImage(shapeOverviewBitmap.Bitmap, offsetX, offsetY);
        }

        return bitmap;
    }

    public static Bitmap CreateMapOverview(List<OverviewBitmap> overviewBitmaps, Size<int> overviewSize, Point<int> bitmapToOrigin)
    {
        var bitmap = new Bitmap(overviewSize.Width, overviewSize.Height);
        using Graphics graph = Graphics.FromImage(bitmap);

        foreach (var overviewBitmap in overviewBitmaps)
        {
            var offsetX = bitmapToOrigin.X + overviewBitmap.OffsetFromOrigin.X;
            var offsetY = bitmapToOrigin.Y + overviewBitmap.OffsetFromOrigin.Y;
            graph.DrawImage(overviewBitmap.Bitmap, offsetX, offsetY);
        }

        return bitmap;
    }

    public static void DrawPlayerViewIndicator(Bitmap bitmap, int penSize)
    {
        using var graphics = Graphics.FromImage(bitmap);
        graphics.DrawRectangle(new Pen(System.Drawing.Color.DarkOrange, penSize), 0, 0, bitmap.Width, bitmap.Height);
    }


    private static bool IsTokenVisible(Point<int> drawingPosition, int gridSize)
    {
        var isVisible = true;

        if (drawingPosition.X + gridSize < 0)
        {
            isVisible = false;
        }
        else if (drawingPosition.X - gridSize > Constants.MapSize.Width)
        {
            isVisible = false;
        }
        else if (drawingPosition.Y + gridSize < 0)
        {
            isVisible = false;
        }
        else if (drawingPosition.Y - gridSize > Constants.MapSize.Height)
        {
            isVisible = false;
        }

        return isVisible;
    }

    private static void DrawTokenId(Bitmap bitmap, string tokenId, TokenListItem tokenListItem, Point<int> drawingPosition, Size<int> tokenSize)
    {
        if (tokenId != null && tokenId != "")
        {
            var idSize = new Size<double>(Math.Max(tokenSize.Width / 5, 1), Math.Max(tokenSize.Height / 5, 1));

            var idBitmap = _conditionIcons.GetDigitIcon(tokenId);

            var resizedConditionImage = ResizeBitmap(idBitmap, Size<int>.Create(idSize));
            resizedConditionImage.RotateFlip(tokenListItem.Token.GetOrientation());

            var drawingPositionId = Point<double>.Create(drawingPosition);

            drawingPositionId.X += tokenSize.Width / 2.0 - idSize.Width / 2.0;
            drawingPositionId.Y += tokenSize.Height / 2.0 - idSize.Height / 2.0;

            DrawImageOnBitmap(bitmap, resizedConditionImage, Point<int>.Create(drawingPositionId));
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
        if (conditionSize.Width >= 1 && conditionSize.Height >= 1)
        {
            for (int i = 0; i < 4 && i < tokenListItem.Conditions.Count; i++)
            {
                var condition = tokenListItem.Conditions[i];
                Bitmap conditionBitmap;
                if (condition == Condition.Height)
                {
                    conditionBitmap = _conditionIcons.GetDigitIcon(tokenListItem.Height);
                }
                else
                {
                    conditionBitmap = _conditionIcons.GetConditionIcon(condition);
                }
                var resizedConditionImage = ResizeBitmap(conditionBitmap, Size<int>.Create(conditionSize));
                resizedConditionImage.RotateFlip(tokenListItem.Token.GetOrientation());

                var drawingPosition = Point<double>.Create(tokenDrawingPosition);

                drawingPosition.X += (tokenSize.Width * conditionPositions[i].X) - (conditionSize.Width * conditionPositions[i].X);
                drawingPosition.Y += (tokenSize.Height * conditionPositions[i].Y) - (conditionSize.Height * conditionPositions[i].Y);

                DrawImageOnBitmap(bitmap, resizedConditionImage, Point<int>.Create(drawingPosition));
            }
        }
    }

    private static void AddTokenHeightCondition(TokenListItem tokenListItem)
    {
        if (tokenListItem.Conditions.Contains(Condition.Height))
        {
            tokenListItem.Conditions.Remove(Condition.Height);
            if (tokenListItem.Height != 0)
            {
                tokenListItem.Conditions.Insert(0, Condition.Height);
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
        var drawingPosition = Point<int>.Create(gridStart);
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
                if (dist > (penSize / 5.0f))
                {
                    var newPoint = new Point<float>((coord1.X + coord2.X) / 2, (coord1.Y + coord2.Y) / 2);
                    points.Insert(i + 1, newPoint);
                    smoothRequired = true;
                }
            }
        }
    }
}
