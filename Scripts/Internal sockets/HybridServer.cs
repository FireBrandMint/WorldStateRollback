using Godot;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

public class HybridServer
{
    private List<HSClient> clientSockets = new List<HSClient>();
    private Dictionary<int, HSClient> dClientSockets = new Dictionary<int, HSClient>();

    List<int> RecentlyConnectedCash = new List<int>();

    List<int> RecentlyDisconnectedCash = new List<int>();

    Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

    Dictionary <IPEndPoint, int> EndpointToId = new Dictionary<IPEndPoint, int>();

    ConcurrentBag<ClientMessage> clientMessages = new ConcurrentBag<ClientMessage>();

    UdpClient udp;

    public bool Functioning = true;

    int IdTicket = 0;

    public HybridServer (string ip, int port)
    {
        SetupServer(ip, port);
    }

    private void SetupServer (string ip, int port)
    {
        GD.Print("Setting up server...");
        try
        {
            serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            serverSocket.Listen(5);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            udp = new UdpClient(port);
            var t = new System.Threading.Thread(UDPReceiving);
            t.Start();
            GD.Print("Server setup!");
        }
        catch (Exception e)
        {
            GD.Print("Server couldn't setup due to an exception!");
            GD.Print(e.Message);
            Functioning = false;
        }
    }

    //NECESSARY: returns clients recently connected.
    public int[] GetClientsConnected ()
    {
        lock (RecentlyConnectedCash)
        {
            bool clientConnected = RecentlyConnectedCash.Count > 0;
            int[] ids = RecentlyConnectedCash.ToArray();
            if (clientConnected) RecentlyConnectedCash.Clear();
            return clientConnected ? ids : null;
        }
    }

    public int[] GetClientsDisconnected ()
    {
        lock (RecentlyDisconnectedCash)
        {
            bool clientConnected = RecentlyDisconnectedCash.Count > 0;
            int[] ids = RecentlyConnectedCash.ToArray();
            if (clientConnected) RecentlyDisconnectedCash.Clear();
            return clientConnected ? ids : null;
        }
    }

    public bool SendTo (int id, byte[] message)
    {
        try
        {
            if (!dClientSockets.ContainsKey(id)) return false;
            var sock = dClientSockets[id].socket;
            if (!sock.Connected) return false;
            Int32 sizee = message.Length;
            sock.Send(BitConverter.GetBytes(sizee));
            sock.Send(message);
            return true;
        }
        catch
        {
            //GD.Print(e.Message);
            //GD.Print(e.StackTrace);
            return false;
        }
    }

    public bool SendToUDP (int id, byte[] message)
    {
        try
        {
            if (!dClientSockets.ContainsKey(id)) return false;

            udp.Send(message, message.Length, (IPEndPoint)dClientSockets[id].socket.RemoteEndPoint);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Broadcast (byte[] message)
    {
        Int32 sizee = message.Length;
        var mlenght = BitConverter.GetBytes(sizee);
        for (int i = 0; i< clientSockets.Count; ++i)
        {
            try
            {
                var sock = clientSockets[i].socket;
                sock.Send(mlenght);
                sock.Send(message);
            }
            catch
            {
                //GD.Print(e.Message);
                //GD.Print(e.StackTrace);
            }
        }
    }

    public void BroadcastUDP (byte[] message)
    {
        for (int i = 0; i< clientSockets.Count; ++i)
        {
            var client = clientSockets[i];
            try
            {
                udp.Send(message, message.Length, (IPEndPoint)client.socket.RemoteEndPoint);
            }
            catch
            {}
        }
    }

    public int MessageCount () => clientMessages.Count;

    public ClientMessage GetMessage ()
    {
        ClientMessage message;
        if (clientMessages.TryTake(out message)) return message;
        return null;
    }

    public void CloseClient (int ID)
    {
        if (!dClientSockets.ContainsKey(ID))
        {
            for (int i = 0; i< clientSockets.Count; ++i)
            {
                var cclient = clientSockets[i];
                if (cclient.ID == ID)
                {
                    clientSockets.RemoveAt(i);
                    lock (EndpointToId) EndpointToId.Remove((IPEndPoint) cclient.socket.RemoteEndPoint);
                    cclient.Close();
                    GD.Print($"Sucessfuly removed client {ID}");
                    break;
                }
            }
            return;
        }
        var client = dClientSockets[ID];
        dClientSockets.Remove(ID);
        clientSockets.Remove(client);
        lock (EndpointToId) EndpointToId.Remove((IPEndPoint) client.socket.RemoteEndPoint);
        client.Close();
        GD.Print($"Sucessfuly removed client {ID}");
    }

    public void CloseAllClients ()
    {
        var clients = clientSockets.ToArray();
        for (int i = 0; i<clients.Length; ++i)
        {
            CloseClient(clients[i].ID);
        }
    }

    public void Close ()
    {
        CloseAllClients();
        serverSocket.Close();
    }

    private void AcceptCallback(IAsyncResult ar)
    {
        try
        {
            Socket socket = serverSocket.EndAccept(ar);

            var a = socket.IsBound;

            int ticket = IdTicket;
            IdTicket++;

            var client = new HSClient(socket, ticket, clientMessages, WhenClientDisconnected);

            clientSockets.Add(client);
            dClientSockets.Add(ticket, client);
            lock (EndpointToId) EndpointToId.Add((IPEndPoint)(socket.RemoteEndPoint), ticket);
            lock (RecentlyConnectedCash) RecentlyConnectedCash.Add(client.ID);
            serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
            
        }
        catch (Exception e)
        {
            GD.Print(e.Message);
            GD.Print(e.StackTrace);
        }
    }
    
    private void WhenClientDisconnected(int ID)
    {
        lock (RecentlyDisconnectedCash)
        {
            RecentlyDisconnectedCash.Add(ID);
        }
    }

    void UDPReceiving ()
    {
        while (Functioning)
        {
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                byte[] message = udp.Receive(ref RemoteIpEndPoint);
                //GD.Print($"Received message from {RemoteIpEndPoint.ToString()}!");
                lock (EndpointToId)
                {
                    if (EndpointToId.ContainsKey(RemoteIpEndPoint) && message.Length != 0)
                    {
                        clientMessages.Add(new ClientMessage(EndpointToId[RemoteIpEndPoint], message));
                        //GD.Print($"Message just received was from client {EndpointToId[RemoteIpEndPoint]}");
                    }
                    /*else
                    {
                        GD.Print("Couldn't find client that sent last message...");
                    }*/
                }
            }
            catch {}
        }
    }

    private class HSClient
    {
        public Socket socket;
        public int ID;
        ConcurrentBag<ClientMessage> bag;
        Action<int> DisconnectedLogging;
        byte[] buffer = new byte[1024];

        bool functioning = true;

        System.Threading.Thread ReceivingThread;

        public bool SleepClient = false;

        public int SleepMs = 1;

        public HSClient (Socket _socket, int _ID, ConcurrentBag<ClientMessage> _bag, Action<int> dLogging)
        {
            socket = _socket;
            ID = _ID;
            bag = _bag;
            DisconnectedLogging = dLogging;
            //socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            var ReceivingThread = new System.Threading.Thread(Receive);
            ReceivingThread.Priority = ThreadPriority.Lowest;
            ReceivingThread.Start();
        }

        private void ReceiveCallback (IAsyncResult ar)
        {
            int received = socket.EndReceive(ar);
            byte[] dataBuf = new byte[received];
            Array.Copy(buffer, dataBuf, received);
            socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            bag.Add(new ClientMessage(ID, dataBuf));
        }

        private void Receive ()
        {
            while(functioning)
            {
                /*if (socket.Available == 0)
                {
                    /*if (SleepClient)
                    {
                        SleepClient = false;
                        System.Threading.Thread.Sleep(SleepMs);
                    }
                    
                    continue;
                }*/
                try
                {
                    byte[] rec = new byte[4];

                    socket.Receive(rec);

                    if (rec[0] == 0 && rec[1] == 0 && rec[2] == 0 && rec[3] == 0) throw(new Exception("Disconnected exception!"));

                    int toReceive = BitConverter.ToInt32(rec, 0);

                    if (toReceive > 2048) continue;

                    var buffer = new byte[toReceive];

                    socket.Receive(buffer);
                    bag.Add(new ClientMessage(ID, buffer));
                }
                catch 
                {
                    DisconnectedLogging.Invoke(ID);
                    break;
                }
            }
        }

        public void Close()
        {
            functioning = false;
            socket.Close();
        }
    }
}

public class ClientMessage
{
    public int clientID;
    public byte[] message;
    public ClientMessage (int _clientID, byte[] _message)
    {
        clientID = _clientID;
        message = _message;
    }
}

