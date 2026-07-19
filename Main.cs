using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class Main : Node
{
    private readonly PackedScene _playerScene = GD.Load<PackedScene>("uid://13coq8nj1c80");

    private MultiplayerSpawner _multiplayerSpawner;
    private Marker2D _playerSpawnPosition;
    private EnemyManager _enemyManager;

    private List<int> deadPeers = new List<int>();

    public override void _Ready()
    {
        _multiplayerSpawner = GetNode<MultiplayerSpawner>("MultiplayerSpawner");
        _playerSpawnPosition = GetNode<Marker2D>("PlayerSpawnPosition");
        _enemyManager = GetNode<EnemyManager>("EnemyManager");

        _multiplayerSpawner.SpawnFunction = Callable.From<Variant, Node>(SpawnPlayer);
        RpcId(1, MethodName.PeerReady);

        _enemyManager.RoundComplete += OnRoundComplete;
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
        
        if (IsMultiplayerAuthority())
        {
            player.Died += ()=> OnPlayerDied(peerId);
        }

        return player;
    }

    private void RespawnDeadPeers()
    {
        GD.Print("RespawnDeadPeers");
        foreach (int peerId in deadPeers)
        {
            GD.Print($"Respawning {peerId}");
            _multiplayerSpawner.Spawn(new Godot.Collections.Dictionary { { "peer_id", peerId } });
        } 
        deadPeers = [];
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void PeerReady()
    {
        int senderId = Multiplayer.GetRemoteSenderId();
        GD.Print($"peer {senderId} ready");
        _multiplayerSpawner.Spawn(new Godot.Collections.Dictionary { { "peer_id", senderId } });
        _enemyManager.Synchronize(senderId);
    }

    void OnPlayerDied(int peerId)
    {
                
        GD.Print($"OnPlayerDied {peerId}");
        deadPeers.Add(peerId);

        GD.Print($"deadPeers {deadPeers.Count}");
    }

    private void OnRoundComplete()
    {
        GD.Print("OnRoundComplete");
        RespawnDeadPeers();
        //CallDeferred(nameof(RespawnDeadPeers));
    }

}
