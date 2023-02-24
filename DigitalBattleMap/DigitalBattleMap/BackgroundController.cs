using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public class BackgroundController
    {
        private Bitmap _backgroundBitmap;
        private Bitmap _fullBackgroundBitmap;
        private Rectangle _rectangle;
        private IWindowService _windowService;
        private int _width;
        private int _height;

        public BackgroundController(IWindowService windowService)
        {
            _windowService = windowService;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            _width = _backgroundBitmap.Width;
            _height = _backgroundBitmap.Height;
        }

        public event EventHandler BackgroundUpdated;

        public Bitmap BackgroundBitmap { get => _backgroundBitmap; }

        public void OpenBackground()
        {
            if(_windowService.ShowFileDialog(out string path))
            { 
                _fullBackgroundBitmap = new Bitmap(path);
                _rectangle = new Rectangle((_fullBackgroundBitmap.Width / 2) - (_width / 2), (_fullBackgroundBitmap.Height / 2) - (_height / 2), _width, _height);
                _backgroundBitmap = BitmapTools.CropBitmap(_fullBackgroundBitmap, _rectangle);
                NotifyBackgroundUpdated();
            }
        }

        public void ClearBackground()
        {
            _fullBackgroundBitmap = null;
            _backgroundBitmap = BitmapTools.CreateEmptyBitmap();
            NotifyBackgroundUpdated();
        }

        private void NotifyBackgroundUpdated()
        {
            BackgroundUpdated?.Invoke(this, new EventArgs());
        }
    }
}
