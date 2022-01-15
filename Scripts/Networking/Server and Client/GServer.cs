using Godot;
using System;
using System.Collections.Generic;
using System.Threading;

public class GServer : Node
{
    public static GServer instance;
    
    private HybridServer Socket;

    private List<Client> Clients;

    public int NextTick = 0;

    long FirstNanos = 0;

    public override void _Ready()
    {
        Socket = new HybridServer(InitInfo.Key, InitInfo.Value);
        Clients = new List<Client>();
        FirstNanos = GMath.NanoTime();
        System.Threading.Thread.Sleep(37);

        instance = this;
    }

    public override void _PhysicsProcess(float delta)
    {
        HandleClientsConnected();
        
        TickClients();

        //SimulateStabilize();

        ReadMessages();

        DisconnectSpiredClients();
        
        Simulate(NextTick);
        ++NextTick;
    }

    private void HandleClientsConnected ()
    {
        var connected = Socket.GetClientsConnected();

        if (connected != null)
        {
            for (int i = 0; i<connected.Length; ++i)
            {
                ClientConnected(connected[i]);
            }
        }
    }

    private void ClientConnected (int i)
    {
        Clients.Add(new Client(i));
    }

    private void TickClients()
    {
        for (int i = 0; i< Clients.Count; ++i) Clients[i].Tick();
    }

    int framesSucessful = 0;

    private void SimulateStabilize ()
    {
        int nt = NextTick;

        int difference = (int)((GMath.NanoTime() - FirstNanos)/(1000000000 / Engine.IterationsPerSecond)) - nt;

        if (difference > 0)
        {
            GD.Print($"Detected slowdown by {difference} ticks, fast forwarding. {framesSucessful} frames were sucessful before this.");
            GD.Print($"Millis: {GMath.NanoTime()}");
            for (int i = 0; i<difference; ++i)
            {
                Simulate(nt - (difference - i));
            }

            NextTick += difference;
            framesSucessful = 0;
        }
        else if (difference < 0)
        {
            GD.Print($"The frame is ahead of time by {difference}, backtracking. {framesSucessful} frames were sucessful before this.");
            
            System.Threading.Thread.Sleep((-difference) * (-1 + 1000/ Engine.IterationsPerSecond));
            NextTick += difference;
            framesSucessful = 0;
        }
        else
        {
            ++framesSucessful;
        }
    }

    private void ReadMessages ()
    {
        ClientMessage message = Socket.GetMessage();

        while (message != null)
        {
            ServerHandleData.HandleData(message.clientID, message.message);

            message = Socket.GetMessage();
        }
    }

    private void DisconnectSpiredClients ()
    {
        int[] disconnected = Socket.GetClientsDisconnected();

        if (disconnected != null)
        for (int i = 0; i<disconnected.Length; ++i)
        {
            CloseClient(disconnected[i]);
        }
    }

    //test dummy for simulate
    //public static TestP testP;

    private void Simulate (int tick)
    {
        
    }

    private void CloseClient (int ID)
    {
        for (int i = 0; i<Clients.Count; ++i)
        {
            if (Clients[i].ID == ID)
            {
                Clients[i].OnClose();
                Clients.RemoveAt(i);
                Socket.CloseClient(i);
                GD.Print($"Client{ID} disconnected!");
                break;
            }
        }
    }

    public void SendTCP(int ID, PacketBuffer data)
    {
        if (! Socket.SendTo(ID, data.ToArray()) ) CloseClient(ID);
    }

    public void SendUDP(int ID, PacketBuffer data)
    {
        if (! Socket.SendToUDP(ID, data.ToArray()) ) CloseClient(ID);
    }

    //Prepares to initialize server before it's instanced
    public static void Initialize (string ip, int port)
    {
        InitInfo = new KeyValuePair<string, int>(ip, port);
    }

    public static KeyValuePair<string, int> InitInfo;
}

public class Client
{
    public int ID;

    public Client (int _ID)
    {
        ID = _ID;
    }

    public void Tick()
    {

    }

    public void OnClose()
    {

    }
}
