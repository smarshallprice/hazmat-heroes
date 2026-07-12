class_name Player

extends CharacterBody2D

@onready var player_input_syncronizer_component:PlayerInputSynchronizerComponent = $PlayerInputSynchronizerComponent
@onready var weapon_root:Node2D = $Visuals/WeaponRoot
@onready var fire_rate_timer:Timer = $FireRateTimer
@onready var health_component:HealthComponent = $HealthComponent
@onready var Visuals:Node2D = $Visuals
@onready var animation_player:AnimationPlayer = $AnimationPlayer
@onready var barrel_position:Marker2D = $Visuals/WeaponRoot/WeaponAnimationRoot/BarrelPosition

var bullet_scene:PackedScene = preload("uid://bpomv1fpftth5")
var muzzle_flash_scene:PackedScene = preload("uid://brqekydgbtkul")

# we need this because when we spawn the player via the network spawn, the Synchronizer component is null because this the player node is not yet added to scene tree
var input_multiplayer_authority:int

func _ready():
	player_input_syncronizer_component.set_multiplayer_authority(input_multiplayer_authority);
	health_component.died.connect(_on_died)
	

# func _input(event: InputEvent) -> void:
# 	if event.is_action_pressed("attack"):
# 		create_bullet()
		

func _process(_delta: float) -> void:
	update_aim_position()
	#as if of now, this is only the server. The client only as authority over input synchronizer
	if is_multiplayer_authority():
		velocity = player_input_syncronizer_component.movement_vector * 100
		move_and_slide()
		if player_input_syncronizer_component.is_attack_pressed:
			try_fire()

func update_aim_position():
	var aim_vector = player_input_syncronizer_component.aim_vector
	var aim_position = weapon_root.global_position + aim_vector

	#bc aim_positrion is a unit vector, we can do this
	Visuals.scale = Vector2.ONE if aim_vector.x >= 0 else Vector2(-1,1)
	weapon_root.look_at(aim_position)

func  try_fire():
	if !fire_rate_timer.is_stopped():
		return

	#TODO: create client only visual bullet immediatly, so we dont have to wait for server round trip from spawner
	var bullet = bullet_scene.instantiate() as Bullet
	bullet.global_position = barrel_position.global_position
	bullet.start(player_input_syncronizer_component.aim_vector)
	get_parent().add_child(bullet,true)
	fire_rate_timer.start()

	#try_fire is on the server, so we need to tell the clients to play the fire effects
	play_fire_effects.rpc()


@rpc("authority", "call_local", "unreliable")
func play_fire_effects():
	#animations dont reset when we trigger animation again
	#so we want to stop it if it is already playing
	if animation_player.is_playing():
		animation_player.stop()
	animation_player.play("fire")

	var muzzle_flash = muzzle_flash_scene.instantiate() as Node2D
	muzzle_flash.global_position = barrel_position.global_position
	muzzle_flash.rotation = barrel_position.global_rotation
	get_parent().add_child(muzzle_flash)

func _on_died() -> void:
	print("player died")