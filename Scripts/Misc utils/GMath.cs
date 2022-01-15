using Godot;
using System;
using System.Diagnostics;
using System.Collections.Generic;

public static class GMath
{
    public static long NanoTime() {
        long nano = 10000L * Stopwatch.GetTimestamp();
        nano /= TimeSpan.TicksPerMillisecond;
        nano *= 100L;
        return nano;
    }

    public static int MiddleMan (List<int> list)
    {
        return 0;
    }
}
