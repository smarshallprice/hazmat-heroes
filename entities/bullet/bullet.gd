class_name Bullet
extends Node2D

const SPEED: int = 600

@onready var LifeTime: Timer = $LifeTimer
@onready var hitbox_component:HitboxComponent = $HitboxComponent

var direction: Vector2

func _ready():
    hitbox_component.hit_hurtbox.connect(_on_hit_hurtbox)
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

func _on_hit_hurtbox(_hurtbox_component: HurtboxComponent):
    register_collision()