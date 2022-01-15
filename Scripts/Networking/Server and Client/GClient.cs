using Godot;
using System;
using System.Collections.Generic;

public class GClient : Node
{
    public static GClient instance;

    private HybridClient Socket;

    public int NextTick () => LocalFrameNext + ServerTickAdvantage;
    
    public int LocalFrameNext = 0;

    public int ServerTickAdvantage = 0;
    public override void _Ready()
    {
        Socket = new HybridClient(InitInfo.Key, InitInfo.Value);
        instance = this;
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Input.IsActionJustPressed("ui_up"))
        {
            ServerTickAdvantage = 0;
            SyncsPerformed = 0;
        }

        ReadMessages();

        SendSync();

        ++LocalFrameNext;
    }

    private void ReadMessages()
    {
        byte[] message = Socket.GetMessage();

        while (message !=null)
        {
            ClientHandleData.HandleData(message);
            message = Socket.GetMessage();
        }
    }

    //Defines the interval between sync messages.
    double SyncRequestIntervalSec = 0.25;

    //Defines the amount of sync tries
    int SyncLimit = 20;

    int IntervalSyncReq = int.MaxValue;

    int SyncTicket = 0;
    private void SendSync ()
    {
        if (SyncsPerformed >= SyncLimit) return;

        if (IntervalSyncReq >= SyncRequestIntervalSec * Engine.IterationsPerSecond)
        {

            using (PacketBuffer buff = new PacketBuffer())
            {
                buff.WriteInteger(PacketType.SyncTick);
                buff.WriteInteger(LocalFrameNext);
                buff.WriteInteger(SyncTicket);
                SendUDP(buff);
            }

            IntervalSyncReq = 0;
            ++SyncTicket;

            //GD.Print("Sent sync request!");
        }

        ++IntervalSyncReq;
    }
    int SyncsPerformed = 0;

    //this is for handling cloned messages
    int LastSvrDelayedTick = -1;

    int LastSyncId = -1;

    private List<int> STests = new List<int>();

    public void ReceiveSync (int PastTick, int ServerDelayedTick, int SyncID)
    {
        //Deals with late packets and duplicates
        if (SyncsPerformed >= SyncLimit || LastSvrDelayedTick >= ServerDelayedTick) return;
        if (SyncID > LastSyncId + 1)
        {
            LastSyncId = SyncID;
            return;
        }
        LastSvrDelayedTick = ServerDelayedTick;

        int delay = (int)((LocalFrameNext - PastTick)*0.5);
        int serverAproxTick = ServerDelayedTick + delay;

        ServerTickAdvantage = serverAproxTick - LocalFrameNext;
        STests.Add(ServerTickAdvantage);

        ++SyncsPerformed;

        LastSyncId = SyncID;

        if (SyncsPerformed>= SyncLimit)
        {
            List<List<int>> groups = new List<List<int>>();

            for (int i = 0; i< STests.Count; ++i)
            {
                if (i< STests.Count-1)
                {
                    List<int> results = null;

                    int r = STests[i];
                    for (int o = i+1; o < STests.Count; ++o)
                    {
                        if(STests[o] > r-4 && STests[o] < r+4)
                        {
                            if (results==null) results = new List<int>();
                            results.Add(STests[o]);
                            STests.RemoveAt(o);
                            --o;
                        }
                    }

                    if (results!=null) groups.Add(results);
                }
            }

            int finalServerAdvResult = 0;

            if (groups.Count == 0) finalServerAdvResult = STests[0];
            else
            {
                int selectedGroup = -1;
                int selectedGroupCount = -1;


                for (int i = 0; i< groups.Count; ++i)
                {
                    var g = groups[i];
                    if (g.Count > selectedGroupCount)
                    {
                        selectedGroup = i;
                        selectedGroupCount = g.Count;
                    }
                }
                var selectedGObj = groups[selectedGroup];
                
                for (int i = 0; i< selectedGObj.Count; ++i)
                {
                    if (selectedGObj[i] > finalServerAdvResult)
                    finalServerAdvResult = selectedGObj[i];
                }
            }

            ServerTickAdvantage = finalServerAdvResult;

            GD.Print("Sync COMPLETED!");
        }
    }

    public void SendTCP (PacketBuffer buff)
    {
        Socket.Send(buff.ToArray());
    }

    public void SendUDP (PacketBuffer buff)
    {
        Socket.SendUDP(buff.ToArray());
    }

    public static void Initialize (string ip, int port)
    {
        InitInfo = new KeyValuePair<string, int>(ip, port);
    }

    public static KeyValuePair<string, int> InitInfo;
}
