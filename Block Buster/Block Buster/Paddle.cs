using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
namespace Block_Buster
{
    public class Paddle
    {
        public int FirstgID
        {
            get
            {
                int skip = 1;
                for (int i = 1; i < Length; i++)
                    skip += i;
                return 24 + Color * 12 + skip;
            }
        }
        public byte Length;
        public byte Color;
        public float Position;
        /// <summary>
        /// 0-mouse, 1-arrow keys, 2-wsad
        /// </summary>
        public byte Controller;

        public Paddle(byte color)
        {
            Length = 2;
            Color = color;
            Position = 0.5f;
        }
    }
}
