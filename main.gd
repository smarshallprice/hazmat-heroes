extends Node

var player_scene:PackedScene = preload("uid://13coq8nj1c80")
var enemey_scene:PackedScene = preload("uid://dlt0fcljhaf06")

@onready var multiplayer_spawner: MultiplayerSpawner = $MultiplayerSpawner
@onready var player_spawn_position:Marker2D = $PlayerSpawnPosition


func _ready():

    multiplayer_spawner.spawn_function = func(data):
        var player = player_scene.instantiate() as Player
        player.name = str(data.peer_id) # rpc node calls have to match the path across clients
        #set authropity for the player that is being created here
        player.input_multiplayer_authority = data.peer_id
        player.global_position = player_spawn_position.global_position
        return player

     #host will be waiting in a lobby, waiting for other connected peers to join, and then will start the game when ready
     # peer tells server it is ready
    peer_ready.rpc_id(1)

    if is_multiplayer_authority():
        var enemey = enemey_scene.instantiate() as Enemy
        enemey.global_position = Vector2(200,200)
        add_child(enemey,true)
   
    

#we need a way for clients to tell server it is ready id:iam on the main scene create my instance for me.
@rpc("any_peer", "call_local", "reliable") #call local is better for peer hosting, call_remote means server cannot call this function
func peer_ready():
    print("peer %s reeady" % multiplayer.get_remote_sender_id()) #which peer sent this message
    var sender_id = multiplayer.get_remote_sender_id()
    multiplayer_spawner.spawn({"peer_id" : sender_id})