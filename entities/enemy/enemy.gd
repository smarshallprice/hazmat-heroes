class_name Enemy
extends CharacterBody2D

@onready var area_2d:Area2D = $Area2D

var current_health:int = 5

func _ready() -> void:
    area_2d.area_entered.connect(_on_area_entered)

func  handle_hit() -> void:
    current_health -= 1
    if current_health <= 0:
        queue_free()

func _on_area_entered(other_area: Area2D) -> void:
    if !is_multiplayer_authority():
        return
    if  other_area.owner is Bullet:
        var bullet = other_area.owner as Bullet
        bullet.register_collision()
        handle_hit()