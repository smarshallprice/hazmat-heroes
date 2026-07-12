class_name EnemyManager
extends Node

signal round_began(round_number: int)

const ROUND_BASE_TIME:int = 10
const ROUND_GROWTH:int = 5
const BASE_ENEMY_SPAWN_TIME:float = 2
const BASE_ENEMY_SPAWN_TIME_GROWTH:float = -.15 #as rounbds increase, descrease time between spawns by .15 secs

@export var enemy_scene:PackedScene
@export var enemy_spawn_root:Node
@export var spawn_rect:ReferenceRect

@onready var spawn_internval_timer:Timer = $SpawnIntervalTimer
@onready var round_timer:Timer = $RoundTimer 

var round_count := 0
var spawned_enemies := 0


func _ready() -> void:
    spawn_internval_timer.timeout.connect(_on_spawn_interval_timer_timeout)
    round_timer.timeout.connect(_on_round_timer_timeout)
    GameEvents.enemy_died.connect(_on_enemy_died)
    begin_round()

func get_round_time_remaining()->float:
    return round_timer.time_left

#round are goingb to be longer, based on round count
#enemies will spawn quicker based on round count
func begin_round()->void:
    round_count += 1
    #add  5 seconds (ROUND_GROWTH) after base time after first round. Video 22 - 5:30
    round_timer.wait_time = ROUND_BASE_TIME + ((round_count - 1) * ROUND_GROWTH)
    round_timer.start()

    #first round will have base time, then we descrease (BASE_ENEMY_SPAWN_TIME_GROWTH) that after first 
    spawn_internval_timer.wait_time = BASE_ENEMY_SPAWN_TIME+ ((round_count - 1) * BASE_ENEMY_SPAWN_TIME_GROWTH)
    spawn_internval_timer.start()
    round_began.emit(round_count)

func check_round_completed():
    if !round_timer.is_stopped():
        return
    
    if spawned_enemies == 0:
        print("round comeplete")
        begin_round()

func _on_spawn_interval_timer_timeout() -> void:
    if is_multiplayer_authority():
        spawn_enemy()
        spawn_internval_timer.start()

func spawn_enemy() -> void:
    var enemy = enemy_scene.instantiate() as Enemy
    enemy.global_position = get_random_spawn_position()
    enemy_spawn_root.add_child(enemy, true)
    spawned_enemies += 1

func get_random_spawn_position():
    var x = randf_range(0, spawn_rect.size.x)
    var y = randf_range(0, spawn_rect.size.y)

    return spawn_rect.global_position + Vector2(x,y)

func _on_round_timer_timeout()->void:
    if is_multiplayer_authority():
        spawn_internval_timer.stop()
        print("round over")
        check_round_completed()


func _on_enemy_died():
    spawned_enemies -= 1
    check_round_completed()