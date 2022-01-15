using Godot;
using System;

public class TestTickSync : Label
{
    //subject sever or client
    Node Subject;
    public override void _Ready()
    {
        Subject = GetParent();
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Subject is GServer server) Text = server.NextTick.ToString();
        else Text = ((GClient)Subject).NextTick().ToString();
    }
}
