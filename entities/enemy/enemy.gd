class_name Enemy
extends CharacterBody2D

@onready var target_acquisition_timer:Timer = $TargetAvquisitionTimer
@onready var health_component : HealthComponent = $HealthComponent
@onready var visuals : Node2D = $Visuals
@onready var attack_cooldown_timer:Timer = $AttackCoolDownTimer
@onready var charge_attack_timer:Timer = $ChargeAttackTimer # how long to be in the charging state
@onready var hitbox_collision_shape: CollisionShape2D = %HitboxCollisionShape
@onready var alert_sprite: Sprite2D = $AlertSprite

var target_position:Vector2

var state_machine: CallableStateMachine = CallableStateMachine.new()
var default_collision_mask:int
var default_collision_layer:int
var alert_tween:Tween

func _ready() -> void:   
    state_machine.add_states(state_spawn, enter_state_spawn, Callable())
    state_machine.add_states(state_normal, enter_state_normal, Callable())
    state_machine.add_states(state_charge_attack, enter_state_charge_attack, leave_state_charge_attack)
    state_machine.add_states(state_attack, enter_state_attack, leave_state_attack)
    state_machine.set_initial_state(state_spawn)

    default_collision_mask = collision_mask
    default_collision_layer = collision_layer
    hitbox_collision_shape.disabled = true
    alert_sprite.scale = Vector2.ZERO #'hide' sprite to start


    if is_multiplayer_authority():
        health_component.died.connect(_on_died)

func _process(_delta: float) -> void:
    state_machine.update() # calls normal state

    if is_multiplayer_authority():
        move_and_slide()
    

func enter_state_spawn()-> void:
    var tween := create_tween()
    tween.tween_property(visuals, "scale", Vector2.ONE, 0.4)\
        .from(Vector2.ZERO)\
        .set_ease(Tween.EASE_OUT)\
        .set_trans(Tween.TRANS_BACK)
    await tween.finished
    state_machine.change_state(state_normal)


func state_spawn()-> void:
    pass  

func enter_state_normal():
    if is_multiplayer_authority():
        acquire_target()
        target_acquisition_timer.start()

func state_normal() ->void :
    if is_multiplayer_authority():
        velocity = global_position.direction_to(target_position) * 40  

        if target_acquisition_timer.is_stopped():
            acquire_target()
            target_acquisition_timer.start()

        if attack_cooldown_timer.is_stopped() && global_position.distance_to(target_position) < 150:
            state_machine.change_state(state_charge_attack)
    
    flip()


func enter_state_charge_attack():
    if is_multiplayer_authority():
        acquire_target()
        charge_attack_timer.start()
    
    #show alert sprite
    if alert_tween != null && alert_tween.is_valid():
        alert_tween.kill()
    alert_tween = create_tween()
    alert_tween.tween_property(alert_sprite, "scale", Vector2.ONE,  0.2)\
    .set_ease(Tween.EASE_OUT)\
    .set_trans(Tween.TransitionType.TRANS_BACK)


func state_charge_attack():

    if is_multiplayer_authority():
        # we want to "stop" the enemy for the charge
        velocity = velocity.lerp(Vector2.ZERO, 1.0 - exp(-15 * get_process_delta_time()))#makes sure lerp is framerate independent
        if  charge_attack_timer.is_stopped():
            state_machine.change_state(state_attack) 

func leave_state_charge_attack():
    #hide alert sprite
    if alert_tween != null && alert_tween.is_valid():
        alert_tween.kill()
    alert_tween= create_tween()
    alert_tween.tween_property(alert_sprite, "scale", Vector2.ZERO,  0.2)\
    .set_ease(Tween.EASE_IN)\
    .set_trans(Tween.TransitionType.TRANS_BACK)

func enter_state_attack():
    if is_multiplayer_authority():
        #this attack is actually the dash attack
        #we want to disable the hitbox while doign the dash attack
        collision_mask = 1 << 0 # take bit 1 and shift it over zero places. wtf? video 39 - 9:30 ish 
        collision_layer = 0
        hitbox_collision_shape.disabled = false # enable so we do damage to player
        velocity = global_position.direction_to(target_position) * 400 #move to dir with dash speed

func state_attack():
    if is_multiplayer_authority():
        velocity = velocity.lerp(Vector2.ZERO, 1.0 - exp(-3 * get_process_delta_time()))   
        if velocity.length() < 25: # if we slow down to less than 25 pixles per second
            state_machine.change_state(state_normal)
    
func leave_state_attack():
    if is_multiplayer_authority():
        collision_mask = default_collision_mask # turn collision back on 
        collision_layer = default_collision_layer
        hitbox_collision_shape.disabled = true # disable attacking box
        attack_cooldown_timer.start()

func  flip() -> void:
    visuals.scale = Vector2.ONE if target_position.x > global_position.x else Vector2(-1, 1)




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
        

func _on_died() -> void:
    GameEvents.emit_enemy_died()
    queue_free()