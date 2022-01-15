using Godot;
using System;
using System.Collections.Generic;

public static class ServerHandleData
{
    public static Dictionary<int, Action<int, PacketBuffer>> Packets = new Dictionary<int, Action<int, PacketBuffer>>(){
        {PacketType.SyncTick, SyncTick},
    };

    static void SyncTick (int ID, PacketBuffer buff)
    {
        GD.Print("Message was sync tick!");
        
        int cTick = buff.ReadInteger();
        int syncID = buff.ReadInteger();

        using (PacketBuffer buff2 = new PacketBuffer())
        {

            buff2.WriteInteger(PacketType.SyncResponse);

            buff2.WriteInteger(cTick);

            buff2.WriteInteger(GServer.instance.NextTick);

            buff2.WriteInteger(syncID);
            
            GServer.instance.SendUDP(ID, buff2);
        }
    }

    public static void HandleData (int clientID, byte[] data)
    {
        GD.Print("Message received!");

        using (PacketBuffer buff = new PacketBuffer(data))
        {
            try
            {
                while(!buff.DoneReading()) Packets[buff.ReadInteger()].Invoke(clientID, buff);
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
                GD.Print(e.StackTrace);
            }
        }
    }
}
