using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Block_Buster
{
    public class Ball
    {
        public byte Index;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Speed;
        public byte Type;
        public float Size;
        public bool TouchedLast;
        public List<Ball> NoCollisionBall = new List<Ball>();
        public List<Brick> NoCollisionBrick = new List<Brick>();
        public bool NoCollisionPaddle;
        public Rectangle Source
        {
            get
            {
                return new Rectangle(96 + (Type % 4) * 8, 48 + (Type / 4) * 8, 8, 8);
            }
        }

    }
}
