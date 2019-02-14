using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Xna.Framework;

namespace Block_Buster
{
    enum PkgType
    {
        Greet = 0,
        PlayerInfo = 1,
        LevelInfo = 2,
        BallInfo = 3,
        PaddleInfo = 4,
        Disconnect = 5,
        BrickInfo = 6,
        LifeInfo = 7,
        DropInfo = 8,
        Ping = 10,
        ServerInfo = 11,
        GameState = 12,
        Score = 13,
        Reject = 254,
        ACK = 255
    }
    class Pkg
    {
        public PkgType Type;
        public byte[] Data;
        public DateTime Time;
        public UInt32 Index;
        public IPEndPoint IpRemote;
        public byte SendCount;
    }

    class Server
    {
        public UdpClient Client;

        public bool connected = false;
        IPEndPoint remote;
        Play Game;
        int[] ping = new int[4];
        public int Ping { get { return (ping[0] + ping[1] + ping[2] + ping[3]) / 4; } }
        DateTime lastPing = DateTime.Now;
        List<Pkg> PkgQueue = new List<Pkg>();
        Dictionary<PkgType, UInt32> PkgIndex = new Dictionary<PkgType, uint>() {
            { PkgType.Ping, 0 }, 
            { PkgType.Greet, 0 }, 
            { PkgType.BallInfo, 0 },
            { PkgType.DropInfo, 0 },
            { PkgType.BrickInfo, 0 }, 
            { PkgType.LevelInfo, 0 }, 
            { PkgType.PaddleInfo, 0 },
            { PkgType.LifeInfo, 0 },
            { PkgType.GameState, 0 },
            { PkgType.Score, 0 }
        };
        byte unAckCount = 0;

        public Server(Play game)
        {
            Game = game;
        }
        public byte Initialize(XNA_GUI_Controls.TextArea status)
        {
            try
            {
                Client = new UdpClient(15000);
                Client.BeginReceive(ReceiveMethod, null);
                Console.WriteLine("Server: listening..");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 1;
            }
            try
            {
                TcpClient client = new TcpClient(AddressFamily.InterNetwork);
                client.Connect("wv.si", 5601);
                NetworkStream stream = client.GetStream();
                stream.Write(new byte[] { 2 }, 0, 1); // register server with game lobby server

                status.Enabled = false;
                status.Text = "Checking your game's connectivity...";
                status.Enabled = true;
                Thread.Sleep(500);
                byte[] data = new byte[256];
                int bytes = stream.Read(data, 0, data.Length);
                client.Close();
                if (bytes == 2 && data[0] == 2 && data[1] == 1)
                    return 0;
                else
                    return 3;   
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return 2;
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
                    if (pkg.Index < pkgTypeIndx) // remove old package
                    {
                        PkgQueue.RemoveAt(i);
                    }
                    else if ((now - pkg.Time).TotalMilliseconds >= 100)  // re-send package
                    {
                        if (pkg.SendCount < 5)
                            Resend(pkg);
                        else
                            unAckCount++;
                    }
                }
            }
            if (connected && unAckCount > 2)
            {
                RemoveClient();
            }
            if (connected && (now - lastPing).TotalMilliseconds > 500)
            {
                PingClient();
                lastPing = now;
            }
        }
        public void Resend(Pkg pkg)
        {
            Client.Send(pkg.Data, pkg.Data.Length, pkg.IpRemote);                
            if (pkg.Type != PkgType.Ping)
                pkg.Time = DateTime.Now;
            pkg.SendCount++;            
        }
        public void Dispose()
        {
            try
            {
                TcpClient masterServer = new TcpClient("wv.si", 5601);
                NetworkStream stream = masterServer.GetStream();
                stream.Write(new byte[] { 3 }, 0, 1);
                masterServer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            } 
            try
            {
                if (connected)
                    Client.Send(new byte[] { (byte)PkgType.Disconnect }, 1, remote);
                Client.Close();
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
                    RemoveClient();
                }
            }
            catch { } 

            try
            {
                DateTime now = DateTime.Now;
                Client.BeginReceive(ReceiveMethod, null);
                if (data.Length > 0)
                {
                    switch ((PkgType)data[0])
                    {
                        case PkgType.ACK:
                            {
                                if (data.Length != 6)
                                    break;
                                PkgType ackType = (PkgType)data[1];
                                UInt32 ackIndx = BitConverter.ToUInt32(data, 2);
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
                                            else if (pkg.Type == PkgType.LevelInfo)
                                            {
                                                // CONNECTED CLIENT 
                                                connected = true;
                                                remote = RemoteIpEndPoint;
                                                Game.Paddles[1] = new Paddle(1);
                                                // SEND other data;
                                                SendGameState();
                                                SendBallData();
                                                SendPaddleData(1);
                                                SendPaddleData(0);
                                            }
                                            PkgQueue.RemoveAt(i);
                                            unAckCount = 0;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        case PkgType.Ping:
                            {
                                if (data.Length == 5)
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6, RemoteIpEndPoint);
                                break;
                            }
                        case PkgType.ServerInfo:
                            {
                                byte[] response = new byte[3];
                                response[0] = data[0];
                                response[1] = 0;
                                if (connected)
                                    response[2] = 0;
                                else
                                    response[2] = 1;
                                Client.Send(response, response.Length, RemoteIpEndPoint);
                                break;
                            }
                        case PkgType.Greet:
                            {
                                if (connected && !remote.Equals(RemoteIpEndPoint))
                                {
                                    SendReject(RemoteIpEndPoint, 0);
                                }
                                else if (data.Length == 5)
                                {
                                    // send ACK
                                    Client.Send(new byte[] { (byte)PkgType.ACK, data[0], data[1], data[2], data[3], data[4] }, 6, RemoteIpEndPoint);
                                    // Send back LvlInfo and wait for ACK to finish connecting     
                                    SendLvlData(RemoteIpEndPoint);
                                }
                                break;
                            }
                        case PkgType.PaddleInfo:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint) && data.Length == 5)
                                {
                                    Game.Paddles[1].Position = 1f - BitConverter.ToSingle(data, 1);
                                }
                                break;
                            }
                        case PkgType.Disconnect:
                            {
                                if (connected && remote.Equals(RemoteIpEndPoint))
                                {
                                    RemoveClient(); 
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
                    RemoveClient();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
        public void SendLvlData()
        {
            if (connected)
                SendLvlData(remote);
        }
        public void SendLvlData(IPEndPoint endPoint)
        {
            List<byte> data = new List<byte>();
            //header
            data.Add((byte)PkgType.LevelInfo);
            UInt32 pkgIndex = PkgIndex[PkgType.LevelInfo];
            data.AddRange(BitConverter.GetBytes(++pkgIndex));
            //data
            data.Add(Game.Lives);
            data.AddRange(BitConverter.GetBytes(Game.score));
            data.Add((byte)Game.level.Width);
            data.Add((byte)Game.level.Height);
            // invert level data
            for (int y = Game.level.Height - 1; y >= 0; y--)
            {
                for (int x = Game.level.Width - 1; x >= 0; x--)
                {
                    Brick b = Game.level.Blocks[x + y * Game.level.Width];
                    if (b == null)
                        data.Add(0);
                    else
                    {
                        data.Add(1);
                        data.Add(b.Color);
                        data.Add(b.Strength);
                    }
                }
            }
            byte[] dataArry = data.ToArray();
            try
            {
                Client.Send(dataArry, dataArry.Length, endPoint);
                lock (PkgQueue)
                {
                    PkgQueue.Add(new Pkg { Type = PkgType.LevelInfo, Time = DateTime.Now, Data = dataArry, Index = pkgIndex, IpRemote = endPoint });
                    PkgIndex[PkgType.LevelInfo] = pkgIndex;
                }
            }
            catch 
            {
                if (connected)
                    RemoveClient();
            }
        }
        public void SendBallData()
        {
            if (connected)
            {
                List<byte> data = new List<byte>();
                //header
                data.Add((byte)PkgType.BallInfo);
                UInt32 bIndex = PkgIndex[PkgType.BallInfo];
                data.AddRange(BitConverter.GetBytes(++bIndex));
                //data
                byte len = (byte)Game.Balls.Length; 
                data.Add(len);
                for (int i = 0; i < len; i++)
                {
                    Ball ball = Game.Balls[i];
                    data.Add(ball.Type);
                    data.AddRange(BitConverter.GetBytes(ball.TouchedLast));
                    data.AddRange(BitConverter.GetBytes(ball.Size));
                    data.AddRange(BitConverter.GetBytes(ball.Speed));
                    data.AddRange(BitConverter.GetBytes(ball.Position.X));
                    data.AddRange(BitConverter.GetBytes(ball.Position.Y));
                    data.AddRange(BitConverter.GetBytes(ball.Velocity.X));
                    data.AddRange(BitConverter.GetBytes(ball.Velocity.Y));
                    data.AddRange(BitConverter.GetBytes(ball.NoCollisionPaddle));
                    data.AddRange(BitConverter.GetBytes(ball.NoCollisionBall.Count));
                    foreach (Ball b in ball.NoCollisionBall)
                        data.Add(b.Index);
                    data.AddRange(BitConverter.GetBytes(ball.NoCollisionBrick.Count));
                    foreach (Brick b in ball.NoCollisionBrick)
                        data.AddRange(BitConverter.GetBytes(b.Index));

                }
                try
                {
                    byte[] dataArry = data.ToArray();
                    Client.Send(dataArry, dataArry.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Type = PkgType.BallInfo, Time = DateTime.Now, Data = dataArry, Index = bIndex, IpRemote = remote });
                        PkgIndex[PkgType.BallInfo] = bIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }
        public void SendLifeInfo()
        {
            if (connected)
            {
                byte[] data = new byte[6];
                //header
                data[0] = (byte)PkgType.LifeInfo;
                UInt32 pkgIndex = PkgIndex[PkgType.LifeInfo];
                BitConverter.GetBytes(++pkgIndex).CopyTo(data, 1);
                //data
                data[5] = Game.Lives;
                try
                {
                    Client.Send(data, data.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Type = PkgType.LifeInfo, Time = DateTime.Now, Data = data, Index = pkgIndex, IpRemote = remote });
                        PkgIndex[PkgType.LifeInfo] = pkgIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }
        public void SendPaddleData(byte paddleID = 0)
        {
            if (connected && Game.Paddles[paddleID] != null)
            {
                byte[] data = new byte[12];
                //header
                data[0] = (byte)PkgType.PaddleInfo;
                UInt32 pkgIndex = PkgIndex[PkgType.PaddleInfo];
                Array.Copy(BitConverter.GetBytes(++pkgIndex), 0, data, 1, 4);
                //data
                data[5] = paddleID;
                Array.Copy(BitConverter.GetBytes(Game.Paddles[paddleID].Position), 0, data, 6, 4);
                data[10] = Game.Paddles[paddleID].Length;
                data[11] = Game.Paddles[paddleID].Color;
                try
                {
                    Client.Send(data, data.Length, remote);
                    if (paddleID != 0)
                    {
                        lock (PkgQueue)
                        {
                            PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pkgIndex, Type = PkgType.PaddleInfo, IpRemote = remote });
                            PkgIndex[PkgType.PaddleInfo] = pkgIndex;
                        }
                    }
                }
                catch { RemoveClient(); }
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
                    Client.Send(data, data.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.Ping, IpRemote = remote });
                        PkgIndex[PkgType.Ping] = pIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }
        public void SendBrickInfo(int index)
        {
            if (connected)
            {
                List<byte> data = new List<byte>();
                //header
                data.Add((byte)PkgType.BrickInfo);
                UInt32 bIndex = PkgIndex[PkgType.BrickInfo];
                data.AddRange(BitConverter.GetBytes(++bIndex));
                //data
                Brick b = Game.level.Blocks[index];
                data.AddRange(BitConverter.GetBytes(index));
                data.Add(b.Strength);
                data.Add(b.Color);
                try
                {
                    byte[] dataArr = data.ToArray();
                    Client.Send(dataArr, dataArr.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Data = dataArr, Time = DateTime.Now, Index = bIndex, Type = PkgType.BrickInfo, IpRemote = remote });
                        PkgIndex[PkgType.BrickInfo] = bIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }

        public void SendDropData()
        {
            if (connected)
            {
                List<byte> data = new List<byte>();
                //header
                data.Add((byte)PkgType.DropInfo);
                UInt32 pkgIndex = PkgIndex[PkgType.DropInfo];
                data.AddRange(BitConverter.GetBytes(++pkgIndex));
                //data
                data.Add((byte)Game.Drops.Count);
                foreach (Drop drop in Game.Drops)
                {
                    data.Add((byte)drop.Type);
                    data.AddRange(BitConverter.GetBytes(drop.Position.X));
                    data.AddRange(BitConverter.GetBytes(drop.Position.Y));
                    data.AddRange(BitConverter.GetBytes(drop.Velocity.X));
                    data.AddRange(BitConverter.GetBytes(drop.Velocity.Y));
                }
                byte[] dataArry = data.ToArray();
                try
                {
                    Client.Send(dataArry, dataArry.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Type = PkgType.DropInfo, Time = DateTime.Now, Data = dataArry, Index = pkgIndex, IpRemote = remote });
                        PkgIndex[PkgType.DropInfo] = pkgIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }
        public void SendGameState()
        {
            if (connected)
            {
                UInt32 pIndex = PkgIndex[PkgType.GameState];
                byte[] data = new byte[6];
                data[0] = (byte)PkgType.GameState;
                Array.Copy(BitConverter.GetBytes(++pIndex), 0, data, 1, 4);
                data[5] = (byte)Game.State;
                try
                {
                    Client.Send(data, data.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.GameState, IpRemote = remote });
                        PkgIndex[PkgType.GameState] = pIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }
        public void SendScoreInfo()
        {
            if (connected)
            {
                UInt32 pIndex = PkgIndex[PkgType.Score];
                byte[] data = new byte[9];
                data[0] = (byte)PkgType.Score;
                Array.Copy(BitConverter.GetBytes(++pIndex), 0, data, 1, 4);
                Array.Copy(BitConverter.GetBytes(Game.score), 0, data, 5, 4);
                try
                {
                    Client.Send(data, data.Length, remote);
                    lock (PkgQueue)
                    {
                        PkgQueue.Add(new Pkg { Data = data, Time = DateTime.Now, Index = pIndex, Type = PkgType.Score, IpRemote = remote });
                        PkgIndex[PkgType.Score] = pIndex;
                    }
                }
                catch { RemoveClient(); }
            }
        }

        public void SendReject(IPEndPoint remoteEP, byte type)
        {
            byte[] data = new byte[2];
            data[0] = (byte)PkgType.Reject;
            data[1] = type;
            try
            {
                Client.Send(data, data.Length, remoteEP);
            }
            catch { }
        }

        public void RemoveClient()
        {
            try
            {
                Client.Send(new byte[] { (byte)PkgType.Disconnect }, 1, remote);
            }
            catch { }
            connected = false;
            remote = null;
            lock (PkgQueue)
            {
                PkgQueue.Clear();
            }
            Game.Paddles[1] = null;
        }
    }
}
