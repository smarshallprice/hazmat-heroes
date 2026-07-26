using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class Main : Node
{
    const string MAIN_MENU_SCENE_PATH = "res://ui/main_menu/main_menu.tscn";
    private readonly PackedScene _playerScene = GD.Load<PackedScene>("uid://13coq8nj1c80");
    //private readonly PackedScene mainMenuScene = GD.Load<PackedScene>("uid://bhuy3ga7evxc6");


    private MultiplayerSpawner _multiplayerSpawner;
    private Marker2D _playerSpawnPosition;
    private EnemyManager _enemyManager;

    private List<int> deadPeers = new List<int>();
    private Godot.Collections.Dictionary<int, Player> playerDictionary = new();

    public override void _Ready()
    {
        _multiplayerSpawner = GetNode<MultiplayerSpawner>("MultiplayerSpawner");
        _playerSpawnPosition = GetNode<Marker2D>("PlayerSpawnPosition");
        _enemyManager = GetNode<EnemyManager>("EnemyManager");

        _multiplayerSpawner.SpawnFunction = Callable.From<Variant, Node>(SpawnPlayer);
        RpcId(1, MethodName.PeerReady);

        _enemyManager.RoundComplete += OnRoundComplete;
        Multiplayer.ServerDisconnected += OnServerDisconnected; // Only set to the peers

        //if a client peer disconnects, we will consider them dead and check if the game is over
        Multiplayer.PeerDisconnected += onPeerDisconnected;
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

        playerDictionary.Add(peerId, player);

        return player;
    }

    private void RespawnDeadPeers()
    {
        GD.Print("RespawnDeadPeers");
        var allPeers = GetAllPeers();
        foreach (int peerId in deadPeers)
        {
            if (!allPeers.Contains(peerId)) continue; // it is possible for there to be a dead peer that is no longer connected
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

    private void EndGame()
    {
        //Multiplayer.MultiplayerPeer = null; //Kills the server and all clients
        Multiplayer.MultiplayerPeer = new OfflineMultiplayerPeer();

        GetTree().ChangeSceneToFile(MAIN_MENU_SCENE_PATH);

    }

    private void CheckGameOver()
    {
        bool isGameover = true;
      
        
        foreach (var connectedPeer in GetAllPeers()) //GetPeers does not return all peers, it excludes self
        {
            if (!deadPeers.Contains(connectedPeer))
            {
                isGameover = false;
                break;
            }
        }

        if (isGameover)
        {
            //terminate server and peers
            EndGame();
        }
    }

    int[] GetAllPeers()
    {
        //get list of all peers
        //if all peer id exist in dead peer list
        int[] allPeers = [1, ..Multiplayer.GetPeers()];
        return allPeers;
    }

    void OnPlayerDied(int peerId)
    {
                
        GD.Print($"OnPlayerDied {peerId}");
        deadPeers.Add(peerId);
        CheckGameOver();
    }

    private void OnRoundComplete()
    {
        GD.Print("OnRoundComplete");
        RespawnDeadPeers();
        //CallDeferred(nameof(RespawnDeadPeers));
    }

    private void OnServerDisconnected()
    {
        EndGame();
    }

    private void onPeerDisconnected(long peerId)
    {
        // GD.Print($"Peer {peerId} disconnected");
        // deadPeers.Add((int)peerId);
        // CheckGameOver();
        //we could do this to get the player at that path, but we are going to use a dict instead
        //var player = GetNode<Player>($"YSortRoot/{peerId}");

        if (playerDictionary.ContainsKey((int)peerId)) // added in video 45
        {
            var player = playerDictionary[(int) peerId];
            if (IsInstanceValid(player))// checks if player is not null, and it is a valid node. Meaning it hasn't been freed yet
            {                
                playerDictionary[(int)peerId].Kill();   
            }
            playerDictionary.Remove((int)peerId);
        }
    }
}
