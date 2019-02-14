using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace Block_Buster
{
    [Serializable]
    public struct Settings
    {
        public float Volume;
        public int DisplayMode;
        public bool Fullscreen;
        /// <summary>
        /// 0-mouse, 1-arrow keys, 2-wsad
        /// </summary>
        public byte Controller;
    }

    public class Main : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        public static DisplayMode[] displayModes;
        public static int currentDM;
        public static Settings settings;
        public static AudioManager Audio;
        public static Texture2D[] Backgrounds;
        public static int currentBG = -1;
        public static Cursor cursor;
        BasicEffect ef;
        Texture2D inanzenLogo;
        float transition = 0f;
        float keepLogo = 2f;
        /// <summary>
        /// 0 - fade in logo, 1 - keep logo / load title, 2 - fade out logo, 3 - done
        /// </summary>
        byte initState = 0;


        VertexBuffer fsVB;
        IndexBuffer fsIB;


        public Main()
        {
            graphics = new GraphicsDeviceManager(this);

            var dispModesList = GraphicsAdapter.DefaultAdapter.SupportedDisplayModes.ToList();
            

            DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            graphics.PreferredBackBufferWidth = 1024;
            if (dm.Width <= 1024)
            {
                graphics.PreferredBackBufferWidth = dm.Width;
                graphics.IsFullScreen = true;
                settings.Fullscreen = true;
            }
            graphics.PreferredBackBufferHeight = 768;
            if (dm.Height <= 768)
            {
                graphics.PreferredBackBufferHeight = dm.Height;
                graphics.IsFullScreen = true;
                settings.Fullscreen = true;
            }

            for (int i = dispModesList.Count - 1; i >= 0; i--) // remove invalid display modes
            {
                if (dispModesList[i].Height < 480)
                    dispModesList.RemoveAt(i);
            }
            displayModes = dispModesList.ToArray();
            for (int i = 0; i < displayModes.Length; i++) // find current
            {
                if (displayModes[i].Width == graphics.PreferredBackBufferWidth && displayModes[i].Height == graphics.PreferredBackBufferHeight)
                {
                    currentDM = i;
                    break;
                }
            }
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            graphics.SynchronizeWithVerticalRetrace = false;
        }

        public void SetDisplayMode(DisplayMode dm, bool fullscreen)
        {
            graphics.PreferredBackBufferWidth = dm.Width;
            graphics.PreferredBackBufferHeight = dm.Height;
            graphics.PreferredBackBufferFormat = dm.Format;
            graphics.IsFullScreen = fullscreen;
            graphics.ApplyChanges();
        }
        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            try
            {
                if (File.Exists("settings.dat"))
                {
                    using (FileStream fs = new FileStream("settings.dat", FileMode.Open, FileAccess.Read))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        settings = (Settings)bf.Deserialize(fs);

                        currentDM = settings.DisplayMode;
                        MediaPlayer.Volume = settings.Volume;
                        SoundEffect.MasterVolume = settings.Volume;
                        DisplayMode dm = displayModes[currentDM];
                        graphics.PreferredBackBufferWidth = dm.Width;
                        graphics.PreferredBackBufferHeight = dm.Height;
                        graphics.PreferredBackBufferFormat = dm.Format;
                        graphics.IsFullScreen = settings.Fullscreen;
                        graphics.ApplyChanges();
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Audio = new AudioManager(this);
            Components.Add(Audio);

            base.Initialize();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    using (FileStream fs = new FileStream("settings.dat", FileMode.Create, FileAccess.Write))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fs, settings);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
            base.Dispose(disposing);
        }
        protected override void LoadContent()
        {
            ef = new BasicEffect(GraphicsDevice);
            Backgrounds = new Texture2D[9];
            for (int i = 0; i < Backgrounds.Length; i++)
                Backgrounds[i] = Content.Load<Texture2D>(String.Format(@"Backgrounds\bg_{0}", i + 1));

            cursor = new Cursor(Content.Load<Texture2D>(@"Sprites\cursor"), 64, 64, 40, 100);
            inanzenLogo = Content.Load<Texture2D>(@"Sprites\inanzen_logo");


            float wPercent = (float)inanzenLogo.Width / graphics.PreferredBackBufferWidth;
            float hPercent = (float)inanzenLogo.Height / graphics.PreferredBackBufferHeight;
            if (wPercent > 1)
                wPercent = 1;
            
            VertexPositionTexture[] vertices =
            {
                new VertexPositionTexture(new Vector3(wPercent, -1 * hPercent, 0), new Vector2(1, 1)), // bottom right
                new VertexPositionTexture(new Vector3(-1 * wPercent, -1 * hPercent, 0), new Vector2(0, 1)), // bottom left
                new VertexPositionTexture(new Vector3(-1 * wPercent, hPercent, 0), new Vector2(0, 0)), // top left
                new VertexPositionTexture(new Vector3(wPercent, hPercent, 0), new Vector2(1, 0)) // top right
            };            
            fsVB = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration, vertices.Length, BufferUsage.None);
            fsVB.SetData<VertexPositionTexture>(vertices);

            ushort[] indices = { 0, 1, 2, 2, 3, 0 };
            fsIB = new IndexBuffer(GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.None);
            fsIB.SetData<ushort>(indices);



            base.LoadContent();
        }
        

        /// <summary>
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if (initState == 0)
            {
                transition += 0.5f * (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (transition >= 1)
                {
                    transition = 1;
                    initState = 1;
                    // start loading title
                    Title title = new Title(this);
                    title.Initialize();
                    title.Visible = false;
                    title.Enabled = false;
                    Components.Add(title);
                }
            }
            else if (initState == 1)
            {
                keepLogo -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (keepLogo <= 0)
                    initState = 2;
            }
            else if (initState == 2)
            {
                transition -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (transition <= 0)
                {
                    transition = 0;
                    initState = 3;
                    Title title = Components.First(c => c.GetType() == typeof(Title)) as Title;
                    title.Visible = true;
                    title.Enabled = true;
                }
            }
            cursor.Update(gameTime);            
            base.Update(gameTime);
        }

  
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if (initState == 3)
                ef.CurrentTechnique.Passes[0].Apply();
            else
            {
                GraphicsDevice.Clear(Color.Black);
                GraphicsDevice.SetVertexBuffer(fsVB);
                GraphicsDevice.Indices = fsIB;
                GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                ef.Alpha = transition;
                ef.TextureEnabled = true;
                ef.Texture = inanzenLogo;
                ef.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4, 0, 2);
            }
            base.Draw(gameTime);
        }
    }
}
