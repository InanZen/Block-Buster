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
using XNA_GUI_Controls;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace Block_Buster
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Title : Microsoft.Xna.Framework.DrawableGameComponent
    {
        Controller gui;
        SpriteFont font;
        SpriteFont fontTitle;
        SpriteBatch spriteBatch;
        Texture2D copyright;
        Texture2D spriteSheet;
        Texture2D selectBg;
        Texture2D keys;
        Control[] panels;
        float panelTrans = 0;
        bool transition;
        int panelIndex = 0;
        int panelOld = 0;
        Random rand = new Random();
        KeyboardState oldKeyState = Keyboard.GetState();

        List<Ball> Balls = new List<Ball>();
        ServerInfo[] Servers = new ServerInfo[0];
        int SelectedServer = -1;

        public Title(Game game)
            : base(game)
        {
            game.Window.ClientSizeChanged += OnWindowSize;
        }
        void OnWindowSize(object sender, EventArgs args)
        {
            SetUpGUI(panelIndex);
        }


        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();
        }
        protected override void LoadContent()
        {
            GraphicsDevice.DeviceReset += OnWindowSize;

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Game.Content.Load<SpriteFont>(@"Fonts\8bitMadness");
            fontTitle = Game.Content.Load<SpriteFont>(@"Fonts\Audiowide");

            copyright = Game.Content.Load<Texture2D>(@"Sprites\copyright");
            spriteSheet = Game.Content.Load<Texture2D>(@"Sprites\tiles");
            keys = Game.Content.Load<Texture2D>(@"Sprites\keys-arrow-wsad-mouse");
            

            gui = new Controller("controller", new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), font);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\golden_steampunk"), 128, 20);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\slider_handle"), 5, 10);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\left"), 32, 32);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\right"), 32, 32);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\checkbox"), 32, 32);
            gui.AddBackgroundStyle(Game.Content.Load<Texture2D>(@"Sprites\slider_bg"));
            selectBg = Game.Content.Load<Texture2D>(@"Sprites\input_underbox");
            gui.AddBackgroundStyle(selectBg); 
            SetUpGUI();

            for (int i = 0; i < 20; i++)
            {
                Balls.Add(new Ball() { 
                    Position = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()), 
                    Velocity = new Vector2(rand.Next(10, 20) / 100f, rand.Next(10, 20) / 100f),
                    Type = (byte)rand.Next(0,7),
                    Size = rand.Next(150, 300) / 100f
                });
            }
            Main.Audio.State = 1;
            base.LoadContent();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape) && !transition)
                ShowPanel(4);

            if (panelIndex == 6) // direct ip connect panel
            {
                var ipboxes = panels[6].FindChildren("joinip", false);
                for (int i = 0; i < ipboxes.Count; i++)
                {
                    InputBox input = ipboxes[i] as InputBox;
                    if (input.Focused && i < ipboxes.Count - 1)
                    {
                        if (keyState.IsKeyDown(Keys.Tab) && oldKeyState.IsKeyUp(Keys.Tab))
                        {
                            ipboxes[i + 1].Focused = true;
                            break;
                        }
                        if (input.Text.Length == 3)
                        {
                            int charCode = (int)input.Text[2];
                            if ((oldKeyState.IsKeyDown((Keys)charCode) && keyState.IsKeyUp((Keys)charCode)) || (oldKeyState.IsKeyDown((Keys)charCode + 48) && keyState.IsKeyUp((Keys)charCode + 48)))
                            {
                                ipboxes[i + 1].Focused = true;
                                break;
                            }
                        }
                    }
                }

            }

            float dT = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000f;
            if (transition)
            {
                panelTrans += dT * 1.5f;

                Control old = panels[panelOld];
                Control cur = panels[panelIndex];
                old.Position = new Rectangle((int)MathHelper.Lerp(0, -1 * GraphicsDevice.Viewport.Width, panelTrans), 0, old.Position.Width, old.Position.Height);
                cur.Position = new Rectangle((int)MathHelper.Lerp(GraphicsDevice.Viewport.Width, 0, panelTrans), 0, cur.Position.Width, cur.Position.Height);

                if (panelTrans >= 1)
                {
                    transition = false;
                    old.Enabled = false;
                }
            }
            foreach (Ball ball in Balls)
            {
                ball.Position.X = MathHelper.Clamp(ball.Position.X + ball.Velocity.X * dT, 0, 1);
                ball.Position.Y = MathHelper.Clamp(ball.Position.Y + ball.Velocity.Y * dT, 0, 1);
                if (ball.Position.X == 0 || ball.Position.X == 1f)
                    ball.Velocity.X *= -1;
                if (ball.Position.Y == 0 || ball.Position.Y == 1f)
                    ball.Velocity.Y *= -1;

                Vector2 ballScrn = new Vector2(ball.Position.X * GraphicsDevice.Viewport.Width, ball.Position.Y * GraphicsDevice.Viewport.Height);
                foreach (Control c in panels[panelIndex].Children)
                {
                    Button b = c as Button;
                    if (b != null)
                    {
                        if (ballScrn.X >= b.Position.X && ballScrn.X <= b.Position.Right && ballScrn.Y >= b.Position.Y && ballScrn.Y <= b.Position.Bottom)
                        {
                            int xdiff = (int)ballScrn.X - b.Position.X;
                            int ydiff = (int)ballScrn.Y - b.Position.Y;
                            if (ball.Velocity.Y < 0)
                                ydiff = b.Position.Bottom - (int)ballScrn.Y;
                            if (ball.Velocity.X < 0)
                                xdiff = b.Position.Right - (int)ballScrn.X;

                            if (xdiff < ydiff)
                                ball.Velocity.X *= -1;
                            else
                                ball.Velocity.Y *= -1;
                        }

                    }
                }
            }

            //ball -ball collision
            float radius = 4.2f / GraphicsDevice.Viewport.Width;
            for (int i = Balls.Count - 1; i >= 0; i--)
            {
                Ball ball = Balls[i];
                for (int j = i- 1; j >= 0; j--)
                {
                    Ball otherBall = Balls[j];
                    if (Vector2.Distance(ball.Position, otherBall.Position) <= radius * ball.Size + radius * otherBall.Size)
                    {
                        Vector2 dPos = ball.Position - otherBall.Position; ;
                        double angle = Math.Atan2(dPos.Y, dPos.X);

                        double b1Dir = Math.Atan2(ball.Velocity.Y, ball.Velocity.X);
                        double b2Dir = Math.Atan2(otherBall.Velocity.Y, otherBall.Velocity.X);

                        float b1Magnitude = ball.Velocity.Length();
                        float b2Magnitude = otherBall.Velocity.Length();

                        Vector2 b1NewVelocity = new Vector2((float)(b1Magnitude * Math.Cos(b1Dir - angle)), (float)(b1Magnitude * Math.Sin(b1Dir - angle)));
                        Vector2 b2NewVelocity = new Vector2((float)(b2Magnitude * Math.Cos(b2Dir - angle)), (float)(b2Magnitude * Math.Sin(b2Dir - angle)));

                        double b1FinalVelocityX = ((ball.Size - otherBall.Size) * b1NewVelocity.X + (otherBall.Size + otherBall.Size) * b2NewVelocity.X) / (ball.Size + otherBall.Size);
                        double b2FinalVelocityX = ((ball.Size + ball.Size) * b1NewVelocity.X + (otherBall.Size - ball.Size) * b2NewVelocity.X) / (ball.Size + otherBall.Size);
                     
                        Vector2 b1Final = new Vector2((float)(Math.Cos(angle) * b1FinalVelocityX + Math.Cos(angle + Math.PI / 2) * b1NewVelocity.Y), (float)(Math.Sin(angle) * b1FinalVelocityX + Math.Sin(angle + Math.PI / 2) * b1NewVelocity.Y));
                        Vector2 b2Final = new Vector2((float)(Math.Cos(angle) * b2FinalVelocityX + Math.Cos(angle + Math.PI / 2) * b2NewVelocity.Y), (float)(Math.Sin(angle) * b2FinalVelocityX + Math.Sin(angle + Math.PI / 2) * b2NewVelocity.Y));

                        ball.Velocity = b1Final;
                        otherBall.Velocity = b2Final;
                        ball.Position += ball.Velocity * dT;
                        otherBall.Position += otherBall.Velocity * dT;
                    }
                }                
            }

            gui.Update(gameTime);

            oldKeyState = keyState;

            base.Update(gameTime);
        }
        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.MintCream); 
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            gui.Draw(spriteBatch);
            foreach (Ball ball in Balls)
            {
                Vector2 ballPos = new Vector2(GraphicsDevice.Viewport.Width * ball.Position.X, GraphicsDevice.Viewport.Height * ball.Position.Y);
                spriteBatch.Draw(spriteSheet, ballPos, ball.Source, Color.White, 0f, new Vector2(4, 4), ball.Size, SpriteEffects.None, 0);
            }
            Main.cursor.Draw(spriteBatch);
            spriteBatch.End();
            base.Draw(gameTime);
        }

        void SetUpGUI(int activePanel = 0)
        {
            gui.ClearChildren();
            int windowW = GraphicsDevice.Viewport.Width;
            int windowH = GraphicsDevice.Viewport.Height;
            int w2 = GraphicsDevice.Viewport.Width / 2;
            int h2 = GraphicsDevice.Viewport.Height / 2;
            int padd = 12;
            int h = 48;
            int pos1 = h2 - padd / 2 - padd - 2 * h;
            int pos2 = h2 - padd / 2 - h;
            int pos3 = h2 + padd / 2;
            int pos4 = h2 + padd / 2 + padd + h;
            int pos5 = h2 + padd / 2 + (padd + h) * 2;
            // title
            TextArea text = gui.AddTextArea("title", "Block Buster", new Rectangle(0, 0, windowW, pos1));
            text.Color = Color.Black;
            text.Font = fontTitle;
            Sprite copySign = gui.AddSprite("copyright", new Rectangle(w2 - 96, windowH - 40, 16, 16), copyright);
            text = gui.AddTextArea("copytext", "2013 Peter Gruden", new Rectangle(w2 - 64, windowH - 64, 400, 64));
            text.Alignment = TextAlignment.Left;

            panels = new Control[12];
            // menu
            panels[0] = new Control("panelMain", new Rectangle(0, 0, windowW, windowH));
            var button = gui.AddButton("play", "Play", new Rectangle(w2 - 110, pos1, 220, h), OnClick, 0, panels[0]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("about", "About", new Rectangle(w2 - 110, pos2, 220, h), OnClick, 0, panels[0]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("settings", "Settings", new Rectangle(w2 - 110, pos3, 220, h), OnClick, 0, panels[0]);
            button.TextColor = Color.NavajoWhite;
            /*button = gui.AddButton("credits", "Credits", new Rectangle(w2 - 110, pos4, 220, h), OnClick, 0, panels[0]);
            button.TextColor = Color.NavajoWhite;*/
            button = gui.AddButton("exit", "Exit", new Rectangle(w2 - 110, pos5, 220, 48), OnClick, 0, panels[0]);
            button.TextColor = Color.NavajoWhite;

            // Play menu
            panels[1] = new Control("panelPlay", new Rectangle(0, 0, windowW, windowH));
            button = gui.AddButton("single", "Single Player", new Rectangle(w2 - 110, pos1, 220, h), OnClick, 0, panels[1]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("localcoop", "Local Co-op", new Rectangle(w2 - 110, pos2, 220, h), OnClick, 0, panels[1]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("internet", "Internet", new Rectangle(w2 - 110, pos3, 220, h), OnClick, 0, panels[1]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("main", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[1]);
            button.TextColor = Color.NavajoWhite; 

            // settings
            panels[2] = new Control("panelSettings", new Rectangle(0, 0, windowW, windowH));
            text = gui.AddTextArea("volumeText", "Music volume:", new Rectangle(0, pos1, w2 - 64, 32), panels[2]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            var slider = gui.AddSlider("volume", new Rectangle(w2, pos1, w2 - 128, 32), 0, 1, panels[2]);
            slider.Value = MediaPlayer.Volume;
            slider.ValueChanged += OnVolumeChange;

            text = gui.AddTextArea("resText", "Resolution:", new Rectangle(0, pos2, w2 - 64, 32), panels[2]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            button = gui.AddButton("resChange", null, new Rectangle(w2, pos2, 32, 32), OnClick, 2, panels[2]);
            button.Tag = -1;
            button = gui.AddButton("resChange", null, new Rectangle(w2 * 2 - 64, pos2, 32, 32), OnClick, 3, panels[2]);
            button.Tag = 1;
            DisplayMode dm = Main.displayModes[Main.currentDM];
            text = gui.AddTextArea("resVal", String.Format("{0}x{1}", dm.Width, dm.Height), new Rectangle(w2 + 64, pos2, w2 - 128, 32), panels[2]);
            text.Color = Color.Gray;
            text.Tag = Main.currentDM;
            text = gui.AddTextArea("fsText", "Fullscreen:", new Rectangle(0, pos3, w2 - 64, 32), panels[2]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            ToggleButton toggle = gui.AddToggleButton("fs", null, new Rectangle(w2, pos3, 32, 32), OnClick, 4, panels[2]);
            if (Main.settings.Fullscreen)
                toggle.State = 1;

            text = gui.AddTextArea("controlTxt", "Control scheme:", new Rectangle(0, pos4, w2 - 64, 32), panels[2]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            Sprite sprite = gui.AddSprite("control", new Rectangle(w2, pos4, 64, h), keys, panels[2]);
            sprite.Source = new Rectangle(0, 128, 64, 64);
            sprite.Tag = 0;
            sprite.OnClick += OnClick;
            if (Main.settings.Controller == 0)
                sprite.Color = Color.DarkGoldenrod;
            sprite = gui.AddSprite("control", new Rectangle(w2 + 72, pos4, 96, h), keys, panels[2]);
            sprite.Source = new Rectangle(0, 0, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 1;
            if (Main.settings.Controller == 1)
                sprite.Color = Color.DarkGoldenrod;
            sprite = gui.AddSprite("control", new Rectangle(w2 + 176, pos4, 96, h), keys, panels[2]);
            sprite.Source = new Rectangle(0, 64, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 2;
            if (Main.settings.Controller == 2)
                sprite.Color = Color.DarkGoldenrod;

            button = gui.AddButton("savesettings", "Done", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[2]);
            button.TextColor = Color.NavajoWhite;  

            // Server browser
            panels[3] = new Control("panelServerBrowser", new Rectangle(0, 0, windowW, windowH));
            int broW = w2 * 2 - 128;
            if (broW > 800)
                broW = 800;
            Rectangle browserRect = new Rectangle((w2 * 2 - broW) / 2, pos1 - 32, broW, h2 * 2 - pos1 - 32);
            int elW = browserRect.Width / 4;
            button = gui.AddButton("internet", "Back", new Rectangle(browserRect.X, browserRect.Bottom - h, elW - padd / 2, h), OnClick, 0, panels[3]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("refresh", "Refresh", new Rectangle(browserRect.X + elW + padd / 2, browserRect.Bottom - h, elW - padd / 2, h), OnClick, 0, panels[3]);
            button.TextColor = Color.NavajoWhite;  
            button = gui.AddButton("browserPage", null, new Rectangle(browserRect.X + 3 * elW - elW / 2 - 32 - padd, browserRect.Bottom - h / 2 - 16, 32, 32), OnClick, 2, panels[3]);
            button.Tag = -1;
            button = gui.AddButton("browserPage", null, new Rectangle(browserRect.X + 3 * elW + elW / 2 + padd, browserRect.Bottom - h / 2 - 16, 32, 32), OnClick, 3, panels[3]);
            button.Tag = 1; 
            text = gui.AddTextArea("pageVal", "Page: 1 / 1", new Rectangle(browserRect.X + 3 * elW - elW / 2, browserRect.Bottom - h, elW, h), panels[3]);
            text.Tag = 0;
            int perPage = (browserRect.Height - h) / 32 - 1;
            for (int i = 0; i < perPage; i++)
            {
                text = gui.AddTextArea("ip", "", new Rectangle(browserRect.X, browserRect.Y + 32 + i * 32, browserRect.Width, 32), panels[3]);
                text.OnClick += OnClick;
            }
            sprite = gui.AddSprite("bg", text.Position, selectBg, panels[3]);
            sprite.Enabled = false;
            button = gui.AddButton("connectSB", "Connect", new Rectangle(0, 0, 128, 32), OnClick, 0, panels[3]);
            button.TextColor = Color.NavajoWhite;
            button.Enabled = false;

            gui.AddTextArea("status", "", new Rectangle(browserRect.X, browserRect.Y, browserRect.Width, 32), panels[3]);
            panels[3].Tag = browserRect;

            // Quit confirm
            panels[4] = new Control("panelQuit", new Rectangle(0, 0, windowW, windowH));
            text = gui.AddTextArea("quitmsg", "Are you sure you want to quit?", new Rectangle(64, pos1, w2 * 2 - 128, h * 3), panels[4]);
            button = gui.AddButton("quit", "Yes", new Rectangle(w2 - 128 - 16, pos4, 128, h), OnClick, 0, panels[4]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("main", "No", new Rectangle(w2 + 16, pos4, 128, h), OnClick, 0, panels[4]);
            button.TextColor = Color.NavajoWhite;
            
            // join game panel
            panels[5] = new Control("panelJoin", new Rectangle(0, 0, windowW, windowH));
            button = gui.AddButton("serverBrowser", "Server browser", new Rectangle(w2 - 110, pos1, 220, h), OnClick, 0, panels[5]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("multi", "Host a game", new Rectangle(w2 - 110, pos2, 220, h), OnClick, 0, panels[5]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("ipConnect", "IP connect", new Rectangle(w2 - 110, pos3, 220, h), OnClick, 0, panels[5]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("play", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[5]);
            button.TextColor = Color.NavajoWhite;  

            // direct connect panel
            panels[6] = new Control("panelConnect", new Rectangle(0, 0, windowW, windowH));
            text = gui.AddTextArea("iptxt", "IP:", new Rectangle(w2 - 64 * 3 - padd * 2 - padd / 2, pos1, 64, 32), panels[6]);
            text.Alignment = TextAlignment.Right;
            gui.AddTextArea("iptxt", ".", new Rectangle(w2 - 64 - padd - padd / 2, pos1, padd, h), panels[6]);
            gui.AddTextArea("iptxt", ".", new Rectangle(w2 - padd / 2, pos1, padd, h), panels[6]);
            gui.AddTextArea("iptxt", ".", new Rectangle(w2 + 64 + padd / 2, pos1, padd, h), panels[6]);
            var input = gui.AddInput("joinip", new Rectangle(w2 - 64 * 2 - padd - padd / 2, pos1, 64, 32), "", 3, 1, panels[6]);
            input.AcceptLetters = false;
            input.Tag = 0;
            input = gui.AddInput("joinip", new Rectangle(w2 - 64 - padd / 2, pos1, 64, 32), "", 3, 1, panels[6]);
            input.AcceptLetters = false;
            input.Tag = 1;
            input = gui.AddInput("joinip", new Rectangle(w2 + padd / 2, pos1, 64, 32), "", 3, 1, panels[6]);
            input.AcceptLetters = false;
            input.Tag = 2;
            input = gui.AddInput("joinip", new Rectangle(w2 + 64 + padd + padd / 2, pos1, 64, 32), "", 3, 1, panels[6]);
            input.AcceptLetters = false;
            input.Tag = 3;
            button = gui.AddButton("connectIP", "Connect", new Rectangle(w2 - 110, pos2, 220, h), OnClick, 0, panels[6]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("internet", "Back", new Rectangle(w2 - 110, pos4, 220, h), OnClick, 0, panels[6]);
            button.TextColor = Color.NavajoWhite;

            // credits panel
            panels[7] = new Control("panelCredits", new Rectangle(0, 0, windowW, windowH));
            text = gui.AddTextArea("developer", "Developer:", new Rectangle(0, pos1, w2 - 32, 32), panels[7]);
            text.Alignment = TextAlignment.Right;
            text = gui.AddTextArea("developer", "InanZen", new Rectangle(w2 - 32, pos1, 128, 32), panels[7]);
            text.Color = Color.DarkBlue;
            text.OnMouseOver += MouseLink;
            text.OnMouseLeave += MouseNormal;
            text.OnClick += OnClick;
            text = gui.AddTextArea("music", "Music:", new Rectangle(0, pos2, w2 - 32, 32), panels[7]);
            text.Alignment = TextAlignment.Right;
            text = gui.AddTextArea("music", "3uhox", new Rectangle(w2 - 32, pos2, 128, 32), panels[7]);
            text.Color = Color.DarkSlateBlue;
            text.OnMouseOver += MouseLink;
            text.OnMouseLeave += MouseNormal;
            text.OnClick += OnClick;
            text = gui.AddTextArea("art", "Art:", new Rectangle(0, pos3, w2 - 32, 32), panels[7]);
            text.Alignment = TextAlignment.Right;
            text = gui.AddTextArea("art", "Buch", new Rectangle(w2 - 32, pos3, 128, 32), panels[7]);
            text.Color = Color.DarkSlateBlue;
            text.OnMouseOver += MouseLink;
            text.OnMouseLeave += MouseNormal;
            text.OnClick += OnClick;
            button = gui.AddButton("about", "Back", new Rectangle(w2 - 110, pos4, 220, h), OnClick, 0, panels[7]);
            button.TextColor = Color.NavajoWhite;

            // local co-op panel
            panels[8] = new Control("panelCoop", new Rectangle(0, 0, windowW, windowH));
            text = gui.AddTextArea("controlTxt", "Player 1 controller:", new Rectangle(0, pos1, w2 - 64, 32), panels[8]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            sprite = gui.AddSprite("control1", new Rectangle(w2, pos1, 64, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 128, 64, 64);
            sprite.Tag = 0;
            sprite.OnClick += OnClick;
            if (Main.settings.Controller == 0)
                sprite.Color = Color.DarkGoldenrod;
            sprite = gui.AddSprite("control1", new Rectangle(w2 + 72, pos1, 96, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 0, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 1;
            if (Main.settings.Controller == 1)
                sprite.Color = Color.DarkGoldenrod;
            sprite = gui.AddSprite("control1", new Rectangle(w2 + 176, pos1, 96, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 64, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 2;
            if (Main.settings.Controller == 2)
                sprite.Color = Color.DarkGoldenrod;
            text = gui.AddTextArea("controlTxt", "Player 2 controller:", new Rectangle(0, pos2, w2 - 64, 32), panels[8]);
            text.Alignment = TextAlignment.Right;
            text.Color = Color.DarkBlue;
            sprite = gui.AddSprite("control2", new Rectangle(w2, pos2, 64, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 128, 64, 64);
            sprite.Tag = 0;
            sprite.OnClick += OnClick;
            sprite = gui.AddSprite("control2", new Rectangle(w2 + 72, pos2, 96, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 0, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 1;
            sprite = gui.AddSprite("control2", new Rectangle(w2 + 176, pos2, 96, h), keys, panels[8]);
            sprite.Source = new Rectangle(0, 64, 96, 64);
            sprite.OnClick += OnClick;
            sprite.Tag = 2;
            button = gui.AddButton("startCoop", "Start", new Rectangle(w2 - 110, pos3, 220, h), OnClick, 0, panels[8]);
            button.TextColor = Color.NavajoWhite;
            button.Disabled = true;
            button = gui.AddButton("play", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[8]);
            button.TextColor = Color.NavajoWhite;

            // about panel
            panels[9] = new Control("panelAbout", new Rectangle(0, 0, windowW, windowH));
            button = gui.AddButton("howtoplay", "How to play", new Rectangle(w2 - 110, pos1, 220, h), OnClick, 0, panels[9]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("upgrades", "Upgrades", new Rectangle(w2 - 110, pos2, 220, h), OnClick, 0, panels[9]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("credits", "Credits", new Rectangle(w2 - 110, pos3, 220, h), OnClick, 0, panels[9]);
            button.TextColor = Color.NavajoWhite;
            button = gui.AddButton("main", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[9]);
            button.TextColor = Color.NavajoWhite;

            // how to play panel
            panels[10] = new Control("panelHowTo", new Rectangle(0, 0, windowW, windowH));
            int w_ = (w2 > 400) ? 800 : (w2 * 2) - 64;
            Rectangle inner = new Rectangle(w2 - w_ / 2, pos1, w_, h * 4);
            gui.AddTextArea("abouttxt", "The goal of the game is to break all the bricks in each level of the game and try to score the most points total.", new Rectangle(inner.X, pos1, w_, h), panels[10]);
            gui.AddTextArea("abouttxt", "You break the bricks by bouncing a ball into them, but be careful not to drop the ball or it will cost you a life (You start with 3 lives).", new Rectangle(inner.X, pos2, w_, h), panels[10]);
            gui.AddTextArea("abouttxt", "Some bricks will drop Upgrades when broken. Collect them for various effects, both good and bad.", new Rectangle(inner.X, pos3, w_, h), panels[10]);
            button = gui.AddButton("about", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[10]);
            button.TextColor = Color.NavajoWhite;

            // upgrades panel
            panels[11] = new Control("panelUpgrades", new Rectangle(0, 0, windowW, windowH));
            int x1 = w2 - 250;
            int x2 = w2 + 125;

            sprite = gui.AddSprite("upIco", new Rectangle(x1 - 32, pos1, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(0, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Smaller paddle", new Rectangle(x1 + 8, pos1, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;
            sprite = gui.AddSprite("upIco", new Rectangle(x2 - 32, pos1, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(16, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Bigger paddle", new Rectangle(x2 + 8, pos1, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;

            sprite = gui.AddSprite("upIco", new Rectangle(x1 - 32, pos1 + 40, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(32, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "+1 Life", new Rectangle(x1 + 8, pos1 + 40, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;
            sprite = gui.AddSprite("upIco", new Rectangle(x2 - 32, pos1 + 40, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(48, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "-1 Life", new Rectangle(x2 + 8, pos1 + 40, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;

            sprite = gui.AddSprite("upIco", new Rectangle(x1 - 32, pos1 + 80, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(64, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Faster balls", new Rectangle(x1 + 8, pos1 + 80, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;
            sprite = gui.AddSprite("upIco", new Rectangle(x2 - 32, pos1 + 80, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(80, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Slower balls", new Rectangle(x2 + 8, pos1 + 80, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;

            sprite = gui.AddSprite("upIco", new Rectangle(x1 - 32, pos1 + 120, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(96, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Smaller balls", new Rectangle(x1 + 8, pos1 + 120, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;
            sprite = gui.AddSprite("upIco", new Rectangle(x2 - 32, pos1 + 120, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(112, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "Larger balls", new Rectangle(x2 + 8, pos1 + 120, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;

            sprite = gui.AddSprite("upIco", new Rectangle(x1 - 32, pos1 + 160, 32, 32), spriteSheet, panels[11]);
            sprite.Source = new Rectangle(128, 192, 16, 16);
            text = gui.AddTextArea("upTxt", "+1 ball", new Rectangle(x1 + 8, pos1 + 160, 200, 32), panels[11]);
            text.Alignment = TextAlignment.Left;

            button = gui.AddButton("about", "Back", new Rectangle(w2 - 110, pos5, 220, h), OnClick, 0, panels[11]);
            button.TextColor = Color.NavajoWhite;


            for (int i = 0; i < panels.Length; i++)
            {
                if (i != activePanel)
                    panels[i].Enabled = false;
                gui.AddChild(panels[i]);            
            }
             
        }
        void OnClick(object sender, EventArgs args)
        {
            Control c = sender as Control;
            if (c.Name == "quit")
                Game.Exit();
            else if (c.Name == "exit")
                ShowPanel(4);
            else if (c.Name == "play")
                ShowPanel(1);
            else if (c.Name == "main")
                ShowPanel(0);
            else if (c.Name == "internet")
                ShowPanel(5);
            else if (c.Name == "serverBrowser")
                ShowPanel(3);
            else if (c.Name == "ipConnect")
                ShowPanel(6);
            else if (c.Name == "connectIP")
                DirectJoin();
            else if (c.Name == "connectSB")
                SBJoin();
            else if (c.Name == "localcoop")
                ShowPanel(8);
            else if (c.Name == "settings")
                ShowPanel(2);
            else if (c.Name == "credits")
                ShowPanel(7);
            else if (c.Name == "about")
                ShowPanel(9);
            else if (c.Name == "howtoplay")
                ShowPanel(10);
            else if (c.Name == "upgrades")
                ShowPanel(11);
            else if (c.Name == "resChange")
            {
                TextArea restxt = c.Parent.FindChild("resVal", false) as TextArea;
                int mod = (int)c.Tag;
                int dmID = (int)restxt.Tag;
                dmID += mod;
                if (dmID < 0)
                    dmID = Main.displayModes.Length - 1;
                else if (dmID >= Main.displayModes.Length)
                    dmID = 0;
                DisplayMode dm = Main.displayModes[dmID];
                restxt.Text = String.Format("{0}x{1}", dm.Width, dm.Height);
                restxt.Tag = dmID;
            }
            else if (c.Name == "control" || c.Name == "control1" || c.Name == "control2")
            {
                int id = (int)c.Tag;
                var controllers = c.Parent.FindChildren(c.Name, false);
                foreach (Control control in controllers)
                {
                    control.Color = Color.White;
                }
                c.Color = Color.DarkGoldenrod;
                if (c.Parent == panels[2])
                    Main.settings.Controller = (byte)id;
                else if (c.Name == "control2")
                {
                    Button b = c.Parent.FindChild("startCoop", false) as Button;
                    b.Disabled = false;
                }
            }
            else if (c.Name == "savesettings")
            {
                TextArea restxt = c.Parent.FindChild("resVal", false) as TextArea;
                int dmID = (int)restxt.Tag;
                DisplayMode dm = Main.displayModes[dmID];
                ToggleButton fsB = c.Parent.FindChild("fs", false) as ToggleButton;
                bool fs = false;
                if (fsB.State == 1 || fsB.State == 3)
                    fs = true;
                Main main = Game as Main;
                if (main != null && (Main.currentDM != dmID || Main.settings.Fullscreen != fs))
                {
                    Main.currentDM = dmID;
                    Main.settings.DisplayMode = dmID;
                    Main.settings.Fullscreen = fs;
                    main.SetDisplayMode(dm, fs);
                }
                ShowPanel(0);
            }
            else if (c.Name == "single")
            {
                Play p = new Play(Game);
                p.Initialize();
                p.NewGame();
                Game.Components.Add(p);
                Game.Components.Remove(this);
                this.Dispose(true);
            }
            else if (c.Name == "multi")
            {
                Play p = new Play(Game);
                p.Initialize();
                p.HostGame();
                Game.Components.Add(p);
                Game.Components.Remove(this);
                this.Dispose(true);
            }
            else if (c.Name == "startCoop")
            {
                var controls = c.Parent.FindChildren("control1", false);
                int c1 = 0;
                foreach (Control ch in controls)
                    if (ch.Color == Color.DarkGoldenrod)
                    {
                        c1 = (int)ch.Tag;
                        break;
                    }
                controls = c.Parent.FindChildren("control2", false);
                int c2 = 0;
                foreach (Control ch in controls)
                    if (ch.Color == Color.DarkGoldenrod)
                    {
                        c2 = (int)ch.Tag;
                        break;
                    }

                Play p = new Play(Game);
                p.Initialize();
                p.NewLocalCoop((byte)c1, (byte)c2);
                Game.Components.Add(p);
                Game.Components.Remove(this);
                this.Dispose(true);
            }

            else if (c.Name == "refresh")
            {
                TextArea status = c.Parent.FindChild("status", false) as TextArea;
                if (status != null)
                {
                    status.Text = "Refreshing ...";
                }
                try
                {
                    TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                    client.BeginConnect("wv.si", 5601, RefreshHandler, client);
                }
                catch (Exception ex)
                {
                    status.Text = ex.ToString();
                }
            }
            else if (c.Name == "browserPage")
            {
                int mod = (int)c.Tag;
                TextArea pageTxt = panels[3].FindChild("pageVal", false) as TextArea;
                int page = (int)pageTxt.Tag;
                page += mod;
                if (page >= 0)
                {
                    pageTxt.Tag = page;
                    UpdateServerList();
                }
            }
            else if (c.Name == "ip")
            {
                TextArea t = c as TextArea;
                if (t.Text != "")
                {
                    SelectedServer = (int)t.Tag;
                    var s = c.Parent.FindChild("bg", false);
                    s.Position = c.Position;
                    s.Enabled = true;
                    var b = c.Parent.FindChild("connectSB", false);
                    b.Position = new Rectangle(c.Position.Right - b.Position.Width, c.Position.Y, b.Position.Width, b.Position.Height);
                    b.Enabled = true;
                }
            }

            else if (c.Name == "developer")
                System.Diagnostics.Process.Start("http://inanzen.eu/");
            else if (c.Name == "music")
                System.Diagnostics.Process.Start("http://opengameart.org/users/3uhox");
            else if (c.Name == "art")
                System.Diagnostics.Process.Start("http://opengameart.org/users/buch");

        }
        void MouseLink(object sender, EventArgs args)
        {
            Main.cursor.Type = 1;
        }
        void MouseNormal(object sender, EventArgs args)
        {
            Main.cursor.Type = 0;
        }
        void DirectJoin()
        {
            var controls = panels[6].FindChildren("joinip", false);
            int[] ipParts = new int[4];
            foreach (Control c in controls)
            {
                InputBox i = c as InputBox;
                int part = (int)i.Tag;
                int.TryParse(i.Text, out ipParts[part]);
            }
            string ip = ipParts[0].ToString();
            for (int i = 1; i < ipParts.Length; i++)
                ip = String.Format("{0}.{1}", ip, ipParts[i]);

            Play p = new Play(Game);
            p.Initialize();
            p.JoinGame(ip);
            Game.Components.Add(p);
            Game.Components.Remove(this);
            this.Dispose(true);
        }
        void SBJoin()
        {
            if (SelectedServer == -1)
                return;
            ServerInfo si = Servers[SelectedServer];
            Play p = new Play(Game);
            p.Initialize();
            p.JoinGame(si.ipendpoint.Address.ToString());
            Game.Components.Add(p);
            Game.Components.Remove(this);
            this.Dispose(true);
        }
        void RefreshHandler(IAsyncResult result)
        {
            TcpClient client = result.AsyncState as TcpClient;
            TextArea status = panels[3].FindChild("status", false) as TextArea;
            try
            {
                client.EndConnect(result);
                NetworkStream stream = client.GetStream();
                // send request
                stream.Write(new byte[] { 1 }, 0, 1);

                // read response
                byte[] recData = new byte[256];
                Int32 bytes = stream.Read(recData, 0, recData.Length);
                Console.WriteLine("received {0} bytes of data from {1}", bytes, client.Client.RemoteEndPoint);

                if (bytes >= 8)
                {
                    long num = BitConverter.ToInt64(recData, 0);
                    Console.WriteLine("received info about {0} servers", num);
                    if (status != null)
                    {
                        status.Enabled = false;
                        status.Text = String.Format("Pinging {0} servers", num);
                        status.Enabled = true;
                    }
                    ServerInfo[] ips = new ServerInfo[num];
                    Servers = new ServerInfo[0];
                    SelectedServer = -1;
                    UpdateServerList();
                    int index = 8;
                    for (int i = 0; i < num; i++)
                    {
                        try
                        {
                            int strlen = recData[index];
                            index += 4;
                            string ipStr = Encoding.UTF8.GetString(recData, index, strlen);
                            index += strlen;

                            Console.WriteLine(" -> server: {0}", ipStr);

                            UdpClient c = new UdpClient();
                            IPAddress ip;
                            if (!IPAddress.TryParse(ipStr, out ip))
                                ip = Dns.GetHostAddresses(ipStr)[0];

                            IPEndPoint eRemote = new IPEndPoint(ip, 15000);
                            ips[i] = new ServerInfo() { ipendpoint = eRemote, client = c, pingSent = DateTime.Now };

                            c.Connect(eRemote);
                            c.BeginReceive(OnPingBack, ips[i]);
                            c.Send(new byte[] { 11 }, 1);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
            }
            catch (SocketException ex)
            {
                if (status != null)
                {
                    status.Enabled = false;
                    status.Text = "Could not connect to master server";
                    status.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }            
            finally
            {
                client.Close();
            }
        }  
        void OnPingBack(IAsyncResult result)
        {
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                ServerInfo si = result.AsyncState as ServerInfo;
                UdpClient c = si.client;
                byte[] data = c.EndReceive(result, ref RemoteIpEndPoint);
                if (data.Length == 3 && data[0] == 11 && data[2] == 1) // server info pkg and free slots
                {
                    var time = DateTime.Now - si.pingSent;
                    si.ping = (int)time.TotalMilliseconds;
                    si.gameMode = data[1];
                    AppendToServerList(si);
                }
                c.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        void AppendToServerList(ServerInfo si)
        {
            int serverIndex = 0;
            lock (Servers)
            {
                serverIndex = Servers.Length;
                ServerInfo[] servers = new ServerInfo[serverIndex + 1];

                for (int i = 0; i < serverIndex; i++)
                    servers[i] = Servers[i];
                servers[serverIndex] = si;
                Servers = servers;
            }
            UpdateServerList();
        }
        void UpdateServerList()
        {
            TextArea status = panels[3].FindChild("status", false) as TextArea;
            Sprite bg = panels[3].FindChild("bg", false) as Sprite;
            bg.Enabled = false;
            var connButton = panels[3].FindChild("connectSB", false);
            connButton.Enabled = false;

            TextArea pageTxt = panels[3].FindChild("pageVal", false) as TextArea;
            int page = (int)pageTxt.Tag;
            
            Rectangle innerRect = (Rectangle)panels[3].Tag;
            var children = panels[3].FindChildren("ip", false);
            int numPerPage = children.Count;
            for (int i = 0; i < numPerPage; i++)
            {
                int sID = page * numPerPage + i;
                TextArea txt = children[i] as TextArea;
                if (sID < Servers.Length)
                {
                    txt.Text = String.Format("{0} ping:{1}", Servers[sID].ipendpoint.Address, Servers[sID].ping);
                    txt.Tag = sID;
                    if (SelectedServer == sID)
                    {
                        bg.Position = txt.Position;
                        bg.Enabled = true;
                        connButton.Position = new Rectangle(txt.Position.Right, txt.Position.Y, connButton.Position.Width, connButton.Position.Height);
                        connButton.Enabled = true;
                    }
                }
                else
                {
                    txt.Text = "";
                    txt.Tag = -1;
                }
            }
            status.Text = String.Format("Servers: {0}", Servers.Length);
            pageTxt.Text = String.Format("Page: {0} / {1}", page + 1, Servers.Length / numPerPage + 1);

        }
        void OnVolumeChange(object sender, EventArgs args)
        {
            Slider s = sender as Slider;
            MediaPlayer.Volume = s.Value;
            SoundEffect.MasterVolume = s.Value;
            Main.settings.Volume = s.Value;
        }

        void ShowPanel(int id)
        {
            if (panelIndex == id)
                return;
            transition = true;
            panelTrans = 0;
            panelOld = panelIndex;
            panelIndex = id;
            panels[id].Enabled = true;
            panels[id].Position = new Rectangle(GraphicsDevice.Viewport.Width, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        }
    }

    class ServerInfo
    {
        public IPEndPoint ipendpoint;
        public UdpClient client;
        public int ping;
        public bool full;
        public DateTime pingSent;
        public byte gameMode;
    }
}
