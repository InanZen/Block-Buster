using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Block_Buster
{
    public enum DropType
    {
        Shrink = 0,
        Enlarge = 1,
        Life = 2,
        Death = 3,
        Accelerate = 4,
        Slow = 5,
        BallShrink = 6,
        BallEnlarge = 7,
        Ball = 8,
        Key = 9
    }
    public class Drop
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public DropType Type;
        public Rectangle Source
        {
            get
            {
                return new Rectangle((byte)Type * 16, 192, 16, 16);
            }
        }
    }
}
