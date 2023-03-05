using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigitalBattleMap
{
    public static class TabIndex
    {
        public const int Background = 0;
        public const int Grid = 1;
        public const int Drawing = 2;
        public const int Tokens = 3;
    }

    public enum ArrowDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    public enum TokenSize
    {
        Tiny,
        Small,
        Medium,
        Large,
        Huge,
        Gargantuan
    }
}
