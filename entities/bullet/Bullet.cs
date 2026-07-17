using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class Bullet : Node2D
{
    private const float Speed = 600.0f;

    private HitboxComponent _hitboxComponent;
    private Timer _lifeTimer;
    //private Vector2 _direction;
     [Export]
    public Vector2 Direction { get; set; }

    public override void _Ready()
    {
        _hitboxComponent = GetNode<HitboxComponent>("HitboxComponent");
        _lifeTimer = GetNode<Timer>("LifeTimer");

        _hitboxComponent.HitHurtbox += OnHitHurtbox;
        _lifeTimer.Timeout += OnLifeTimerTimeout;
    }

    public override void _Process(double delta)
    {
        GlobalPosition += Direction * Speed * (float)delta;
    }

    public void Start(Vector2 pvDirection)
    {
        Direction = pvDirection;
        Rotation = Direction.Angle();
    }

    public void RegisterCollision()
    {
        QueueFree();
    }

    private void OnLifeTimerTimeout()
    {
        if (IsMultiplayerAuthority())
        {
            QueueFree();
        }
    }

    private void OnHitHurtbox(HurtboxComponent _hurtboxComponent)
    {
        RegisterCollision();
    }
}
