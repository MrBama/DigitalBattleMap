using System;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace DigitalBattleMap
{
    public class BackgroundController
    {
        private Bitmap _backgroundBitmap;
        private Bitmap _fullBackgroundBitmap;
        private Rectangle _area;
        private IWindowService _windowService;
        private Size<int> _bitmapSize;
        private Size<double> _canvasSize;
        private Size<int> _gridCells = new Size<int>(10, 10);
        private Point<double> _mouseDownPosition;
        private bool _mouseDown;

        public BackgroundController(IWindowService windowService)
        {
            _windowService = windowService;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            _bitmapSize = BitmapTools.GetBitmapSize();
        }

        public event EventHandler BackgroundEditorUpdated;
        public event EventHandler BackgroundUpdated;

        public int GridCellsWidth
        {
            get => _gridCells.Width;
            set
            {
                if (value != _gridCells.Width)
                {
                    _gridCells.Width = value;
                    NotifyBackgroundEditorUpdated();
                }
            }
        }

        public int GridCellsHeight
        {
            get => _gridCells.Height;
            set
            {
                if (value != _gridCells.Height)
                {
                    _gridCells.Height = value;
                    NotifyBackgroundEditorUpdated();
                }
            }
        }

        public BitmapSource GetBackgroundBitmapSource()
        {
            return _backgroundBitmap.ToBitmapImage();
        }

        public Bitmap GetBackgroundBitmap()
        {
            return _backgroundBitmap;
        }

        public bool HasOpenedBackground()
        {
            return _fullBackgroundBitmap != null;
        }

        public void OpenBackground()
        {
            if (_windowService.ShowOpenFileDialog(out string path))
            {
                _fullBackgroundBitmap = BitmapTools.LoadBitmap(path);
                _area = new Rectangle(
                    (_fullBackgroundBitmap.Width / 2) - (_bitmapSize.Width / 2),
                    (_fullBackgroundBitmap.Height / 2) - (_bitmapSize.Height / 2),
                    _bitmapSize.Width,
                    _bitmapSize.Height);

                _backgroundBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
                ExtractGridCells(Path.GetFileNameWithoutExtension(path));
                NotifyBackgroundUpdated();
            }
        }

        public void ClearBackground()
        {
            _fullBackgroundBitmap = null;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            NotifyBackgroundUpdated();
        }

        public void MouseDown(Point<double> point)
        {
            _mouseDownPosition = point;
            _mouseDown = true;
        }

        public void MouseUp(Point<double> point)
        {
            if (_fullBackgroundBitmap != null && _mouseDown)
            {
                var distanceX = _mouseDownPosition.X - point.X;
                distanceX = distanceX.Map(0, _canvasSize.Width, 0, _area.Width);
                _area.X += (int)distanceX;

                var distanceY = _mouseDownPosition.Y - point.Y;
                distanceY = distanceY.Map(0, _canvasSize.Width, 0, _area.Width);
                _area.Y += (int)distanceY;

                CreateBackground();
            }
            _mouseDown = false;
        }

        public void SetCanvasSize(Size<double> canvasSize)
        {
            _canvasSize = canvasSize;
        }

        public void ZoomIn(double zoomPercentage)
        {
            if (_fullBackgroundBitmap != null)
            {
                var zoomFactor = (100 + zoomPercentage) / 100;
                ZoomWithZoomFactor(zoomFactor);
                CreateBackground();
            }
        }

        public void ZoomOut(double zoomPercentage)
        {
            if (_fullBackgroundBitmap != null)
            {
                var zoomFactor = (100 + zoomPercentage) / 100;
                zoomFactor = 1 / zoomFactor;
                ZoomWithZoomFactor(zoomFactor);
                CreateBackground();
            }
        }

        public void Zoom(double zoomFactor)
        {
            if (_fullBackgroundBitmap != null)
            {
                ZoomWithZoomFactor(zoomFactor);
                CreateBackground();
            }
        }

        private void ZoomWithZoomFactor(double zoomFactor)
        {
            var newWidth = _area.Width / zoomFactor;
            var newHeight = _area.Height/ zoomFactor;
            _area.X += (int)Math.Round((_area.Width - newWidth) / 2);
            _area.Y += (int)Math.Round((_area.Height - newHeight) / 2);
            _area.Width = (int)Math.Round(newWidth);
            _area.Height = (int)Math.Round(newHeight);
        }

        public void MoveBackground(ArrowDirection direction, int gridSize)
        {
            if (_fullBackgroundBitmap != null)
            {
                double preciseGridSize = gridSize;
                var distanceX = (int)Math.Round(preciseGridSize.Map(0, _bitmapSize.Width, 0, _area.Width));
                var distanceY = (int)Math.Round(preciseGridSize.Map(0, _bitmapSize.Height, 0, _area.Height));

                switch (direction)
                {
                    case ArrowDirection.Up:
                        _area.Y -= distanceY;
                        break;
                    case ArrowDirection.Down:
                        _area.Y += distanceY;
                        break;
                    case ArrowDirection.Left:
                        _area.X -= distanceX;
                        break;
                    case ArrowDirection.Right:
                        _area.X += distanceX;
                        break;
                }

                CreateBackground();
            }
        }

        public void FitToGrid(int gridSize)
        {
            // Resize background to match grid size
            var newSize = new Size<double>(gridSize * GridCellsWidth, gridSize * GridCellsHeight);
            double factor = _fullBackgroundBitmap.Width / newSize.Width;

            _area.Width = (int)Math.Round(_bitmapSize.Width * factor);
            _area.Height = (int)Math.Round(_bitmapSize.Height * factor);

            // Move background grid to 0,0
            double backgroundGridSize = _fullBackgroundBitmap.Width / GridCellsWidth;
            _area.X += (int)Math.Round(backgroundGridSize - (_area.X % backgroundGridSize));
            _area.Y += (int)Math.Round(backgroundGridSize - (_area.Y % backgroundGridSize));

            // Move background grid to overlap with normal grid
            var gridOffset = BitmapTools.CalculateGridOffset(gridSize).ToPointDouble();
            _area.X -= (int)Math.Round(gridOffset.X.Map(0, _bitmapSize.Width, 0, _area.Width));
            _area.Y -= (int)Math.Round(gridOffset.Y.Map(0, _bitmapSize.Height, 0, _area.Height));

            CreateBackground();
        }

        public void AddToSaveFile(SaveFile saveFile)
        {
            saveFile.FullBackground = _fullBackgroundBitmap;
            saveFile.BackgroundArea = _area;
            saveFile.GridCellsWidth = GridCellsWidth;
            saveFile.GridCellsHeight = GridCellsHeight;
        }

        public void OpenSaveFile(SaveFile saveFile)
        {
            ClearBackground();

            GridCellsWidth = saveFile.GridCellsWidth;
            GridCellsHeight = saveFile.GridCellsHeight;

            if (saveFile.FullBackground != null)
            {
                _fullBackgroundBitmap = saveFile.FullBackground;
                _area = saveFile.BackgroundArea;
                CreateBackground();
            }
        }

        private void NotifyBackgroundUpdated()
        {
            BackgroundUpdated?.Invoke(this, new EventArgs());
        }

        private void NotifyBackgroundEditorUpdated()
        {
            BackgroundEditorUpdated?.Invoke(this, new EventArgs());
        }

        private void CreateBackground()
        {
            var croppedBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _area);
            _backgroundBitmap = BitmapTools.ResizeBitmap(croppedBitmap);
            NotifyBackgroundUpdated();
        }

        private void ExtractGridCells(string fileName)
        {
            GridCellsWidth = 10;
            GridCellsHeight = 10;

            var startIndex = fileName.IndexOf("(");
            if (startIndex != -1)
            {
                var endIndex = fileName.Substring(startIndex).IndexOf(")");
                if (endIndex != -1)
                {
                    var size = fileName.Substring(startIndex + 1, endIndex - 1);
                    var widthAndHeight = size.ToLower().Split("x");
                    if (widthAndHeight.Length == 2)
                    {
                        try
                        {
                            GridCellsWidth = int.Parse(widthAndHeight[0]);
                            GridCellsHeight = int.Parse(widthAndHeight[1]);
                        }
                        catch
                        {
                            GridCellsWidth = 10;
                            GridCellsHeight = 10;
                        }
                    }
                }
            }
        }
    }
}
