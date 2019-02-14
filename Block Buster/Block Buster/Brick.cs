using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Block_Buster
{
    public class Brick
    {
        public int GID
        {
            get
            {
                return Color * 4 + Strength;
            }
        }
        public byte Color;
        public byte Strength;
        public Drop Drop;
        public int Index;

        public Brick(int index, byte color, byte strength)
        {
            Index = index;
            Color = color;
            Strength = strength;
        }

    }
}
