using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class Player : CharacterBody2D
{
    public int InputMultiplayerAuthority { get; set; }

    private readonly PackedScene _bulletScene = GD.Load<PackedScene>("uid://bpomv1fpftth5");
    private readonly PackedScene _muzzleFlashScene = GD.Load<PackedScene>("uid://brqekydgbtkul");

    private PlayerInputSynchronizerComponent _playerInputSynchronizer;
    private Node2D _weaponRoot;
    private Timer _fireRateTimer;
    private HealthComponent _healthComponent;
    private Node2D _visuals;
    private AnimationPlayer _animationPlayer;
    private Marker2D _barrelPosition;

    public override void _Ready()
    {
        _playerInputSynchronizer = GetNode<PlayerInputSynchronizerComponent>("PlayerInputSynchronizerComponent");
        _weaponRoot = GetNode<Node2D>("Visuals/WeaponRoot");
        _fireRateTimer = GetNode<Timer>("FireRateTimer");
        _healthComponent = GetNode<HealthComponent>("HealthComponent");
        _visuals = GetNode<Node2D>("Visuals");
        _animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        _barrelPosition = GetNode<Marker2D>("Visuals/WeaponRoot/WeaponAnimationRoot/BarrelPosition");

        _playerInputSynchronizer.SetMultiplayerAuthority(InputMultiplayerAuthority, true);
        _healthComponent.Died += OnDied;
    }

    public override void _Process(double _delta)
    {
        UpdateAimPosition();

        if (IsMultiplayerAuthority())
        {
            Velocity = _playerInputSynchronizer.MovementVector * 100.0f;
            MoveAndSlide();

            if (_playerInputSynchronizer.IsAttackPressed)
            {
                TryFire();
            }
        }
    }

    private void UpdateAimPosition()
    {
        Vector2 aimVector = _playerInputSynchronizer.AimVector;
        Vector2 aimPosition = _weaponRoot.GlobalPosition + aimVector;

        _visuals.Scale = aimVector.X >= 0.0f ? Vector2.One : new Vector2(-1.0f, 1.0f);
        _weaponRoot.LookAt(aimPosition);
    }

    private void TryFire()
    {
        if (!_fireRateTimer.IsStopped())
        {
            return;
        }

        Bullet bullet = _bulletScene.Instantiate<Bullet>();
        bullet.GlobalPosition = _barrelPosition.GlobalPosition;
        bullet.Start(_playerInputSynchronizer.AimVector);
        GetParent().AddChild(bullet, true);
        _fireRateTimer.Start();

        //PlayFireEffectsRpc();
        Rpc(MethodName.PlayFireEffectsRpc);
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void PlayFireEffectsRpc()
    {
        if (_animationPlayer.IsPlaying())
        {
            _animationPlayer.Stop();
            _animationPlayer.Play("fire");
        }

        Node2D muzzleFlash = _muzzleFlashScene.Instantiate<Node2D>();
        muzzleFlash.GlobalPosition = _barrelPosition.GlobalPosition;
        muzzleFlash.GlobalRotation = _barrelPosition.GlobalRotation;
        GetParent().AddChild(muzzleFlash);
    }

    private void OnDied()
    {
        GD.Print("player died");
    }
}
