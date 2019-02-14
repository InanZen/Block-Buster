using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Block_Buster
{
    public class Cursor :  SpriteSheet
    {
        public bool Active;
        public byte Type;
        public ushort FrameCount;
        public ushort Frame;
        public float FrameTime;
        float CurTime;

        public Rectangle Source
        {
            get
            {
                return GetSourceRect((ushort)(Type * FrameCount + Frame));
            }
        }

        public Cursor(Texture2D texture, ushort spriteW, ushort spriteH, ushort frames, float frameTime)
            : base(texture, spriteW, spriteH, 0, 0)
        {
            Active = true;
            Type = 0;
            FrameCount = frames;
            FrameTime = frameTime;
            CurTime = frameTime;
        }

        public void Update(GameTime gameTime)
        {
            if (Active)
            {
                float dT = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
                CurTime -= dT;
                if (CurTime <= 0)
                {
                    CurTime += FrameTime;
                    Frame++;
                    if (Frame >= FrameCount - 1)
                        Frame = 0;
                }
            }
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            if (Active)
            {
                MouseState ms = Mouse.GetState();
                Vector2 pos = new Vector2(ms.X, ms.Y);
                spriteBatch.Draw(Texture, pos, Source, Color.White, 0, new Vector2(8, 8), 1f, SpriteEffects.None, 0);
            }
        }
    }
}
