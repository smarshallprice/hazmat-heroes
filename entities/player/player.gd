class_name Player

extends CharacterBody2D

@onready var player_input_syncronizer_component:PlayerInputSynchronizerComponent = $PlayerInputSynchronizerComponent
@onready var weapon_root:Node2D = $WeaponRoot
@onready var fire_rate_timer:Timer = $FireRateTimer


var bullet_scene:PackedScene = preload("uid://bpomv1fpftth5")

# we need this because when we spawn the player via the network spawn, the Synchronizer component is null because this the player node is not yet added to scene tree
var input_multiplayer_authority:int

func _ready():
	player_input_syncronizer_component.set_multiplayer_authority(input_multiplayer_authority);
	

# func _input(event: InputEvent) -> void:
# 	if event.is_action_pressed("attack"):
# 		create_bullet()
		

func _process(delta: float) -> void:
	weapon_root.look_at(weapon_root.global_position + player_input_syncronizer_component.aim_vector)
	#as if of now, this is only the server. The client only as authority over input synchronizer
	if is_multiplayer_authority():
		velocity = player_input_syncronizer_component.movement_vector * 100
		move_and_slide()
		if player_input_syncronizer_component.is_attack_pressed:
			try_create_bullet()

func  try_create_bullet():
	if !fire_rate_timer.is_stopped():
		return

	#TODO: create client only visual bullet immediatly, so we dont have to wait for server round trip from spawner
	var bullet = bullet_scene.instantiate() as Bullet
	bullet.global_position = weapon_root.global_position
	bullet.start(player_input_syncronizer_component.aim_vector)
	get_parent().add_child(bullet,true)
	fire_rate_timer.start()

	#get_tree().current_scene.add_child(bullet)
