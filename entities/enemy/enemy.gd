class_name Enemy
extends CharacterBody2D

@onready var area_2d:Area2D = $Area2D
@onready var target_acquisition_timer:Timer = $TargetAvquisitionTimer

var current_health:int = 1
var target_position:Vector2

func _ready() -> void:
    area_2d.area_entered.connect(_on_area_entered)
    target_acquisition_timer.timeout.connect(_on_target_acquistion_timeout)
    if is_multiplayer_authority():
        #get target on spawn
        acquire_target()

func _process(delta: float) -> void:
    if is_multiplayer_authority():
        #returns normalize directrion pointing to target position
        velocity = global_position.direction_to(target_position) * 40
        move_and_slide()

func  handle_hit() -> void:
    current_health -= 1
    if current_health <= 0:
        queue_free()

func acquire_target()-> void:
    #Get all the players via player group
    var players = get_tree().get_nodes_in_group("player")

    var nearest_player:Player = null
    var nearest_squared_distance:float
    for player in players:
        if nearest_player == null:
            nearest_player = player
            #distance square to enemy from player.
            #distance_squared_to is more effeciant than distance_to
            nearest_squared_distance = nearest_player.global_position.distance_squared_to(global_position)
            continue
        #How far is the player from enemy
        var player_squared_distance: float = player.global_position.distance_squared_to(global_position);
        if player_squared_distance < nearest_squared_distance:
            nearest_squared_distance = player_squared_distance
            nearest_player = player
    if nearest_player != null:
        target_position = nearest_player.global_position
        

func _on_area_entered(other_area: Area2D) -> void:
    if !is_multiplayer_authority():
        return
    if  other_area.owner is Bullet:
        var bullet = other_area.owner as Bullet
        bullet.register_collision()
        handle_hit()

func _on_target_acquistion_timeout()-> void:
    if is_multiplayer_authority():
        acquire_target()