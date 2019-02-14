using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Media;

namespace Block_Buster
{
    class NetClient
    {
        public UdpClient Client;
        public bool connected = false;
        bool rejected = false;
        IPEndPoint remote;
        Play Game;

        int[] ping = new int[4];
        public int Ping { get { return (ping[0] + ping[1] + ping[2] + ping[3]) / 4; } }
        DateTime lastPing = DateTime.Now;

        List<Pkg> PkgQueue = new List<Pkg>();
        Dictionary<PkgType, UInt32> PkgIndex = new Dictionary<PkgType, uint>() { { PkgType.Ping, 0 }, { PkgType.Greet, 0} };
        Dictionary<PkgType, UInt32> RecIndex = new Dictionary<PkgType, uint>() {
            { PkgType.BallInfo, 0 },
            { PkgType.DropInfo, 0 },
            { PkgType.BrickInfo, 0 },
            { PkgType.LevelInfo, 0 },
            { PkgType.PaddleInfo, 0 },
            { PkgType.LifeInfo, 0 },            
            { PkgType.GameState, 0 },            
            { PkgType.Score, 0 }
        };

        public NetClient(Play game)
        {
            Game = game;
        }

        public byte Initialize(XNA_GUI_Controls.TextArea status, string ip)
        {
            try
            {
                Client = new UdpClient(ip, 15000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                Client.BeginReceive(ReceiveMethod, null);

                // send Greet PKG and Wait for ACK
                UInt32 pIndex = PkgIndex[PkgType.Greet];
                byte[] index = BitConverter.GetBytes(++pIndex);
                byte[] data = new byte[] { (byte)PkgType.Greet, index[0], index[1], index[2], index[3] };
                Client.Send(data, data.Length);

                Pkg pkg = new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.Greet };
                lock (PkgQueue)
                {
                    PkgQueue.Add(pkg);
                    PkgIndex[PkgType.Greet] = pIndex;
                }

                // wait untill either connected or timeout
                while (true)
                {
                    if (connected)
                        return 0;
                    else if (rejected)
                        return 2;
                    else if ((DateTime.Now - pkg.Time).TotalMilliseconds > 3000)
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
        }

        public void Dispose()
        {
            try
            {
                if (connected)
                {
                    Client.Send(new byte[] { (byte)PkgType.Disconnect }, 1);
                    connected = false;
                }
                Client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        public void Update(GameTime gameTime)
        {
            DateTime now = DateTime.Now;
            lock (PkgQueue)
            {
                for (int i = PkgQueue.Count - 1; i >= 0; i--)
                {
                    Pkg pkg = PkgQueue[i];
                    UInt32 pkgTypeIndx = PkgIndex[pkg.Type];
                    if (pkg.Index < pkgTypeIndx)
                    {
                        PkgQueue.RemoveAt(i);
                    }
                    else if (pkg.SendCount < 5 && (now - pkg.Time).TotalMilliseconds >= 100)
                        Resend(pkg);
                }
            }

            if (connected && (now - lastPing).TotalMilliseconds > 500)
            {
                PingClient();
                lastPing = now;
            }
        }
        public void Resend(Pkg pkg)
        {
            try
            {
                Client.Send(pkg.Data, pkg.Data.Length);
                if (pkg.Type != PkgType.Ping)
                    pkg.Time = DateTime.Now;
                pkg.SendCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        void ReceiveMethod(IAsyncResult result)
        {
            byte[] data = new byte[0];
            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 15000);
            try
            {
                data = Client.EndReceive(result, ref RemoteIpEndPoint);
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionReset && connected)
                {
                    Disconnect("Lost connection to server");
                }
            }
            catch { } 

            try
            {
                DateTime now = DateTime.Now;
                Client.BeginReceive(ReceiveMethod, null);
                if (data.Length > 0)
                {
                   // Console.WriteLine("[{1}] {0}", (PkgType)data[0], RemoteIpEndPoint.Address);
                    
                    switch ((PkgType)data[0])
                    {
                        case PkgType.ACK:
                            {
                                if (data.Length != 6)
                                    break;
                                PkgType ackType = (PkgType)data[1];
                                UInt32 ackIndx = BitConverter.ToUInt32(data, 2);
                               // Console.WriteLine("[ACK] {0} Id:{1}", ackType.ToString(), ackIndx);
                                lock (PkgQueue)
                                {
                                    for (int i = PkgQueue.Count - 1; i >= 0; i--)
                                    {
                                        Pkg pkg = PkgQueue[i];
                                        if (pkg.Type == ackType && pkg.Index == ackIndx)
                                        {
                                            if (pkg.Type == PkgType.Ping)
                                            {
                                                for (int j = 1; j < ping.Length; j++)
                                                {
                                                    ping[j - 1] = j;
                                                }
                                                ping[ping.Length - 1] = (int)(now - pkg.Time).TotalMilliseconds;
                                            }
                                            else if (pkg.Type == PkgType.Greet)
                                            {
                                                // conected to server, server will send lvl info to finish connecting
                                                connected = true;
                                                remote = RemoteIpEndPoint;
                                            }
                                            PkgQueue.RemoveAt(i);
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        case PkgType.Ping:
                            {
                                if (data.Length == 5)                                
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                break;
                            }
                        case PkgType.LevelInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint))
                                {
                                    UInt32 pckIndex = BitConverter.ToUInt32(data, 1);
                                    if (pckIndex > RecIndex[PkgType.LevelInfo])
                                    {
                                        RecIndex[PkgType.LevelInfo] = pckIndex;
                                        Game.Lives = data[5];
                                        Game.score = BitConverter.ToInt32(data, 6);
                                        byte[] lvldata = new byte[data.Length - 10];
                                        Array.Copy(data, 10, lvldata, 0, lvldata.Length);
                                        Game.LevelFromBytes(lvldata);
                                        Game.State = Play.PlayState.Playing;
                                        if (Main.Audio.State == 2)
                                            Main.Audio.NextSong();
                                        else
                                            Main.Audio.State = 2;
                                        Game.NextBG();
                                        Game.gui.ClearChildren();
                                    }
                                    // send back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);                                    
                                }
                                break;
                            }
                        case PkgType.BallInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && Game.level != null)
                                {                                    
                                    int index = 1;
                                    UInt32 pckIndx = BitConverter.ToUInt32(data, index);
                                    if (pckIndx > RecIndex[PkgType.BallInfo])
                                    {
                                        RecIndex[PkgType.BallInfo] = pckIndx;
                                        index += 4;
                                        byte ballcount = data[index++];
                                        Ball[] balls = new Ball[ballcount];
                                        int[] ballCollisionIndices = new int[ballcount];

                                        for (int i = 0; i < ballcount; i++)
                                        {
                                            byte type = data[index++];
                                            bool touched = BitConverter.ToBoolean(data, index++);
                                            float size = BitConverter.ToSingle(data, index);
                                            index += 4;
                                            float speed = BitConverter.ToSingle(data, index);
                                            index += 4;
                                            Vector2 pos = new Vector2(1f - BitConverter.ToSingle(data, index), 1f - BitConverter.ToSingle(data, index + 4)); // read and invert
                                            index += 8;
                                            Vector2 vel = new Vector2(BitConverter.ToSingle(data, index), BitConverter.ToSingle(data, index + 4)) * -1; // read and invert
                                            index += 8;
                                            pos += vel * speed * Ping / 2000f; // modify position by half ping time to compensate for lag
                                            Ball ball = new Ball() { Index = (byte)i, Position = pos, Velocity = vel, Size = size, Type = type, Speed = speed, TouchedLast = !touched };
                                            ball.NoCollisionPaddle = BitConverter.ToBoolean(data, index++);

                                            // ball no-collision data, save index for procesing
                                            ballCollisionIndices[i] = index;
                                            int count = BitConverter.ToInt32(data, index);
                                            index += 4;
                                            index += count; // skip ahead

                                            count = BitConverter.ToInt32(data, index);
                                            index += 4;
                                            for (int b = 0; b < count; b++)
                                            {
                                                int brickID = BitConverter.ToInt32(data, index);
                                                index += 4;
                                                ball.NoCollisionBrick.Add(Game.level.Blocks[brickID]);
                                            }
                                            balls[i] = ball;
                                        }

                                        // now process balls no-collision data;
                                        for (int i = 0; i < ballcount; i++)
                                        {                            
                                            int dataIndex = ballCollisionIndices[i];
                                            int count = BitConverter.ToInt32(data, dataIndex);
                                            dataIndex += 4;
                                            for (int b = 0; b < count; b++)
                                            {
                                                int ballIndex = data[dataIndex++];
                                                balls[i].NoCollisionBall.Add(balls[ballIndex]);
                                            }
                                        }

                                        Game.Balls = balls;
                                    }
                                    // sent back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                }
                                break;
                            }
                        case PkgType.PaddleInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && data.Length == 12)
                                {
                                    UInt32 pckIndex = BitConverter.ToUInt32(data, 1);
                                    byte paddleID = data[5];

                                    if (paddleID == 0) // invert paddle ids
                                        paddleID = 1;
                                    else
                                        paddleID = 0;

                                    if (paddleID == 0)
                                    {
                                        // sent back ACK
                                        Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                        if (pckIndex > RecIndex[PkgType.PaddleInfo])
                                            RecIndex[PkgType.PaddleInfo] = pckIndex;
                                        else
                                            break;
                                    }
                                    if (Game.Paddles[paddleID] == null)
                                        Game.Paddles[paddleID] = new Paddle(0);
                                    Game.Paddles[paddleID].Position = 1f - BitConverter.ToSingle(data, 6); // invert position
                                    Game.Paddles[paddleID].Length = data[10];
                                    Game.Paddles[paddleID].Color = data[11];
                                }
                                break;
                            }
                        case PkgType.BrickInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && Game.level != null) // if level data havent been received yet, dont send ACK and server will resend pkg.
                                {
                                    UInt32 pckIndex = BitConverter.ToUInt32(data, 1);
                                    // apply only the latest brick info
                                    if (pckIndex > RecIndex[PkgType.BrickInfo]) 
                                    {
                                        RecIndex[PkgType.BrickInfo] = pckIndex;
                                        int index = BitConverter.ToInt32(data, 5);
                                        // extract x y coordinates from index
                                        int x = index % Game.level.Width;
                                        int y = index / Game.level.Width;
                                        // invert x y
                                        index = (Game.level.Width - 1 - x) + (Game.level.Height - 1 - y) * Game.level.Width;
                                        byte str = data[9];
                                        byte color = data[10];
                                        if (str == 0)
                                            Game.level.Blocks[index] = null;
                                        else if (Game.level.Blocks[index] == null)
                                            Game.level.Blocks[index] = new Brick(index, color, str);
                                        else
                                        {
                                            Game.level.Blocks[index].Color = color;
                                            Game.level.Blocks[index].Strength = str;
                                        }
                                    }
                                    // send back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                }
                                break;
                            }
                        case PkgType.LifeInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && data.Length == 6)
                                {
                                    UInt32 pkgIndex = BitConverter.ToUInt32(data, 1);
                                    if (pkgIndex > RecIndex[PkgType.LifeInfo])
                                    {
                                        RecIndex[PkgType.LifeInfo] = pkgIndex;
                                        Game.Lives = data[5];
                                    }
                                    // send back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);                
                                }
                                break;
                            }
                        case PkgType.DropInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint))
                                {
                                    int index = 1;
                                    UInt32 pckIndx = BitConverter.ToUInt32(data, index);
                                    if (pckIndx > RecIndex[PkgType.DropInfo])
                                    {
                                        RecIndex[PkgType.DropInfo] = pckIndx;
                                        index += 4;
                                        byte dropcount = data[index++];
                                        List<Drop> drops = new List<Drop>();
                                        for (int i = 0; i < dropcount; i++)
                                        {
                                            byte type = data[index++];
                                            Vector2 pos = new Vector2(1f - BitConverter.ToSingle(data, index), 1f - BitConverter.ToSingle(data, index + 4)); // read and invert
                                            index += 8;
                                            Vector2 vel = new Vector2(BitConverter.ToSingle(data, index), BitConverter.ToSingle(data, index + 4)) * -1; // read and invert
                                            index += 8;
                                            pos += vel * Ping / 2000f; // modify position by half ping time to compensate for lag
                                            drops.Add(new Drop() { Type = (DropType)type, Position = pos, Velocity = vel });
                                        }
                                        Game.Drops = drops;
                                    }
                                    // sent back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                }
                                break;
                            }

                        case PkgType.Disconnect:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint))
                                {
                                    Disconnect("Server has disconnected");
                                }
                                break;
                            }

                        case PkgType.GameState:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && data.Length == 6)
                                {
                                    UInt32 pkgIndex = BitConverter.ToUInt32(data, 1);
                                    if (pkgIndex > RecIndex[PkgType.GameState])
                                    {
                                        RecIndex[PkgType.GameState] = pkgIndex;
                                        Game.State = (Play.PlayState)data[5];
                                    }
                                    // sent back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                }
                                break;
                            }
                        case PkgType.Score:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && data.Length == 9)
                                {
                                    UInt32 pkgIndex = BitConverter.ToUInt32(data, 1);
                                    if (pkgIndex > RecIndex[PkgType.Score])
                                    {
                                        RecIndex[PkgType.Score] = pkgIndex;
                                        Game.score = BitConverter.ToInt32(data, 5);
                                    }
                                    // sent back ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6);
                                }
                                break;
                            }
                        case PkgType.Reject:
                            {
                                if (!connected)
                                {
                                    rejected = true;
                                }
                                break;
                            }
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionReset && connected)
                {
                    Disconnect("Lost connection to server");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        public void SendPaddleData()
        {
            if (connected)
            {
                byte[] data = new byte[5];
                //header
                data[0] = (byte)PkgType.PaddleInfo;
                //data
                Array.Copy(BitConverter.GetBytes(Game.Paddles[0].Position), 0, data, 1, 4);
                try
                {
                    Client.Send(data, data.Length);
                }
                catch (Exception ex) 
                {
                    Disconnect("Lost connection to server");
                }
            }   
        }
        public void SendLvlRequest()
        {
            if (connected)
            {
                UInt32 pIndex = PkgIndex[PkgType.LevelInfo];
                byte[] index = BitConverter.GetBytes(++pIndex);
                byte[] data = new byte[] { (byte)PkgType.LevelInfo, index[0], index[1], index[2], index[3] }; 
                try
                {
                    Client.Send(data, data.Length);
                }
                catch (Exception ex)
                {
                    Disconnect("Lost connection to server");
                }
                lock (PkgQueue)
                {
                    PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.LevelInfo });
                    PkgIndex[PkgType.LevelInfo] = pIndex;
                }
            }
        }

        public void PingClient()
        {
            if (connected)
            {
                UInt32 pIndex = PkgIndex[PkgType.Ping];
                byte[] index = BitConverter.GetBytes(++pIndex);
                byte[] data = new byte[] { (byte)PkgType.Ping, index[0], index[1], index[2], index[3] }; 
                try
                {
                    Client.Send(data, data.Length);
                }
                catch (Exception ex)
                {
                    Disconnect("Lost connection to server");
                }
                lock (PkgQueue)
                {
                    PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.Ping });
                    PkgIndex[PkgType.Ping] = pIndex;
                }
            }
        }
        public void Disconnect(string msg)
        {
            Dispose();
            Game.level = null;
            Game.gui.ClearChildren();
            int w2 = Game.GraphicsDevice.Viewport.Width / 2;
            var text = Game.gui.AddTextArea("txt", msg, new Rectangle(w2 - 100, Game.GraphicsDevice.Viewport.Height / 2, 200, 32));
            text.Color = Color.NavajoWhite;

            Game.gui.AddButton("main", "Back to main", new Rectangle(w2 - 128, Game.GraphicsDevice.Viewport.Height - 64, 256, 40), Game.OnClick);
        }
    }
}
