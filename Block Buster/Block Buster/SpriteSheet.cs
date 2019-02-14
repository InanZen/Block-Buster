using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Block_Buster
{
    public class SpriteSheet
    {
        UInt16 tileW;
        UInt16 tileH;
        UInt16 xCount;
        UInt16 yCount;
        UInt16 elCount;
        UInt16 paddingEnd;

        public Texture2D Texture;
        public UInt16 TileW
        {
            get { return tileW; }
            set
            {
                tileW = value;
                xCount = (UInt16)(Texture.Width / value);
                elCount = (UInt16)(xCount * yCount - paddingEnd);
            }
        }
        public UInt16 TileH
        {
            get { return tileH; }
            set
            {
                tileH = value;
                yCount = (UInt16)(Texture.Height / value);
                elCount = (UInt16)(xCount * yCount - paddingEnd);
            }
        }
        /// <summary>
        /// Number of empty tiles at the end of the sheet
        /// </summary>
        public UInt16 Padding
        {
            get { return paddingEnd; }
            set
            {
                paddingEnd = value;
                elCount = (UInt16)(xCount * yCount - paddingEnd);
            }
        }
        public UInt16 wTileCount { get { return xCount; } }
        public UInt16 hTileCount { get { return yCount; } }
        public UInt16 ElementCount { get { return elCount; } }
        public UInt16 FirstGID;

        public SpriteSheet(Texture2D texture, UInt16 spriteW, UInt16 spriteH, UInt16 firstGID = 1, UInt16 padding = 0)
        {
            Texture = texture;
            Texture.Name = texture.Name;
            FirstGID = firstGID;
            tileW = spriteW;
            tileH = spriteH;
            paddingEnd = padding;
            xCount = (UInt16)(texture.Width / tileW);
            yCount = (UInt16)(texture.Height / tileH);
            elCount = (UInt16)(xCount * yCount - paddingEnd);
        }
        public Rectangle GetSourceRect(UInt16 gID)
        {
            UInt16 setIndx = (UInt16)(gID - FirstGID);
            return new Rectangle((setIndx % xCount) * tileW, (setIndx / xCount) * tileH, tileW, tileH);
        }

    }
}
