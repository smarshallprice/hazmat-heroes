class_name Player

extends CharacterBody2D

@onready var player_input_syncronizer_component:PlayerInputSynchronizerComponent = $PlayerInputSynchronizerComponent
# we need this because when we spawn the player via the network spawn, the Synchronizer component is null because this the player node is not yet added to scene tree
var input_multiplayer_authority:int

func _ready():
    player_input_syncronizer_component.set_multiplayer_authority(input_multiplayer_authority);
    
    #Disable this node if this is not the multiplayer authority
    set_process(is_multiplayer_authority())
    pass

func _process(delta: float) -> void:
    # var movement_vector = Input.get_vector("move_left", "move_right", "move_up", "move_down")
    # velocity = movement_vector * 200 #pixels per second 
    velocity = player_input_syncronizer_component.movement_vector * 100;
    move_and_slide()