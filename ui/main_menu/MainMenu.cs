using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class MainMenu : Control
{
    private const int Port = 3000;

    private readonly PackedScene _mainScene = GD.Load<PackedScene>("uid://dnhh42ul1davo");

    private Button _hostButton;
    private Button _joinButton;

    public override void _Ready()
    {
        _hostButton = GetNode<Button>("HBoxContainer/HostButton");
        _joinButton = GetNode<Button>("HBoxContainer/JoinButton");

        _hostButton.Pressed += OnHostPressed;
        _joinButton.Pressed += OnJoinPressed;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
    }

    public override void _ExitTree()
    {
        _hostButton.Pressed -= OnHostPressed;
        _joinButton.Pressed -= OnJoinPressed;
        Multiplayer.ConnectedToServer -= OnConnectedToServer;
    }

    private void OnHostPressed()
    {
        var serverPeer = new ENetMultiplayerPeer();
        serverPeer.CreateServer(Port);
        Multiplayer.MultiplayerPeer = serverPeer;
        GetTree().ChangeSceneToPacked(_mainScene);
    }

    private void OnJoinPressed()
    {
        var clientPeer = new ENetMultiplayerPeer();
        clientPeer.CreateClient("127.0.0.1", Port);
        Multiplayer.MultiplayerPeer = clientPeer;
    }

    private void OnConnectedToServer()
    {
        GetTree().ChangeSceneToPacked(_mainScene);
    }
}
