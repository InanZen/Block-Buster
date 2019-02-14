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

namespace Block_Buster
{
    public class Play : Microsoft.Xna.Framework.DrawableGameComponent
    {
        public enum PlayState
        {
            None = 0,
            Loading = 1,
            Connecting = 2,
            Pause = 3,
            LevelComplete = 4,
            GameOver = 5,
            Playing = 8
        }
        SpriteBatch spriteBatch;
        SpriteSheet spriteSheet;
        MouseState oldMouseState;
        KeyboardState oldKeyState;
        Vector2 center;

        public Level level;
        int spaceW;
        int offsetX;
        int spaceH;
        int offsetY;
        int tileW;
        int margin;
        int tileH;
        int marginH;

        public Paddle[] Paddles = new Paddle[2];
        public Ball[] Balls = new Ball[0];
        public List<Drop> Drops = new List<Drop>();
        byte lives;
        public byte Lives
        {
            get { return lives; }
            set
            {
                lives = value;
                if (server != null)
                    server.SendLifeInfo();
                if (lives == 0)                
                    State = PlayState.GameOver;                
            }
        }
        public int score;
        PlayState state;
        PlayState oldState;
        public PlayState State
        {
            get { return state; }
            set
            {
                if (value != state)
                {
                    if (value == PlayState.Pause)
                    {
                        var pause = gui.FindChild("pause", false);
                        if (pause == null)
                        {
                            int w2 = GraphicsDevice.Viewport.Width / 2;
                            int h2 = GraphicsDevice.Viewport.Height / 2;
                            var text = gui.AddTextArea("pause", "Paused", new Rectangle(w2 - 100 + 2, h2 + 2, 200, 32));
                            text = gui.AddTextArea("pause", "Paused", new Rectangle(w2 - 100, h2, 200, 32), text);
                            text.Color = Color.NavajoWhite;
                            if (client == null)
                            {
                                var button = gui.AddButton("restart", "Restart", new Rectangle(w2 - 64, h2 + 48, 128, 40), OnClick, 0, text);
                                button.TextColor = Color.NavajoWhite;
                                button = gui.AddButton("main", "Exit", new Rectangle(w2 - 64, h2 + 100, 128, 40), OnClick, 0, text);
                                button.TextColor = Color.NavajoWhite;
                            }
                            else
                            {
                                gui.AddTextArea("pause", "press Q to disconnect", new Rectangle(w2 - 150 + 2, h2 + 32 + 2, 300, 32), text);
                                var text2 = gui.AddTextArea("pause", "press Q to disconnect", new Rectangle(w2 - 150, h2 + 32, 300, 32), text);
                                text2.Color = Color.NavajoWhite;
                            }
                        }
                    }
                    else if (state == PlayState.Pause)
                    {                        
                        var pause = gui.FindChild("pause", false);
                        if (pause != null)
                            gui.RemoveChild(pause);
                    }

                    if (value == PlayState.LevelComplete)
                    {
                        var control = gui.FindChild("lvlcomplete", false);
                        if (control == null)
                        {
                            int w2 = GraphicsDevice.Viewport.Width / 2;
                            int h2 = GraphicsDevice.Viewport.Height / 2;
                            var text = gui.AddTextArea("lvlcomplete", "Level complete", new Rectangle(w2 - 198, h2 + 2, 400, 32));
                            text = gui.AddTextArea("lvlcomplete", "Level complete", new Rectangle(w2 - 200, h2, 400, 32), text);
                            text.Color = Color.NavajoWhite;
                            if (client == null)
                            {
                                var button = gui.AddButton("nextlvl", "Next level", new Rectangle(w2 - 128, h2 + 64, 256, 40), OnClick, 0, text);
                                button.TextColor = Color.NavajoWhite;
                            }
                            else
                            {
                                gui.AddTextArea("txt", "(waiting on server)", new Rectangle(w2 - 150 + 2, h2 + 32 + 2, 300, 32), text);
                                var text2 = gui.AddTextArea("txt", "(waiting on server)", new Rectangle(w2 - 150, h2 + 32, 300, 32), text);
                                text2.Color = Color.NavajoWhite;
                            }
                        }
                    }
                    else if (state == PlayState.LevelComplete)
                    {
                        var control = gui.FindChild("lvlcomplete", false);
                        if (control != null)
                            gui.RemoveChild(control);
                    }

                    if (value == PlayState.GameOver)
                    {
                        var control = gui.FindChild("gameover", false);
                        if (control == null)
                        {
                            int w2 = GraphicsDevice.Viewport.Width / 2;
                            int h2 = GraphicsDevice.Viewport.Height / 2;
                            var text = gui.AddTextArea("gameover", "Game Over", new Rectangle(w2 - 198, h2 + 2, 400, 32));
                            text = gui.AddTextArea("gameover", "Game Over", new Rectangle(w2 - 200, h2, 400, 32), text);
                            text.Color = Color.NavajoWhite;
                            if (client == null)
                            {
                                var b = gui.AddButton("restart", "Restart", new Rectangle(w2 - 64, h2 + 48, 128, 40), OnClick, 0, text);
                                b.TextColor = Color.NavajoWhite;
                            }
                            var button = gui.AddButton("main", "Exit", new Rectangle(w2 - 64, h2 + 100, 128, 40), OnClick, 0, text);
                            button.TextColor = Color.NavajoWhite;
                        }
                    }
                    else if (state == PlayState.GameOver)
                    {
                        var control = gui.FindChild("gameover", false);
                        if (control != null)
                            gui.RemoveChild(control);
                    }


                    if (value == PlayState.Playing)
                        Main.cursor.Active = false;
                    else
                        Main.cursor.Active = true;

                    oldState = state;
                    state = value;
                    if (server != null)
                        server.SendGameState();
                }
            }
        }

        public Random rand = new Random();

        public Controller gui;
        Texture2D border;
        SpriteFont font;

        Server server;
        NetClient client;

        Texture2D background;

        public Play(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (server != null)
                    server.Dispose();
                else if (client != null)
                    client.Dispose();                
            }
            base.Dispose(disposing);
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            spriteSheet = new SpriteSheet(Game.Content.Load<Texture2D>(@"Sprites\tiles"), 32, 16);
            border = Game.Content.Load<Texture2D>(@"Sprites\Border");
            font = Game.Content.Load<SpriteFont>(@"Fonts\8bitMadness");

            gui = new Controller("controller", new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), font);
            gui.AddButtonStyle(Game.Content.Load<Texture2D>(@"Sprites\button"), 100, 20);
            gui.AddFrameStyle(Game.Content.Load<Texture2D>(@"Sprites\frame"), 20, 20);

            oldMouseState = Mouse.GetState();
            oldKeyState = Keyboard.GetState();

            center = new Vector2(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);

            base.LoadContent();
        }
        public void JoinGame(string ip)
        {
            client = new NetClient(this);
            System.Threading.Thread t = new System.Threading.Thread(TryJoin);
            t.Start(ip);
        }
        public void HostGame()
        {            
            server = new Server(this);
            System.Threading.Thread t = new System.Threading.Thread(TryHost);
            t.Start();
        }
        void TryHost()
        {
            TextArea status = gui.AddTextArea("status", "Connecting to master server...", new Rectangle(gui.Position.Width / 4, 0, gui.Position.Width / 2, gui.Position.Height));
            status.Color = Color.White;
            System.Threading.Thread.Sleep(1000);
            byte connStatus = server.Initialize(status);
            if (connStatus == 0)
            {
                status.Text = "Starting game";
                System.Threading.Thread.Sleep(1000);
                gui.ClearChildren();
                NewGame();
            }
            else
            {
                gui.AddButton("main", "Back", new Rectangle(GraphicsDevice.Viewport.Width / 2 - 128 - 16, GraphicsDevice.Viewport.Height * 3 / 4, 128, 48), OnClick);
                gui.AddButton("start", "Ignore", new Rectangle(GraphicsDevice.Viewport.Width / 2 + 16, GraphicsDevice.Viewport.Height * 3 / 4, 128, 48), OnClick);
                status.Enabled = false;
                if (connStatus == 1)                
                    status.Text = "Could not bind to socket";                
                else if (connStatus == 2)                
                    status.Text = "Could not connect to master server";                
                else if (connStatus == 3)
                {
                    string localIP = "";
                    try
                    {
                        var addr = System.Net.Dns.GetHostEntry(Environment.MachineName).AddressList;
                        for (int i = 0; i < addr.Length; i++)
                        {
                            var ip = addr[i];
                            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                localIP = ip.ToString();
                                break;
                            }
                        }
                    }
                    catch{}
                    status.Text = String.Format("Master server could not connect to your game. Open port 15000 UDP on your firewall to be able to host. (local players may still be able to connect to your IP: {0})", localIP);
                }
                else
                    status.Text = "Unknown error";             
                status.Enabled = true;
            }

        }
        void TryJoin(object args)
        {
            string ip = (string)args;
            TextArea status = gui.AddTextArea("status", String.Format("Connecting to {0}...", ip), new Rectangle(gui.Position.Width / 4, 0, gui.Position.Width / 2, gui.Position.Height));
            status.Color = Color.White;
            System.Threading.Thread.Sleep(1000);
            byte connStatus = client.Initialize(status, ip);
            if (connStatus == 0)
            {
                gui.ClearChildren();
                Paddles[0] = new Paddle(1) { Controller = Main.settings.Controller };
                Main.cursor.Active = false;
            }
            else
            {
                gui.AddButton("main", "Back", new Rectangle(GraphicsDevice.Viewport.Width / 2 - 64, GraphicsDevice.Viewport.Height * 3 / 4, 128, 48), OnClick);
                status.Enabled = false;
                if (connStatus == 1)
                    status.Text = String.Format("Could not connect to {0}", ip);
                else if (connStatus == 2)
                    status.Text = "The game is full";
                else
                    status.Text = "Unknown error";
                status.Enabled = true;
            }
        }
        public void NewGame()
        {
            Main.Audio.State = 2;
            if (server != null)
                GenerateLevel(12, 18, 5, 5, true, true);
            else
                GenerateLevel(12, 16, 2, 8, true, false);
            Paddles[0] = new Paddle(0) { Controller = Main.settings.Controller };
            Balls = new Ball[0];
            Drops = new List<Drop>();
            Lives = 3;
            State = PlayState.Playing;
            NextBG();
            Main.cursor.Active = false;
        }
        public void NewLocalCoop(byte cont1, byte cont2)
        {
            Main.Audio.State = 2;
            GenerateLevel(12, 18, 5, 5, true, true);
            Paddles[0] = new Paddle(0) { Controller = cont1 };
            Paddles[1] = new Paddle(1) { Controller = cont2 };
            Balls = new Ball[0];
            Drops = new List<Drop>();
            Lives = 3;
            State = PlayState.Playing;
            NextBG();
            Main.cursor.Active = false;
        }

        public void RestartGame()
        {
            score = 0;
            lives = 3;
            NextLevel();
        }

        public void NextBG()
        {
            int nBG = rand.Next(0, Main.Backgrounds.Length);
            while (nBG == Main.currentBG)
                nBG = rand.Next(0, Main.Backgrounds.Length);
            Main.currentBG = nBG;
            background = Main.Backgrounds[nBG];
        }
        public void NextLevel()
        {
            if (server != null || Paddles[1] != null)
                GenerateLevel(12, 18, 5, 5, true, true);
            else
                GenerateLevel(12, 16, 2, 8, true, false);
            Drops = new List<Drop>();
            Balls = new Ball[0];
            Paddles[0].Length = 2;
            if (Paddles[1] != null)            
                Paddles[1].Length = 2;            
            State = PlayState.Playing;
            if (server != null)
            {
                server.SendLvlData();
                server.SendBallData();
                server.SendDropData();
                server.SendPaddleData(0);
                server.SendPaddleData(1);                
            }
            Main.Audio.NextSong();
            NextBG();
            gui.ClearChildren();
        }


        public override void Update(GameTime gameTime)
        {
            float dT = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000;

            if (Game.IsActive)
            {
                gui.Update(gameTime);
                HandleInput();
                if (MediaPlayer.State == MediaState.Paused)
                    MediaPlayer.Resume();
            }
            else if (MediaPlayer.State == MediaState.Playing)
                MediaPlayer.Pause();

            if (State == PlayState.Pause || State == PlayState.GameOver || State == PlayState.LevelComplete || level == null)
            {
                base.Update(gameTime);
                return;
            }
            

            bool sendballinfo = false;            
            float ballRadius = 0.15f / level.Width;
            for (int i = Balls.Length - 1; i >= 0; i--)
            {
                Ball ball = Balls[i];
                ball.Position += ball.Velocity * ball.Speed * dT;

                if (ball.Position.X <= ballRadius * ball.Size)
                {
                    ball.Position.X = ballRadius * ball.Size;
                    ball.Velocity.X *= -1;
                }
                else if (ball.Position.X >= 1 - ballRadius * ball.Size)
                {
                    ball.Position.X = 1f - ballRadius * ball.Size;
                    ball.Velocity.X *= -1;
                }
                if (ball.Position.Y <= ballRadius * ball.Size)
                {
                    if (Paddles[1] == null)
                    {
                        ball.Velocity.Y *= -1;
                        sendballinfo = true;
                    }
                    else if (!ball.NoCollisionPaddle)
                    {
                        float offset = Paddles[1].Position - ball.Position.X;
                        float hPaddle = ((float)Paddles[1].Length / level.Width) / 2;
                        if (Math.Abs(offset) < hPaddle) // ########## Ball - Paddle 2 Collision ######
                        {
                            Vector2 normal = new Vector2(offset / hPaddle, -2);
                            normal.Normalize();
                            Vector2 reflect = Vector2.Reflect(ball.Velocity, normal);
                            reflect.Normalize();
                            if (reflect.Y <= 0 || Math.Abs(reflect.X) / Math.Abs(reflect.Y) >= 3)
                            {
                                reflect.Y = Math.Abs(reflect.X) / 3f;
                                reflect.Normalize();
                            }
                            ball.NoCollisionPaddle = true;
                            ball.Velocity = reflect;
                            ball.TouchedLast = false;
                            Main.Audio.PlayEffect(0);
                            sendballinfo = true;
                        }
                        // ball misses paddle
                        else if (ball.Position.Y <= 0)
                        {
                            sendballinfo = true;
                            Main.Audio.PlayEffect(2);

                            RemoveBallFromList(i, ref Balls);
                            if (client == null && Balls.Length == 0)
                            {
                                Lives--;
                                Paddles[1].Length = 2;
                                if (server != null)
                                    server.SendPaddleData(1);
                            }
                            continue;
                        }
                    }
                }
                else if (ball.Position.Y + ballRadius * ball.Size >= 1)
                {
                    if (!ball.NoCollisionPaddle)
                    {
                        float offset = ball.Position.X - Paddles[0].Position;
                        float hPaddle = ((float)Paddles[0].Length / level.Width) / 2;

                        if (Math.Abs(offset) < hPaddle)// ########## Ball - Paddle 1 Collision ######
                        {
                            Vector2 normal = new Vector2(offset / hPaddle, -2);
                            normal.Normalize();
                            Vector2 reflect = Vector2.Reflect(ball.Velocity, normal);
                            reflect.Normalize();
                            if (reflect.Y >= 0 || Math.Abs(reflect.X) / Math.Abs(reflect.Y) >= 3)
                            {
                                reflect.Y = Math.Abs(reflect.X) / -3f;
                                reflect.Normalize();
                            }
                            ball.Velocity = reflect;
                            ball.TouchedLast = true;
                            ball.NoCollisionPaddle = true;
                            Main.Audio.PlayEffect(4);
                            sendballinfo = true;
                        }
                        else if (ball.Position.Y >= 1)// ball misses paddle
                        {
                            sendballinfo = true;
                            Main.Audio.PlayEffect(2);
                            RemoveBallFromList(i, ref Balls);
                            if (client == null && Balls.Length == 0)
                            {
                                Lives--;
                                Paddles[0].Length = 2;
                                if (server != null)
                                    server.SendPaddleData(0);
                            }
                            continue;
                        }
                    }
                }
                else
                    ball.NoCollisionPaddle = false;
                // ########### Ball - Brick collisions ###############

                int ballTileX = (int)(level.Width * (ball.Position.X - ballRadius * ball.Size));
                int ballTileX2 = (int)(level.Width * (ball.Position.X + ballRadius * ball.Size));

                int ballTileY = (int)(level.Height * (ball.Position.Y - ballRadius * ball.Size));
                int ballTileY2 = (int)(level.Height * (ball.Position.Y + ballRadius * ball.Size));

                List<Point> bricks = new List<Point>();
                List<Brick> collide = new List<Brick>();
                bricks.Add(new Point(ballTileX, ballTileY));
                if (ballTileX != ballTileX2)                
                    bricks.Add(new Point(ballTileX2, ballTileY));                
                if (ballTileY != ballTileY2)
                {
                    bricks.Add(new Point(ballTileX, ballTileY2)); 
                    if (ballTileX != ballTileX2)                    
                        bricks.Add(new Point(ballTileX2, ballTileY2));
                }
                float tilePW = 1f / level.Width;
                float tilePH = 1f / level.Height;
                Vector2 brickPadding = new Vector2(tilePW * 0.2f, tilePH * 0.1f);
                float radius2 = ballRadius * ball.Size * ballRadius * ball.Size;

                foreach (Point point in bricks)
                {
                    if (point.Y < level.Height && point.X < level.Width && point.X >= 0 && point.Y >= 0)
                    {
                        int tile = point.X + point.Y * level.Width;
                        Brick brick = level.Blocks[tile];
                        if (brick == null)
                            continue;
                        float x = tilePW * point.X + brickPadding.X;
                        float y = tilePH * point.Y + brickPadding.Y;

                        float x2 = tilePW * point.X + tilePW - brickPadding.X;
                        float y2 = tilePH * point.Y + tilePH - brickPadding.Y;

                        float xDiff = (ball.Position.X < x) ? x - ball.Position.X : ball.Position.X - x2;
                        float yDiff = (ball.Position.Y < y) ? y - ball.Position.Y : ball.Position.Y - y2;
                        if (xDiff < 0)
                            xDiff = 0;
                        if (yDiff < 0)
                            yDiff = 0;

                        float distance2 = xDiff * xDiff + yDiff * yDiff;

                        if (distance2 < radius2) // ########## Ball- Brick Collision ########
                        {
                            collide.Add(brick);
                            if (ball.NoCollisionBrick.Contains(brick))                            
                                continue;
                            bool flipX = false;
                            bool flipY = false;
                            if (ball.Velocity.X < 0) // going left
                            {
                                if (ball.Position.X > x2)
                                    flipX = true;
                            }
                            else if (ball.Position.X < x)
                                flipX = true;                  
                            if (ball.Velocity.Y < 0) // going up
                            {
                                if (ball.Position.Y > y2)
                                    flipY = true;                         
                            }
                            else if (ball.Position.Y < y)
                                flipY = true;

                            if (flipX && flipY)
                            {
                                Vector2 center = new Vector2(tilePW * point.X + tilePW / 2, tilePH * point.Y + tilePH / 2);
                                ball.Velocity = Vector2.Reflect(ball.Velocity, Vector2.Normalize(ball.Position - center));
                            }
                            else if (flipX)
                                ball.Velocity.X *= -1;
                            else if (flipY)
                                ball.Velocity.Y *= -1;
                            else // ball center inside brick
                            {
                                if (ball.Velocity.X < 0) // going left
                                {
                                    if (Math.Abs(ball.Position.X - x) > Math.Abs(ball.Position.X - x2)) // ball closer to right edge
                                        ball.Velocity.X *= -1;
                                }
                                else if (Math.Abs(ball.Position.X - x) < Math.Abs(ball.Position.X - x2)) // ball closer to left edge
                                    ball.Velocity.X *= -1;
                                if (ball.Velocity.Y < 0) // going up
                                {
                                    if (Math.Abs(ball.Position.Y - y) > Math.Abs(ball.Position.Y - y2)) // ball closer to bottom edge
                                        ball.Velocity.Y *= -1;
                                }
                                else if (Math.Abs(ball.Position.Y - y) < Math.Abs(ball.Position.Y - y2)) // ball closer to top edge
                                    ball.Velocity.Y *= -1;
                            }

                            
                            if (client == null)
                            {
                                brick.Strength--;

                                if (server != null)
                                    server.SendBrickInfo(tile);
                                if (brick.Strength <= 0)
                                {
                                    score += 10;
                                    if (server != null)
                                        server.SendScoreInfo();
                                    if (brick.Drop != null)
                                    {
                                        Drop drop = brick.Drop;
                                        drop.Position = new Vector2(((float)ballTileX + 0.5f) / level.Width, ((float)ballTileY + 0.5f) / level.Height);
                                        drop.Velocity = new Vector2(0, 0.2f);
                                        if (!ball.TouchedLast)
                                            drop.Velocity *= -1;
                                        Drops.Add(drop);
                                        if (server != null)
                                            server.SendDropData();
                                    }
                                    level.Blocks[tile] = null;
                                    Main.Audio.PlayEffect(0);
                                    bool end = true;
                                    for (int b = 0; b < level.Blocks.Length; b++)
                                    {
                                        if (level.Blocks[b] != null)
                                        {
                                            end = false;
                                            break;
                                        }
                                    }
                                    if (end)
                                    {
                                        State = PlayState.LevelComplete;
                                    }
                                }
                                else
                                    Main.Audio.PlayEffect(1);
                            }
                            sendballinfo = true;
                        }
                    }
                }
                ball.NoCollisionBrick = collide;
            }
            // ########### Ball - Ball collisions ###############
            for (int i = Balls.Length - 1; i >= 0; i--)
            {
                Ball ball = Balls[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    Ball otherBall = Balls[j];
                    if (Vector2.Distance(ball.Position, otherBall.Position) <= ballRadius * ball.Size + ballRadius * otherBall.Size)
                    {
                        if (ball.NoCollisionBall.Contains(otherBall))                        
                            continue;

                        Vector2 dPos = ball.Position - otherBall.Position; ;
                        double angle = Math.Atan2(dPos.Y, dPos.X);
                        double b1Dir = Math.Atan2(ball.Velocity.Y, ball.Velocity.X);
                        double b2Dir = Math.Atan2(otherBall.Velocity.Y, otherBall.Velocity.X);
                        Vector2 b1NewVelocity = new Vector2((float)(ball.Speed * Math.Cos(b1Dir - angle)), (float)(ball.Speed * Math.Sin(b1Dir - angle)));
                        Vector2 b2NewVelocity = new Vector2((float)(otherBall.Speed * Math.Cos(b2Dir - angle)), (float)(otherBall.Speed * Math.Sin(b2Dir - angle)));
                        //double pA_finalVelocityX = ((particleA.Mass - particleB.Mass) * pA_newVelocityX + (particleB.Mass + particleB.Mass) * pB_newVelocityX) / (particleA.Mass + particleB.Mass);  
                        //double pB_finalVelocityX = ((particleA.Mass + particleA.Mass) * pA_newVelocityX + (particleB.Mass - particleA.Mass) * pB_newVelocityX) / (particleA.Mass + particleB.Mass);   
                        double b1FinalVelocityX = ((ball.Size - otherBall.Size) * b1NewVelocity.X + (otherBall.Size + otherBall.Size) * b2NewVelocity.X) / (ball.Size + otherBall.Size);
                        double b2FinalVelocityX = ((ball.Size + ball.Size) * b1NewVelocity.X + (otherBall.Size - ball.Size) * b2NewVelocity.X) / (ball.Size + otherBall.Size);
                        //particleA.Velocity = new Vector2((float)(Math.Cos(collisionAngle) * pA_finalVelocityX + Math.Cos(collisionAngle + Math.PI / 2) * pA_finalVelocityY), (float)(Math.Sin(collisionAngle) * pA_finalVelocityX + Math.Sin(collisionAngle + Math.PI / 2) * pA_finalVelocityY));
                        // particleB.Velocity = new Vector2((float)(Math.Cos(collisionAngle) * pB_finalVelocityX + Math.Cos(collisionAngle + Math.PI / 2) * pB_finalVelocityY), (float)(Math.Sin(collisionAngle) * pB_finalVelocityX + Math.Sin(collisionAngle + Math.PI / 2) * pB_finalVelocityY));
                        Vector2 b1Final = new Vector2((float)(Math.Cos(angle) * b1FinalVelocityX + Math.Cos(angle + Math.PI / 2) * b1NewVelocity.Y), (float)(Math.Sin(angle) * b1FinalVelocityX + Math.Sin(angle + Math.PI / 2) * b1NewVelocity.Y));
                        Vector2 b2Final = new Vector2((float)(Math.Cos(angle) * b2FinalVelocityX + Math.Cos(angle + Math.PI / 2) * b2NewVelocity.Y), (float)(Math.Sin(angle) * b2FinalVelocityX + Math.Sin(angle + Math.PI / 2) * b2NewVelocity.Y));

                        ball.Speed = b1Final.Length();
                        ball.Velocity = Vector2.Normalize(b1Final);
                        otherBall.Speed = b2Final.Length();
                        otherBall.Velocity = Vector2.Normalize(b2Final);

                        //ball.Position += ball.Velocity * ball.Speed * dT;
                        //otherBall.Position += otherBall.Velocity * otherBall.Speed * dT;
                        ball.NoCollisionBall.Add(otherBall);

                        sendballinfo = true;
                    }
                    else
                        ball.NoCollisionBall.Remove(otherBall);
                }
            }

            // ######## DROPS ####################
            ballRadius = (0.5f / level.Width) / 2;
            for (int i = Drops.Count - 1; i >= 0; i--)
            {
                Drop drop = Drops[i];
                drop.Position += drop.Velocity * dT;
                
                if (drop.Position.Y > 1f || drop.Position.Y < 0f)
                {
                    Drops.RemoveAt(i);
                }
                else if (drop.Position.Y >= 1f - ballRadius)
                {
                    float offset = Paddles[0].Position - drop.Position.X - ballRadius;
                    float hPaddle = ((float)Paddles[0].Length / level.Width) / 2;
                    if (Math.Abs(offset) < hPaddle) // you collect upgrade
                    {
                        if (client == null)
                            HandleDrop(drop, 0);
                        Drops.RemoveAt(i);
                        Main.Audio.PlayEffect(3);
                    }
                }
                else if (Paddles[1] != null && drop.Position.Y <= ballRadius)
                {
                    float offset = Paddles[1].Position - drop.Position.X - ballRadius;
                    float hPaddle = ((float)Paddles[1].Length / level.Width) / 2;
                    if (Math.Abs(offset) < hPaddle) // player 2 collects upgrade
                    {
                        if (client == null)
                            HandleDrop(drop, 1);
                        Drops.RemoveAt(i);
                        Main.Audio.PlayEffect(3);
                    }
                }
            }
            if (server != null)
            {
                server.Update(gameTime);
                if (sendballinfo)
                    server.SendBallData();
            }
            else if (client != null)
            {
                client.Update(gameTime);
            }
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);



            if (level != null)
            {
                // ######### Background ########
                if (background != null)
                    spriteBatch.Draw(background, new Rectangle(offsetX + spaceW / 2, offsetY + spaceH, spaceW * level.Width, spaceH * level.Height), Color.White);


                // ########### BLOCKS ##########            
                for (int y = 0; y < level.Height; y++)
                {
                    for (int x = 0; x < level.Width; x++)
                    {
                        int tile = x + y * level.Width;
                        if (level.Blocks[tile] != null)
                        {
                            Rectangle dest = new Rectangle(offsetX + spaceW / 2 + x * spaceW + margin / 2, offsetY + spaceH + y * spaceH + marginH / 2, tileW, tileH);
                            spriteBatch.Draw(spriteSheet.Texture, dest, spriteSheet.GetSourceRect((ushort)level.Blocks[tile].GID), Color.White);
                        }
                    }
                }
                // #############################


                // ####### Paddle ##############
                for (int i = 0; i < Paddles.Length; i++)
                {
                    if (Paddles[i] != null)
                    {
                        Rectangle paddleSource = spriteSheet.GetSourceRect((ushort)Paddles[i].FirstgID);
                        if (Paddles[i].Length > 1)
                            paddleSource = new Rectangle(paddleSource.X, paddleSource.Y, paddleSource.Width * Paddles[i].Length, paddleSource.Height);
                        float posY = GraphicsDevice.Viewport.Height - offsetY - spaceH;
                        if (i == 1)
                            posY = offsetY;
                        Vector2 paddlePos = new Vector2(offsetX + spaceW / 2 + level.Width * spaceW * Paddles[i].Position, posY);
                        spriteBatch.Draw(spriteSheet.Texture, paddlePos, paddleSource, Color.White, 0f, new Vector2((spriteSheet.TileW * Paddles[i].Length) / 2, 0), new Vector2(spaceW / 32f, spaceH / 16f), SpriteEffects.None, 0);
                    }
                }

                // #############################

                // ######### Ball ##############
                if (Balls.Length == 0)
                {
                    Vector2 ballPos;
                    if (client != null && Paddles[1] != null)
                        ballPos = new Vector2(offsetX + spaceW / 2 + level.Width * spaceW * Paddles[1].Position, offsetY + spaceH);
                    else
                        ballPos = new Vector2(offsetX + spaceW / 2 + level.Width * spaceW * Paddles[0].Position, GraphicsDevice.Viewport.Height - offsetY - spaceH);
                    spriteBatch.Draw(spriteSheet.Texture, ballPos, new Rectangle(96, 48, 8, 8), Color.White, 0f, new Vector2(4, 4), new Vector2(spaceW / 32f, spaceH / 16f), SpriteEffects.None, 0);
                }
                else
                {
                    foreach (Ball ball in Balls)
                    {
                        Vector2 ballPos = new Vector2(offsetX + spaceW / 2 + level.Width * spaceW * ball.Position.X, offsetY + spaceH + level.Height * spaceH * ball.Position.Y);
                        spriteBatch.Draw(spriteSheet.Texture, ballPos, ball.Source, Color.White, 0f, new Vector2(4, 4), new Vector2(ball.Size * spaceW / 32f, ball.Size * spaceH / 16f), SpriteEffects.None, 0);
                    }
                }
                // #############################

                // ########### Upgrades ########
                foreach (Drop drop in Drops)
                {
                    Vector2 pos = new Vector2(offsetX + spaceW / 2 + level.Width * spaceW * drop.Position.X, offsetY + spaceH + level.Height * spaceH * drop.Position.Y);
                    spriteBatch.Draw(spriteSheet.Texture, pos, drop.Source, Color.White, 0f, new Vector2(8, 8), new Vector2(spaceW / 32f, spaceH / 16f), SpriteEffects.None, 0);
                }
                // #############################

                // ######### Border ##############
                int bW = (int)((spaceW / 40f) * 20);
                int bH = (int)((spaceH / 20f) * 20);
                int bW4 = bW / 4;
                int bH4 = bH / 4;
                if (Paddles[1] == null)
                    spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2, offsetY + spaceH - bH4 * 3, level.Width * spaceW, bH), new Rectangle(20, 0, 20, 20), Color.White); // top
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 - bW4 * 3, offsetY + spaceH, bW, level.Height * spaceH), new Rectangle(40, 0, 20, 20), Color.White); // left
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 + spaceW * level.Width - bW4, offsetY + spaceH, bW, level.Height * spaceH), new Rectangle(40, 0, 20, 20), Color.White); //right
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 - bW4 * 3, offsetY + spaceH - bH4 * 3, bW, bH), new Rectangle(0, 0, 20, 20), Color.White); // top left
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 + spaceW * level.Width - bW4, offsetY + spaceH - bH4 * 3, bW, bH), new Rectangle(0, 0, 20, 20), Color.White); // top right
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 - bW4 * 3, offsetY + spaceH + spaceH * level.Height - bH4, bW, bH), new Rectangle(0, 0, 20, 20), Color.White); // bottom left
                spriteBatch.Draw(border, new Rectangle(offsetX + spaceW / 2 + spaceW * level.Width - bW4, offsetY + spaceH + spaceH * level.Height - bH4, bW, bH), new Rectangle(0, 0, 20, 20), Color.White); // bottom right
                // #############################

                // ########### GUI ############
                // LIVES
                int lW = (int)((spaceW / 32f) * 9);
                int lH = (int)((spaceH / 16f) * 9);
                for (int i = 0; i < Lives; i++)
                    spriteBatch.Draw(spriteSheet.Texture, new Rectangle(offsetX + spaceW / 2 + 10 + (lW / 2) * i, offsetY + spaceH + 10, lW, lH), new Rectangle(128, 48, 10, 10), Color.White);                
                // SCORE
                string scoreTxt = String.Format("score: {0}", score);
                Vector2 strlen = font.MeasureString(scoreTxt);
                spriteBatch.DrawString(font, scoreTxt, new Vector2(GraphicsDevice.Viewport.Width - spaceW / 2 - 10 - offsetX - strlen.X + 2, offsetX + spaceH + 10 + 2), Color.Black);
                spriteBatch.DrawString(font, scoreTxt, new Vector2(GraphicsDevice.Viewport.Width - spaceW / 2 - 10 - offsetX - strlen.X, offsetX + spaceH + 10), Color.NavajoWhite);
                // ping
                string pingTxt = "";
                if (server != null && server.connected)                
                    pingTxt = String.Format("ping: {0}", server.Ping);
                else if (client != null && client.connected)
                    pingTxt = String.Format("ping: {0}", client.Ping);                
                if (pingTxt != "")
                {
                    strlen = font.MeasureString(pingTxt);
                    spriteBatch.DrawString(font, pingTxt, new Vector2(GraphicsDevice.Viewport.Width - spaceW / 2 - 10 - offsetX - strlen.X + 2, offsetX + spaceH + 50 + 2), Color.Black);
                    spriteBatch.DrawString(font, pingTxt, new Vector2(GraphicsDevice.Viewport.Width - spaceW / 2 - 10 - offsetX - strlen.X, offsetX + spaceH + 50), Color.NavajoWhite);
                }
            }

            gui.Draw(spriteBatch);
            Main.cursor.Draw(spriteBatch);
            spriteBatch.End();
            base.Draw(gameTime);
        }



        // ########################### METHODS #####################################################################



        void HandleInput()
        {
            KeyboardState keyState = Keyboard.GetState();
            if (keyState.IsKeyDown(Keys.Escape) && oldKeyState.IsKeyUp(Keys.Escape))
            {
                if (state == PlayState.Pause)
                    State = oldState;
                else
                    State = PlayState.Pause;
            }

            if (keyState.IsKeyDown(Keys.Q) && oldKeyState.IsKeyUp(Keys.Q) && client != null && state == PlayState.Pause)
            {
                client.Disconnect("Disconnected");
            }
             if (keyState.IsKeyDown(Keys.R) && oldKeyState.IsKeyUp(Keys.R))
                 NextLevel();
             if (keyState.IsKeyDown(Keys.B) && oldKeyState.IsKeyUp(Keys.B))
             {
                 Ball ball = new Ball()
                 {
                     Type = (byte)rand.Next(0, 6),
                     Speed = rand.Next(200, 500) / 1000f,
                     Velocity = Vector2.Normalize(new Vector2((float)rand.NextDouble(), (float)rand.NextDouble())),
                     Position = new Vector2((float)rand.NextDouble(), (float)rand.NextDouble()),
                     Size = 0.5f
                 };
                 AddBallToList(ref ball, ref Balls);

             }
             if (keyState.IsKeyDown(Keys.N) && oldKeyState.IsKeyUp(Keys.N))
                 HandleDrop(new Drop() { Type = DropType.Ball }, 0);
            
             if (keyState.IsKeyDown(Keys.Add) && oldKeyState.IsKeyUp(Keys.Add))
                 HandleDrop(new Drop() { Type = DropType.Accelerate }, 0);
             if (keyState.IsKeyDown(Keys.Subtract) && oldKeyState.IsKeyUp(Keys.Subtract))
                 HandleDrop(new Drop() { Type = DropType.Slow }, 0);
             if (keyState.IsKeyDown(Keys.T) && oldKeyState.IsKeyUp(Keys.T))
                 throw new Exception("test");


            MouseState mouseState = Mouse.GetState();
            if (State == PlayState.Playing)
            {
                if (Paddles[0] != null)
                {
                    float diff = 0;
                    if (Paddles[0].Controller == 2)
                    {
                        if (keyState.IsKeyDown(Keys.A))
                            diff = -0.01f;
                        if (keyState.IsKeyDown(Keys.D))
                            diff = 0.01f;
                        if (keyState.IsKeyDown(Keys.S))
                            diff = -0.025f;
                        if (keyState.IsKeyDown(Keys.W))
                            diff = 0.025f;
                    }
                    else if (Paddles[0].Controller == 1)
                    {
                        if (keyState.IsKeyDown(Keys.Left))
                            diff = -0.01f;
                        if (keyState.IsKeyDown(Keys.Right))
                            diff = 0.01f;
                        if (keyState.IsKeyDown(Keys.Down))
                            diff = -0.025f;
                        if (keyState.IsKeyDown(Keys.Up))
                            diff = 0.025f;
                    }
                    else
                        diff = (mouseState.X - center.X) * 0.001f;

                    Paddles[0].Position = MathHelper.Clamp(Paddles[0].Position + diff, 0, 1);
                    if (diff != 0)
                    {
                        if (server != null)
                            server.SendPaddleData();
                        else if (client != null)
                            client.SendPaddleData();
                    }

                    if (client == null && Balls.Length == 0 && Lives > 0)
                    {
                        if ((Paddles[0].Controller == 0 && mouseState.LeftButton == ButtonState.Pressed && oldMouseState.LeftButton == ButtonState.Released) ||
                            (Paddles[0].Controller == 1 && keyState.IsKeyDown(Keys.Up) && oldKeyState.IsKeyUp(Keys.Up)) ||
                            (Paddles[0].Controller == 2 && keyState.IsKeyDown(Keys.W) && oldKeyState.IsKeyUp(Keys.W)))
                        {

                            float yS = 0.7f;
                            float xS = yS * (rand.Next(5, 20) / 100f);
                            yS -= xS;
                            Vector2 vel = new Vector2(xS, -yS);
                            vel.Normalize();
                            float ballRadius = 0.125f / level.Width;
                            Ball b = new Ball() { Position = new Vector2(Paddles[0].Position, 1f - ballRadius), Size = 1f, Type = 0, Velocity = vel, Speed = 0.5f, TouchedLast = true };
                            AddBallToList(ref b, ref Balls);
                            if (server != null)
                                server.SendBallData();
                        }
                    }
                }

                if (client == null && server == null && Paddles[1] != null) // 2nd paddle local coop
                {
                    float diff = 0;
                    if (Paddles[1].Controller == 2)
                    {
                        if (keyState.IsKeyDown(Keys.A))
                            diff = -0.01f;
                        if (keyState.IsKeyDown(Keys.D))
                            diff = 0.01f;
                        if (keyState.IsKeyDown(Keys.S))
                            diff = -0.025f;
                        if (keyState.IsKeyDown(Keys.W))
                            diff = 0.025f;
                    }
                    else if (Paddles[1].Controller == 1)
                    {
                        if (keyState.IsKeyDown(Keys.Left))
                            diff = -0.01f;
                        if (keyState.IsKeyDown(Keys.Right))
                            diff = 0.01f;
                        if (keyState.IsKeyDown(Keys.Down))
                            diff = -0.025f;
                        if (keyState.IsKeyDown(Keys.Up))
                            diff = 0.025f;
                    }
                    else
                        diff = (mouseState.X - center.X) * 0.001f;
                    Paddles[1].Position = MathHelper.Clamp(Paddles[1].Position + diff, 0, 1);
                }                
            }
            oldKeyState = keyState;

            if (!Main.cursor.Active)
                Mouse.SetPosition((int)center.X, (int)center.Y);
        }


        Brick GenerateBrick(int posIndex)
        {
            int percent = rand.Next(0, 100);
            byte str = 1;
            if (percent > 90)
                str = 4;
            else if (percent > 78)
                str = 3;
            else if (percent > 50)
                str = 2;
            return new Brick(posIndex, (byte)rand.Next(0, 5), str);
        }
        void GenerateLevel(int w, int h, int padT, int padB, bool mirrorW, bool mirrorH)
        {
            Brick[] blocks = new Brick[w * h];
            int numOfBricks = 0;

            // 1st quadrant
            for (int y = padT; y < h / 2; y++)
            {
                for (int x = 1; x < w / 2; x++)
                {
                    if (rand.NextDouble() < 0.5f)
                    {
                        numOfBricks++;
                        blocks[x + y * w] = GenerateBrick(x + y * w);
                    }
                }
            }
            // 2nd quadrant
            for (int y = padT; y < h / 2; y++)
            {
                for (int x = w / 2; x < w - 1; x++)
                {
                    if (mirrorW)
                    {
                        Brick m = blocks[w - x - 1 + y * w];
                        if (m != null)
                            blocks[x + y * w] = new Brick(x + y * w, m.Color, m.Strength);
                    }
                    else if (rand.NextDouble() < 0.5f)
                    {
                        blocks[x + y * w] = GenerateBrick(x + y * w);
                        numOfBricks++;
                    }
                }
            }
            // 3rd quadrant
            for (int y = h / 2; y < h - padB; y++)
            {
                for (int x = 1; x < w / 2; x++)
                {
                    if (mirrorH)
                    {
                        Brick m = blocks[x + (h - y - 1) * w];
                        if (m != null)
                            blocks[x + y * w] = new Brick(x + y * w, m.Color, m.Strength);
                    }
                    else if (rand.NextDouble() < 0.5f)
                    {
                        blocks[x + y * w] = GenerateBrick(x + y * w);
                        numOfBricks++;
                    }
                }
            }

            // 4th quadrant
            for (int y = h / 2; y < h - padB; y++)
            {
                for (int x = w / 2; x < w - 1; x++)
                {
                    if (mirrorW)
                    {
                        Brick m = blocks[w - x - 1 + y * w];
                        if (m != null)
                            blocks[x + y * w] = new Brick(x + y * w, m.Color, m.Strength);
                    }
                    else if (mirrorH)
                    {
                        Brick m = blocks[x + (h - y - 1) * w];
                        if (m != null)
                            blocks[x + y * w] = new Brick(x + y * w, m.Color, m.Strength);
                    }
                    else if (rand.NextDouble() < 0.5f)
                    {
                        blocks[x + y * w] = GenerateBrick(x + y * w);
                        numOfBricks++;
                    }
                }
            }
            if (mirrorW)
                numOfBricks *= 2;
            if (mirrorH)
                numOfBricks *= 2;


            int numOfDrops = numOfBricks / 3;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int tile = x + y * w;
                    if (numOfDrops > 0 && blocks[tile] != null && rand.Next(0, 100) < 40)
                    {
                        blocks[tile].Drop = new Drop() { Type = (DropType)rand.Next(0, 9) };
                        numOfDrops--;
                    }
                }
            }

            spaceW = GraphicsDevice.Viewport.Width / (w + 1);
            offsetX = (GraphicsDevice.Viewport.Width % (w + 1)) / 2;
            spaceH = GraphicsDevice.Viewport.Height / (h + 2);
            offsetY = (GraphicsDevice.Viewport.Height % (h + 2)) / 2;

            tileW = (int)(spaceW * 0.8f);
            margin = spaceW - tileW;

            tileH = (int)(spaceH * 0.8f);
            marginH = spaceH - tileH;

            level = new Level() { Width = w, Height = h, Blocks = blocks };
        }
        public void LevelFromBytes(byte[] data)
        {
            int index = 0;
            int w = data[index++];
            int h = data[index++];
            Brick[] blocks = new Brick[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    byte b = data[index++];
                    if (b == 1)
                    {
                        blocks[x + y * w] = new Brick(x + y * w, data[index++], data[index++]);
                    }
                }
            }
            level = new Level() { Width = w, Height = h, Blocks = blocks };

            spaceW = GraphicsDevice.Viewport.Width / (level.Width + 1);
            offsetX = (GraphicsDevice.Viewport.Width % (level.Width + 1)) / 2;
            spaceH = GraphicsDevice.Viewport.Height / (level.Height + 2);
            offsetY = (GraphicsDevice.Viewport.Height % (level.Height + 2)) / 2;

            tileW = (int)(spaceW * 0.8f);
            margin = spaceW - tileW;

            tileH = (int)(spaceH * 0.8f);
            marginH = spaceH - tileH;
        }

        public void OnClick(object sender, EventArgs args)
        {
            Control c = sender as Control;
            if (c.Name == "main")
            {
                Title t = new Title(Game);
                t.Initialize();
                Game.Components.Add(t);
                Game.Components.Remove(this);
                this.Dispose(true);
            }
            else if (c.Name == "start")
            {
                gui.ClearChildren();
                NewGame();
            }
            else if (c.Name == "nextlvl")
            {
                NextLevel();
            }
            else if (c.Name == "restart")
            {
                RestartGame();
            }
        }

        void HandleDrop(Drop drop, byte paddleID)
        {
            switch (drop.Type)
            {
                case DropType.Shrink:
                    {
                        if (Paddles[paddleID].Length > 1)
                        {
                            Paddles[paddleID].Length--;
                            if (server != null)
                                server.SendPaddleData(paddleID);
                        }
                        break;
                    }
                case DropType.Enlarge:
                    {
                        if (Paddles[paddleID].Length < 4)
                        {
                            Paddles[paddleID].Length++;
                            if (server != null)
                                server.SendPaddleData(paddleID);
                        }
                        break;
                    }
                case DropType.Accelerate:
                    {
                        foreach (Ball b in Balls)
                        {
                            b.Speed += 0.2f;
                            if (b.Speed > 1.5f)
                                b.Speed = 1.5f;
                        }
                        if (server != null)
                            server.SendBallData();
                        break;
                    }
                case DropType.Slow:
                    {
                        foreach (Ball b in Balls)
                        {
                            b.Speed -= 0.2f;
                            if (b.Speed < 0.2f)
                                b.Speed = 0.2f;
                        }
                        if (server != null)
                            server.SendBallData();
                        break;
                    }
                case DropType.BallEnlarge:
                    {
                        foreach (Ball b in Balls)
                        {
                            b.Size += 0.3f;
                            if (b.Size > 3)
                                b.Size = 3;
                        }
                        if (server != null)
                            server.SendBallData();
                        break;
                    }
                case DropType.BallShrink:
                    {
                        foreach (Ball b in Balls)
                        {
                            b.Size -= 0.3f;
                            if (b.Size < 0.5f)
                                b.Size = 0.5f;
                        }
                        if (server != null)
                            server.SendBallData();
                        break;
                    }
                case DropType.Life:
                    {
                        Lives++;
                        if (server != null)
                            server.SendLifeInfo();
                        break;
                    }
                case DropType.Death:
                    {
                        Lives--;
                        if (server != null)
                            server.SendLifeInfo();
                        break;
                    }
                case DropType.Ball:
                    {
                        if (Balls.Length > 0)
                        {
                            Ball b = Balls[0];
                            //(U,V)=(U.x*V.y-U.y*V.x)
                            Vector2 perp = new Vector2(-b.Velocity.Y, b.Velocity.X);
                            Vector2 vel1 = b.Velocity + perp;
                            vel1.Normalize();
                            Vector2 vel2 = b.Velocity + (perp * -1);
                            vel2.Normalize();
                            b.Velocity = vel1;
                            Ball b2 = new Ball() { Position = b.Position, Velocity = vel2, Size = 1, Type = b.Type, Speed = b.Speed, TouchedLast = b.TouchedLast };
                            AddBallToList(ref b2, ref Balls);
                            b.NoCollisionBall.Add(b2);
                            b2.NoCollisionBall.Add(b);
                            if (server != null)
                                server.SendBallData();
                        }
                        break;
                    }
            }
        }

        public static void AddBallToList(ref Ball ball, ref Ball[] list)
        {
            int count = list.Length;
            Ball[] newlist = new Ball[count + 1];
            Array.Copy(list, 0, newlist, 0, count);
            ball.Index = (byte)count;
            newlist[count] = ball;
            list = newlist;
        }
        public static bool RemoveBallFromList(ref Ball ball, ref Ball[] list)
        {
            int count = list.Length;
            Ball[] newlist = new Ball[count - 1];
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                if (ball == list[i])
                {
                    found = true;
                }
                else if (found)
                {
                    newlist[i - 1] = list[i];
                    newlist[i - 1].Index = (byte)(i - 1);
                }
                else if (i != count - 1)
                    newlist[i] = list[i];
            }
            if (found)
            {
                list = newlist;
                return true;
            }
            return false;
        }
        public static bool RemoveBallFromList(int index, ref Ball[] list)
        {
            int count = list.Length;
            if (index < 0 || index >= count)
                return false;
            Ball[] newlist = new Ball[count - 1];
            for (int i = 0; i < count; i++)
            {
                if (i < index)
                    newlist[i] = list[i];
                else if (i > index)
                {
                    newlist[i - 1] = list[i];
                    newlist[i - 1].Index = (byte)(i - 1);
                }
            }
            list = newlist;
            return true;
        }
    }
    
}
