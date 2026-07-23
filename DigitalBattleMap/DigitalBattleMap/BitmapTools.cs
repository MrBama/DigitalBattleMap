using DigitalBattleMap.Common;
using DigitalBattleMap.DataClasses;
using DigitalBattleMap.DrawingShapes;
using DigitalBattleMap.FogShapes;
using DigitalBattleMap.Imaging;
using DigitalBattleMap.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalBattleMap;

public static class BitmapTools
{
    private static readonly ConditionIcons _conditionIcons = new();

    public static IImage CreateGrid(int gridSize)
    {
        var gridBitmap = CreateEmptyBitmap();
        var gridOrigin = new Point<int>(Constants.MapSize.Width / 2, Constants.MapSize.Height / 2);
        DrawGrid(gridBitmap, gridSize, gridOrigin, 1);
        return gridBitmap;
    }

    public static IImage CreateEmptyBitmap()
    {
        return ImageFactory.Create(Constants.MapSize.Width, Constants.MapSize.Height);
    }

    public static IImage CreateBlackBitmap()
    {
        return ImageFactory.Create(Constants.MapSize.Width, Constants.MapSize.Height, Color.Black);
    }

    public static IImage CreateColorButton(Color color, bool addSelectionIndicator)
    {
        var image = ImageFactory.Create(70, 70);
        image.FillEllipse(color, Rectangle.FromTopLeft(9, 9, 50, 50));

        if (addSelectionIndicator)
        {
            image.DrawEllipse(System.Drawing.Color.Gray, 4, Rectangle.FromTopLeft(4, 4, 60, 60));
        }

        return image;
    }

    public static IImage CreateEraserButton(bool addSelectionIndicator)
    {
        var image = ImageFactory.Create(70, 70);
        image.FillRectangle(System.Drawing.Color.Yellow, Rectangle.FromTopLeft(19, 14, 30, 40));
        image.FillRectangle(System.Drawing.Color.Pink, Rectangle.FromTopLeft(19, 14, 30, 12));

        if (addSelectionIndicator)
        {
            image.DrawEllipse(System.Drawing.Color.Gray, 4, Rectangle.FromTopLeft(4, 4, 60, 60));
        }

        return image;
    }

    public static IImage CreateArrowButton(ArrowDirection direction)
    {
        var image = ImageFactory.Create(70, 70);
        var points = direction switch
        {
            ArrowDirection.Up => new Point<float>[] { new(9, 59), new(59, 59), new(34, 9) },
            ArrowDirection.Down => new Point<float>[] { new(9, 9), new(59, 9), new(34, 59) },
            ArrowDirection.Left => new Point<float>[] { new(59, 9), new(59, 59), new(9, 34) },
            ArrowDirection.Right => new Point<float>[] { new(9, 9), new(9, 59), new(59, 34) },
            _ => throw new NotImplementedException(),
        };

        image.FillPolygon(Color.Black, points);
        return image;
    }

    public static IImage CreateZoomButton(bool isZoomInButton)
    {
        var image = ImageFactory.Create(70, 70);
        image.FillRectangle(Color.Black, Rectangle.FromTopLeft(9, 30, 50, 8));

        if (isZoomInButton)
        {
            image.FillRectangle(Color.Black, Rectangle.FromTopLeft(30, 9, 8, 50));
        }

        return image;
    }

    public static IImage CreateCropButton()
    {
        var image = ImageFactory.Create(70, 70);
        image.FillRectangle(System.Drawing.Color.Black, Rectangle.FromTopLeft(25, 35, 40, 30));

        return image;
    }

    public static IImage CropBitmap(IImage image, System.Drawing.Rectangle rectangle)
    {
        return image.CropTo(Rectangle.FromTopLeft(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
    }

    public static IImage ResizeBitmap(IImage image)
    {
        return ResizeBitmap(image, Constants.MapSize);
    }

    public static IImage ResizeToBitmap(IImage bitmap, IImage bitmapTo)
    {
        return ResizeBitmap(bitmap, new(bitmapTo.Width, bitmapTo.Height));
    }

    public static IImage ResizeBitmap(IImage image, Size<int> size)
    {
        return image.ResizeTo(size.Width, size.Height);
    }

    public static void DrawToken(IImage image, TokenListItem tokenListItem, string tokenId, int gridSize)
    {
        (var drawingPosition, var tokenSize) = CalculateTokenDrawingPositionAndSize(tokenListItem.Token.GetSizeFactor(), tokenListItem.Position, gridSize);

        // Resize and draw token
        if (IsTokenVisible(drawingPosition, gridSize))
        {
            var resizedTokenImage = ResizeBitmap(tokenListItem.GetBitmap(), tokenSize);
            resizedTokenImage.Rotate(tokenListItem.Token.GetBitmapRotation());
            DrawImageOnBitmap(image, resizedTokenImage, drawingPosition);
            AddTokenHeightCondition(tokenListItem);
            DrawTokenConditions(image, tokenListItem, drawingPosition, tokenSize);
            DrawTokenId(image, tokenId, tokenListItem, drawingPosition, tokenSize);
        }
    }

    public static void DrawTokenSelection(IImage image, double tokenSizeFactor, Point<int> tokenPosition, int gridSize)
    {
        (var drawingPosition, var tokenSize) = CalculateTokenDrawingPositionAndSize(tokenSizeFactor, tokenPosition, gridSize);

        if (IsTokenVisible(drawingPosition, gridSize))
        {
            image.DrawEllipse(System.Drawing.Color.Blue, 4, Rectangle.FromTopLeft(drawingPosition.X, drawingPosition.Y, tokenSize.Width, tokenSize.Height));
        }
    }

    public static IImage CreateTokenBitmap(IImage image)
    {
        if (image.Width == image.Height)
        {
            return image.Clone();
        }

        var size = Math.Max(image.Width, image.Height);
        var tokenImage = ImageFactory.Create(size, size);
        var position = new Point<int>();

        if (size == image.Width)
        {
            position.Y = (size - image.Height) / 2;
        }
        else
        {
            position.X = (size - image.Width) / 2;
        }

        DrawImageOnBitmap(tokenImage, image, position);
        return tokenImage;
    }

    public static IImage MergeBitmaps(params IImage[] bitmaps)
    {
        if (bitmaps.Count() > 0)
        {
            var image = bitmaps.First();
            for (int i = 1; i < bitmaps.Count(); i++)
            {
                DrawImageOnBitmap(image, bitmaps[i], new Point<int>());
            }
            return image;
        }
        else
        {
            return CreateEmptyBitmap();
        }
    }

    public static IImage ConcateDigitBitmaps(List<IImage> bitmaps)
    {
        int width = 0;
        int height = 0;
        foreach (var map in bitmaps)
        {
            width = Math.Max(width, map.Width);
            height = Math.Max(height, map.Height);
        }

        var image = ImageFactory.Create(width, height);

        // Add 25% clearance for the image
        var widthPadding = image.Width / 4;
        var heightPadding = image.Height / 4;

        var imageWidth = image.Width - (2 * widthPadding);
        var imageHeight = image.Height - (2 * heightPadding);

        image.FillEllipse(System.Drawing.Color.White, Rectangle.FromTopLeft(0, 0, image.Width, image.Height));
        for (int i = 0; i < bitmaps.Count; i++)
        {
            var digitPadding = DigitPadding(bitmaps.Count, i, image.Width);
            image.DrawImage(bitmaps[i], Rectangle.FromTopLeft(widthPadding + digitPadding, heightPadding, imageWidth, imageHeight));
        }

        return image;
    }

    public static void RotateBitmap(IImage image, BitmapRotation rotation)
    {
        image.Rotate(rotation);
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

    public static IImage CreateFogOfWarBitmap(System.Drawing.Rectangle area, List<SelectedArea> removedAreas)
    {
        var image = ImageFactory.Create(area.Width, area.Height, Color.Black);

        if (removedAreas.Count > 0)
        {
            // Image is 0 based while area is not
            foreach (var removedArea in removedAreas)
            {
                var points = new List<Point<float>>();
                foreach (var point in removedArea.Points)
                {
                    points.Add(new Point<float>((float)point.X - area.X, (float)point.Y - area.Y));
                }
                image.FillPolygon(System.Drawing.Color.White, points.ToArray());
            }

            image.MakeColorTransparent(System.Drawing.Color.White);
        }

        return image;
    }

    /**
     * First draws all transparent polygon aftewords the black polygons. 
     * This gives fog priority on overlapping shaps and aligns with the visuals in the UI.
     */
    public static void DrawFogShapes(IImage image, List<FogShape> shapes, Size<double> canvasSize)
    {
        foreach (var shape in shapes.OrderBy(s => s.IsFogEnabled))
        {
            FogPolygon(canvasSize, image, shape);
        }
    }

    private static void FogPolygon(Size<double> canvasSize, IImage image, FogShape shape)
    {
        if (!shape.Points.Any())
        {
            return;
        }

        var pointsF = new List<Point<float>>();
        foreach (var point in shape.Points)
        {
            var halfSize = shape.PenSizeCanvas / 2;
            var middleOfPoint = new Point<double>(point.X - halfSize, point.Y - halfSize);

            var resizedX = (float)middleOfPoint.X.Map(0, canvasSize.Width, 0, Constants.MapSize.Width);
            var resizedY = (float)middleOfPoint.Y.Map(0, canvasSize.Height, 0, Constants.MapSize.Height);
            pointsF.Add(new Point<float>(resizedX, resizedY));
        }

        if (shape.IsFogEnabled)
        {
            image.FillSmoothPolygon(System.Drawing.Color.Black, pointsF.ToArray());
        }
        else
        {
            image.FillPolygon(System.Drawing.Color.Transparent, pointsF.ToArray(), blendColors: false);
        }
    }

    public static void DrawShapes(IImage image, List<DrawingShape> shapes, Size<double> canvasSize)
    {
        foreach (var shape in shapes)
        {
            var pointsF = new List<Point<float>>();
            var penSizeF = (float)shape.PenSize;

            foreach (var point in shape.Points)
            {
                var halfSize = shape.PenSizeCanvas / 2;
                var middleOfPoint = new Point<double>(point.X - halfSize, point.Y - halfSize);

                var resizedX = (float)middleOfPoint.X.Map(0, canvasSize.Width, 0, Constants.MapSize.Width);
                var resizedY = (float)middleOfPoint.Y.Map(0, canvasSize.Height, 0, Constants.MapSize.Height);
                pointsF.Add(new Point<float>(resizedX, resizedY));
            }

            DrawShape(image, pointsF, shape.Color, penSizeF);
        }
    }

    private static void DrawShape(IImage image, List<Point<float>> points, Color color, float penSizeF)
    {
        SmoothLine(points, penSizeF);
        foreach (var point in points)
        {
            image.FillEllipse(color, Rectangle.FromTopLeft((int)point.X, (int)point.Y, (int)penSizeF, (int)penSizeF));
        }
    }

    public static void DrawGrid(IImage image, int gridSize, Point<int> gridOrigin, int penSize)
    {
        var gridOffset = Mathematics.CalculateGridOffset(gridSize, gridOrigin);

        for (int x = gridOffset.X; x < image.Width; x += gridSize)
        {
            image.DrawLine(Color.Black, penSize, x, 0, x, image.Height);
        }

        for (int y = gridOffset.Y; y < image.Height; y += gridSize)
        {
            image.DrawLine(Color.Black, penSize, 0, y, image.Width, y);
        }
    }

    public static IImage CreateTokenOverviewBitmap(Dictionary<TokenListItem, Point<int>> tokenListWithNormilizedPositions, int gridSize)
    {
        // Helper lambda's to calculate token sizes
        var calculateTokenSize = (TokenListItem tokenListItem) => (int)Math.Round(gridSize * tokenListItem.Token.GetSizeFactor());
        var calculateHalfTokenSize = (TokenListItem tokenListItem) => (int)Math.Round(gridSize * tokenListItem.Token.GetSizeFactor() / 2);

        // Calculate the bounding box around the tokens
        var minTokenPositionX = tokenListWithNormilizedPositions.Min(kv => kv.Value.X - calculateHalfTokenSize(kv.Key));
        var maxTokenPositionX = tokenListWithNormilizedPositions.Max(kv => kv.Value.X + calculateHalfTokenSize(kv.Key));
        var minTokenPositionY = tokenListWithNormilizedPositions.Min(kv => kv.Value.Y - calculateHalfTokenSize(kv.Key));
        var maxTokenPositionY = tokenListWithNormilizedPositions.Max(kv => kv.Value.Y + calculateHalfTokenSize(kv.Key));

        var image = ImageFactory.Create(maxTokenPositionX - minTokenPositionX, maxTokenPositionY - minTokenPositionY);

        foreach ((var tokenListItem, var position) in tokenListWithNormilizedPositions)
        {
            if (tokenListItem.Visible)
            {
                // Tokens positions can be negative but bitmap positions always start at 0
                var zeroBasedPosition = new Point<int>(position.X - minTokenPositionX, position.Y - minTokenPositionY);
                var topLeftCorner = new Point<int>(zeroBasedPosition.X - calculateHalfTokenSize(tokenListItem), zeroBasedPosition.Y - calculateHalfTokenSize(tokenListItem));
                var tokenImage = ResizeBitmap(tokenListItem.GetBitmap(), new Size<int>(calculateTokenSize(tokenListItem), calculateTokenSize(tokenListItem)));
                image.DrawImage(tokenImage, topLeftCorner.X, topLeftCorner.Y);

                if (tokenListWithNormilizedPositions.Count(t => t.Key.Token.Name == tokenListItem.Token.Name) > 1)
                {
                    var tokenId = tokenListItem.Id.ToString();
                    var idSize = new Size<double>(Math.Max(calculateTokenSize(tokenListItem) / 5, 1), Math.Max(calculateTokenSize(tokenListItem) / 5, 1));
                    var idBitmap = _conditionIcons.GetDigitIcon(tokenId);
                    idBitmap = ResizeBitmap(idBitmap, Size<int>.Create(idSize));
                    var drawingPositionId = new Point<double>(zeroBasedPosition.X - (idSize.Width / 2.0), zeroBasedPosition.Y - (idSize.Height / 2.0));
                    DrawImageOnBitmap(image, idBitmap, Point<int>.Create(drawingPositionId));
                }
            }
        }

        return image;
    }

    public static IImage CreateShapeOverviewBitmap(List<Point<double>> points, System.Windows.Media.Color shapeColor, double penSize)
    {
        var penSizeF = (float)penSize;
        var pointsF = points.Select(p => Point<float>.Create(p)).ToList();

        // Calculate the bounding box around the shape
        var halfPenSize = penSizeF / 2;
        var minX = (int)Math.Round(points.Min(t => t.X) - halfPenSize);
        var maxX = (int)Math.Round(points.Max(t => t.X) + halfPenSize);
        var minY = (int)Math.Round(points.Min(t => t.Y) - halfPenSize);
        var maxY = (int)Math.Round(points.Max(t => t.Y) + halfPenSize);

        var image = ImageFactory.Create(maxX - minX, maxY - minY);

        // Offset points into bitmap-local space
        var offsetPoints = pointsF.Select(p =>
            new Point<float>(
                p.X - minX - halfPenSize,
                p.Y - minY - halfPenSize))
            .ToList();

        DrawShape(image, offsetPoints, shapeColor, penSizeF);
        return image;
    }

    public static IImage CreateFogOverviewBitmap(List<FogOverviewBitmap> shapeOverviewBitmaps, bool isFillFogEnabled, int width, int height, Point<int> backgroundOffset)
    {
        IImage image;

        if (isFillFogEnabled)
        {
            image = ImageFactory.Create(width, height, System.Drawing.Color.FromArgb(127, Color.Black));

            foreach (var shape in shapeOverviewBitmaps.Where(s => !s.IsFogEnabled))
            {
                // Shift shape points from player-viewport space into background image space
                var shiftedPoints = shape.ScaledPoints
                    .Select(p => new Point<float>(
                        p.X - backgroundOffset.X,
                        p.Y - backgroundOffset.Y))
                    .ToArray();
                image.FillPolygon(System.Drawing.Color.Transparent, shiftedPoints, blendColors: false);
            }
        }
        else
        {
            image = ImageFactory.Create(width, height);
            foreach (var shape in shapeOverviewBitmaps.Where(s => s.IsFogEnabled))
            {
                image.DrawImage(shape.Bitmap, shape.OffsetFromOrigin.X - backgroundOffset.X, shape.OffsetFromOrigin.Y - backgroundOffset.Y);
            }
        }

        return image;
    }

    public static IImage CreateFogShapeOverviewBitmap(List<Point<double>> points, FogShape shape, double penSize)
    {
        var penSizeF = (float)penSize;
        var pointsF = points.Select(p => Point<float>.Create(p)).ToList();

        var halfPenSize = penSizeF / 2;
        var minX = (int)Math.Round(points.Min(t => t.X) - halfPenSize);
        var maxX = (int)Math.Round(points.Max(t => t.X) + halfPenSize);
        var minY = (int)Math.Round(points.Min(t => t.Y) - halfPenSize);
        var maxY = (int)Math.Round(points.Max(t => t.Y) + halfPenSize);

        var offsetPoints = pointsF.Select(p =>
            new Point<float>(
                p.X - minX - halfPenSize,
                p.Y - minY - halfPenSize))
            .ToList();

        var image = ImageFactory.Create(maxX - minX, maxY - minY);

        if (shape.IsFogEnabled)
        {
            var transparentBlack = System.Drawing.Color.FromArgb(127, System.Drawing.Color.Black);
            image.FillPolygon(transparentBlack, offsetPoints.ToArray());
        }
        else
        {
            image.FillPolygon(System.Drawing.Color.Transparent, offsetPoints.ToArray(), blendColors: false);
        }

        return image;
    }

    public static IImage CreateShapesOverviewBitmap(List<OverviewBitmap> overviewBitmaps)
    {
        // Calculate the bounding box around all shapes
        var minX = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.X));
        var minY = Mathematics.Min(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y));
        var maxX = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.X + l.Bitmap.Width));
        var maxY = Mathematics.Max(overviewBitmaps.Select(l => l.OffsetFromOrigin.Y + l.Bitmap.Height));

        var image = ImageFactory.Create(Math.Abs(maxX - minX), Math.Abs(maxY - minY));

        var bitmapToOrigin = new Point<int>(-minX, -minY);
        foreach (var shapeOverviewBitmap in overviewBitmaps)
        {
            var offsetX = bitmapToOrigin.X + shapeOverviewBitmap.OffsetFromOrigin.X;
            var offsetY = bitmapToOrigin.Y + shapeOverviewBitmap.OffsetFromOrigin.Y;
            image.DrawImage(shapeOverviewBitmap.Bitmap, offsetX, offsetY);
        }

        return image;
    }

    public static IImage CreateMapOverview(List<OverviewBitmap> overviewBitmaps, Size<int> overviewSize, Point<int> bitmapToOrigin)
    {
        var image = ImageFactory.Create(overviewSize.Width, overviewSize.Height);

        foreach (var overviewBitmap in overviewBitmaps)
        {
            var offsetX = bitmapToOrigin.X + overviewBitmap.OffsetFromOrigin.X;
            var offsetY = bitmapToOrigin.Y + overviewBitmap.OffsetFromOrigin.Y;
            image.DrawImage(overviewBitmap.Bitmap, offsetX, offsetY);
        }

        return image;
    }

    public static void DrawPlayerViewIndicator(IImage image, int penSize)
    {
        image.DrawRectangle(System.Drawing.Color.DarkOrange, penSize, Rectangle.FromTopLeft(0, 0, image.Width, image.Height));
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

    private static void DrawTokenId(IImage bitmap, string tokenId, TokenListItem tokenListItem, Point<int> drawingPosition, Size<int> tokenSize)
    {
        if (tokenId != null && tokenId != "")
        {
            var idSize = new Size<double>(Math.Max(tokenSize.Width / 5, 1), Math.Max(tokenSize.Height / 5, 1));

            var idBitmap = _conditionIcons.GetDigitIcon(tokenId);

            var resizedConditionImage = ResizeBitmap(idBitmap, Size<int>.Create(idSize));
            resizedConditionImage.Rotate(tokenListItem.Token.GetBitmapRotation());

            var drawingPositionId = Point<double>.Create(drawingPosition);

            drawingPositionId.X += tokenSize.Width / 2.0 - idSize.Width / 2.0;
            drawingPositionId.Y += tokenSize.Height / 2.0 - idSize.Height / 2.0;

            DrawImageOnBitmap(bitmap, resizedConditionImage, Point<int>.Create(drawingPositionId));
        }
    }

    private static void DrawTokenConditions(IImage bitmap, TokenListItem tokenListItem, Point<int> tokenDrawingPosition, Size<int> tokenSize)
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
                //Bitmap conditionBitmap;
                IImage conditionImage;
                if (condition == Condition.Height)
                {
                    conditionImage = _conditionIcons.GetDigitIcon(tokenListItem.Height);
                }
                else
                {
                    conditionImage = ImageFactory.FromDrawingBitmap(_conditionIcons.GetConditionIcon(condition));
                }
                var resizedConditionImage = ResizeBitmap(conditionImage, Size<int>.Create(conditionSize));
                resizedConditionImage.Rotate(tokenListItem.Token.GetBitmapRotation());

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

    private static void DrawImageOnBitmap(IImage bitmap, IImage image, Point<int> position)
    {
        bitmap.DrawImage(image, position.X, position.Y);
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
