using Godot;
using System;
using System.Collections.Generic;

public static class PacketType
{
    //Packets the server receives

    public const int SyncTick = 0,
    TickSynced = 1;

    //Packets the client receives

    public const int SyncResponse = 1000;
}
