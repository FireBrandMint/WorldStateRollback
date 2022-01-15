using Godot;
using System;
using System.Collections.Generic;

public static class ClientHandleData
{
    public static Dictionary<int, Action<PacketBuffer>> Packets = new Dictionary<int, Action<PacketBuffer>>(){
        {PacketType.SyncResponse, SyncReceived}
    };

    private static void SyncReceived (PacketBuffer buff)
    {
        int PastTick = buff.ReadInteger();
        int ServerDelayedTick = buff.ReadInteger();
        int SyncID = buff.ReadInteger();
        GClient.instance.ReceiveSync(PastTick, ServerDelayedTick, SyncID);
    }

    public static void HandleData (byte[] data)
    {
        using (PacketBuffer buff = new PacketBuffer(data))
        {
            try
            {
                while(!buff.DoneReading()) Packets[buff.ReadInteger()].Invoke(buff);
            }
            catch (Exception e)
            {
                GD.Print(e.Message);
                GD.Print(e.StackTrace);
            }
        }
    }
}
