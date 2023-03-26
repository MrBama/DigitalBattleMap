using DigitalBattleMap.Common;
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
        private DrawLayer _drawLayer;
        private Bitmap _bitmap;

        public MapUpdate(DrawLayer drawLayer, Bitmap bitmap)
        {
            _drawLayer = drawLayer;
            _bitmap = new Bitmap(bitmap);
        }

        public Bitmap GetBitmap()
        {
            return _bitmap;
        }

        public string GetAction()
        {
            if (_drawLayer == DrawLayer.Background)
            {
                return TcpConstants.UpdateMapBackgroundAction;
            }
            else if (_drawLayer == DrawLayer.GridAndStrokes)
            {
                return TcpConstants.UpdateMapGridAndStrokesAction;
            }
            else
            {
                return TcpConstants.UpdateMapTokensAction;
            }
        }
    }
}
