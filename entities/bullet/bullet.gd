class_name Bullet
extends Node2D

const SPEED: int = 600

@onready var LifeTime: Timer = $LifeTimer

var direction: Vector2

func _ready():
    LifeTime.timeout.connect(_on_life_timer_timeout)

func _process(delta: float) -> void:
    global_position += direction * SPEED * delta

func start(direction:Vector2):
    self.direction = direction
    rotation = direction.angle()

func register_collision():
    queue_free()

func _on_life_timer_timeout():
    if is_multiplayer_authority():
        queue_free()