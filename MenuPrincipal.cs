using Godot;
using System;

public class MenuPrincipal : Control
{
    //a
    string ServerScenePath = "res://Scenes/Server test scene.tscn";

    string ClientScenePath = "res://Scenes/Client test scene.tscn";

    public override void _Ready()
    {

    }

    public void StartServer()
    {
        GServer.Initialize(GetNode<TextEdit>("IP").Text, GetNode<TextEdit>("PORT").Text.ToInt());

        GetTree().ChangeScene(ServerScenePath);
    }

    public void StartClient()
    {
        GClient.Initialize(GetNode<TextEdit>("IP").Text, GetNode<TextEdit>("PORT").Text.ToInt());

        GetTree().ChangeScene(ClientScenePath);
    }
}
