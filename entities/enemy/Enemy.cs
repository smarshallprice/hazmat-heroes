using Godot;

namespace MatchTastic;

[GlobalClass, SceneTree]
public partial class Enemy : CharacterBody2D
{
    [Export]
    public Vector2 TargetPosition { get; set; }

    [Export]
    public string CurrentState
    {
        get => _stateMachine.CurrentState;
        set => _stateMachine.ChangeState(value);
    }

    //private Timer _targetAcquisitionTimer;
   // private HealthComponent _healthComponent;
   // private Node2D _visuals;
    //private Timer _attackCooldownTimer;
    //private Timer _chargeAttackTimer;
   // private CollisionShape2D _hitboxCollisionShape;
    //private Sprite2D _alertSprite;

    private readonly CallableStateMachine _stateMachine = new();
    private uint _defaultCollisionMask;
    private uint _defaultCollisionLayer;
    private Tween _alertTween;

    public override void _Notification(int what)
    {
        if (what != NotificationSceneInstantiated)
        {
            return;
        }

        _stateMachine.AddState(nameof(StateSpawn), StateSpawn, EnterStateSpawn);
        _stateMachine.AddState(nameof(StateNormal), StateNormal, EnterStateNormal);
        _stateMachine.AddState(
            nameof(StateChargeAttack),
            StateChargeAttack,
            EnterStateChargeAttack,
            LeaveStateChargeAttack);
        _stateMachine.AddState(nameof(StateAttack), StateAttack, EnterStateAttack, LeaveStateAttack);
    }

    public override void _Ready()
    {
        //_targetAcquisitionTimer = GetNode<Timer>("TargetAvquisitionTimer");
        //_healthComponent = GetNode<HealthComponent>("HealthComponent");
        //_visuals = GetNode<Node2D>("Visuals");
        //_attackCooldownTimer = GetNode<Timer>("AttackCoolDownTimer");
        //_chargeAttackTimer = GetNode<Timer>("ChargeAttackTimer");
        //_hitboxCollisionShape = GetNode<CollisionShape2D>("HitboxComponent/HitboxCollisionShape");
       // _alertSprite = GetNode<Sprite2D>("AlertSprite");

        _defaultCollisionMask = CollisionMask;
        _defaultCollisionLayer = CollisionLayer;
        _.HitboxComponent.HitboxCollisionShape.Disabled = true;
        _.AlertSprite.Scale = Vector2.Zero;

        if (IsMultiplayerAuthority())
        {
            _.HealthComponent.Died += OnDied;
            _stateMachine.SetInitialState(nameof(StateSpawn));
        }
    }

    public override void _Process(double _delta)
    {
        _stateMachine.Update();

        if (IsMultiplayerAuthority())
        {
            MoveAndSlide();
        }
    }

    private async void EnterStateSpawn()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_.Visuals.Get(), "scale", Vector2.One, 0.4f)
            .From(Vector2.Zero)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);

        await ToSignal(tween, Tween.SignalName.Finished);
        _stateMachine.ChangeState(nameof(StateNormal));
    }

    private void StateSpawn()
    {
    }

    private void EnterStateNormal()
    {
        if (IsMultiplayerAuthority())
        {
            AcquireTarget();
            _.TargetAvquisitionTimer.Start();
        }
    }

    private void StateNormal()
    {
        if (IsMultiplayerAuthority())
        {
            Velocity = GlobalPosition.DirectionTo(TargetPosition) * 40.0f;

            if (_.TargetAvquisitionTimer.IsStopped())
            {
                AcquireTarget();
                _.TargetAvquisitionTimer.Start();
            }

            if (_.AttackCoolDownTimer.IsStopped() && GlobalPosition.DistanceTo(TargetPosition) < 150.0f)
            {
                _stateMachine.ChangeState(nameof(StateChargeAttack));
            }
        }

        Flip();
    }

    private void EnterStateChargeAttack()
    {
        if (IsMultiplayerAuthority())
        {
            AcquireTarget();
            _.ChargeAttackTimer.Start();
        }

        if (_alertTween != null && _alertTween.IsValid())
        {
            _alertTween.Kill();
        }

        _alertTween = CreateTween();
        _alertTween.TweenProperty(_.AlertSprite, "scale", Vector2.One, 0.2f)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void StateChargeAttack()
    {
        if (IsMultiplayerAuthority())
        {
            Velocity = Velocity.Lerp(Vector2.Zero, 1.0f - Mathf.Exp(-15.0f * (float)GetProcessDeltaTime()));

            if (_.ChargeAttackTimer.IsStopped())
            {
                _stateMachine.ChangeState(nameof(StateAttack));
            }
        }

        Flip();
    }

    private void LeaveStateChargeAttack()
    {
        if (_alertTween != null && _alertTween.IsValid())
        {
            _alertTween.Kill();
        }

        _alertTween = CreateTween();
        _alertTween.TweenProperty(_.AlertSprite, "scale", Vector2.Zero, 0.2f)
            .SetEase(Tween.EaseType.In)
            .SetTrans(Tween.TransitionType.Back);
    }

    private void EnterStateAttack()
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        CollisionMask = 1u << 0;
        CollisionLayer = 0;
        _.HitboxComponent.HitboxCollisionShape.Disabled = false;
        Velocity = GlobalPosition.DirectionTo(TargetPosition) * 40.0f;
    }

    private void StateAttack()
    {
        if (IsMultiplayerAuthority())
        {
            Velocity = Velocity.Lerp(Vector2.Zero, 1.0f - Mathf.Exp(-3.0f * (float)GetProcessDeltaTime()));

            if (Velocity.Length() < 25.0f)
            {
                _stateMachine.ChangeState(nameof(StateNormal));
            }
        }
    }

    private void LeaveStateAttack()
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        CollisionMask = _defaultCollisionMask;
        CollisionLayer = _defaultCollisionLayer;
        _.HitboxComponent.HitboxCollisionShape.Disabled = true;
        _.AttackCoolDownTimer.Start();
    }

    private void Flip()
    {
        _.Visuals.Get().Scale = TargetPosition.X > GlobalPosition.X ? Vector2.One : new Vector2(-1.0f, 1.0f);
    }

    private void AcquireTarget()
    {
        Player nearestPlayer = null;
        float nearestSquaredDistance = float.PositiveInfinity;

        foreach (Node playerNode in GetTree().GetNodesInGroup("player"))
        {
            if (playerNode is not Player player)
            {
                continue;
            }

            float playerSquaredDistance = player.GlobalPosition.DistanceSquaredTo(GlobalPosition);
            if (playerSquaredDistance < nearestSquaredDistance)
            {
                nearestSquaredDistance = playerSquaredDistance;
                nearestPlayer = player;
            }
        }

        if (nearestPlayer != null)
        {
            TargetPosition = nearestPlayer.GlobalPosition;
        }
    }

    private void OnDied()
    {
        GetNode<GameEvents>("/root/GameEvents").EmitEnemyDied();
        QueueFree();
    }
}
