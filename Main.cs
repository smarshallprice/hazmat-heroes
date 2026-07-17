using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class Main : Node
{
    private readonly PackedScene _playerScene = GD.Load<PackedScene>("uid://13coq8nj1c80");

    private MultiplayerSpawner _multiplayerSpawner;
    private Marker2D _playerSpawnPosition;
    private EnemyManager _enemyManager;

    public override void _Ready()
    {
        _multiplayerSpawner = GetNode<MultiplayerSpawner>("MultiplayerSpawner");
        _playerSpawnPosition = GetNode<Marker2D>("PlayerSpawnPosition");
        _enemyManager = GetNode<EnemyManager>("EnemyManager");

        _multiplayerSpawner.SpawnFunction = Callable.From<Variant, Node>(SpawnPlayer);
        RpcId(1, MethodName.PeerReady);
    }

    private Node SpawnPlayer(Variant data)
    {
        Godot.Collections.Dictionary spawnData = data.As<Godot.Collections.Dictionary>();
        int peerId = spawnData["peer_id"].AsInt32();

        Player player = _playerScene.Instantiate<Player>();
        player.Name = peerId.ToString();
        player.InputMultiplayerAuthority = peerId;
        player.GlobalPosition = _playerSpawnPosition.GlobalPosition;
        GD.Print($"Spawning player for peer {peerId} at position {player.GlobalPosition}");
        return player;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PeerReady()
    {
        int senderId = Multiplayer.GetRemoteSenderId();
        GD.Print($"peer {senderId} ready");
        _multiplayerSpawner.Spawn(new Godot.Collections.Dictionary { { "peer_id", senderId } });
        _enemyManager.Synchronize(senderId);
    }
}
