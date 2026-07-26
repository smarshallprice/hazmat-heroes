using System;
using Godot;

namespace MatchTastic;

[GlobalClass, SceneTree]
public partial class Player : CharacterBody2D
{
    [Signal]
    public delegate void DiedEventHandler();

    public int InputMultiplayerAuthority { get; set; }

    private readonly PackedScene _bulletScene = GD.Load<PackedScene>("uid://bpomv1fpftth5");
    private readonly PackedScene _muzzleFlashScene = GD.Load<PackedScene>("uid://brqekydgbtkul");

    //private PlayerInputSynchronizerComponent _playerInputSynchronizer;
    //private Node2D _weaponRoot;
   //private Timer _fireRateTimer;
    //private HealthComponent _healthComponent;
    //private Node2D _visuals;
    //private AnimationPlayer _animationPlayer;
    private Marker2D barrelPosition;

    private bool isDying = false;

    public override void _Ready()
    {
        GD.Print($"Scene path: {SceneFilePath}");
        GD.Print($"Parent: {GetParent()}");

        //_playerInputSynchronizer = GetNode<PlayerInputSynchronizerComponent>("PlayerInputSynchronizerComponent");
        //WeaponRoot =  _.Visuals.WeaponRoot. GetNode<Node2D>("Visuals/WeaponRoot");
        //_fireRateTimer = GetNode<Timer>("FireRateTimer");
        //_healthComponent = GetNode<HealthComponent>("HealthComponent");
        //_visuals = GetNode<Node2D>("Visuals");
        //_animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        //_barrelPosition = GetNode<Marker2D>("Visuals/WeaponRoot/WeaponAnimationRoot/BarrelPosition");
        barrelPosition = _.Visuals.WeaponRoot.WeaponAnimationRoot.BarrelPosition;
        _.PlayerInputSynchronizerComponent.SetMultiplayerAuthority(InputMultiplayerAuthority, true);
        if (IsMultiplayerAuthority())
        {
            _.HealthComponent.Died += OnDied;        
        }
    }

    public override void _Process(double _delta)
    {
        UpdateAimPosition();

        if (IsMultiplayerAuthority())
        {           
            Velocity = _.PlayerInputSynchronizerComponent.MovementVector * 100.0f;
            MoveAndSlide();

            if (isDying)
            {
                GlobalPosition = Vector2.Right * 1000; //see kill method for why we do this
                return;
            }

            if (_.PlayerInputSynchronizerComponent.IsAttackPressed)
            {
                TryFire();
            }

           
        }
    }

    private void UpdateAimPosition()
    {
        Node2D weaponRoot = _.Visuals.WeaponRoot.Get();
        Vector2 aimVector = _.PlayerInputSynchronizerComponent.AimVector;
        Vector2 aimPosition = weaponRoot.GlobalPosition + aimVector;

        _.Visuals.Get().Scale = aimVector.X >= 0.0f ? Vector2.One : new Vector2(-1.0f, 1.0f);
        weaponRoot.LookAt(aimPosition);
    }

    private void TryFire()
    {
        if (!_.FireRateTimer.IsStopped())
        {
            return;
        }

        Bullet bullet = _bulletScene.Instantiate<Bullet>();
        bullet.GlobalPosition = barrelPosition.GlobalPosition;
        bullet.Start(_.PlayerInputSynchronizerComponent.AimVector);
        GetParent().AddChild(bullet, true);
        _.FireRateTimer.Start();

        //PlayFireEffectsRpc();
        Rpc(MethodName.PlayFireEffectsRpc);
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void PlayFireEffectsRpc()
    {
        if (_.AnimationPlayer.IsPlaying())
        {
            _.AnimationPlayer.Stop();
            _.AnimationPlayer.Play("fire");
        }

        Node2D muzzleFlash = _muzzleFlashScene.Instantiate<Node2D>();
        muzzleFlash.GlobalPosition = barrelPosition.GlobalPosition;
        muzzleFlash.GlobalRotation = barrelPosition.GlobalRotation;
        GetParent().AddChild(muzzleFlash);
    }

    public async void Kill()
    {
        if (!IsMultiplayerAuthority())
        {
           GD.PushError("Cannmot call kill on non-server client"); 
           return;
        }

        // we have to do this to tell client you have died on the server, so stop stending input so we can QueueFree your node.
        //we have to do this because while the server Queue Frees the node, the client can still send input to a removed node
        _KillRpc();
        await ToSignal(GetTree().CreateTimer(0.5), Timer.SignalName.Timeout);
            
        GD.Print("player died");
        EmitSignal(SignalName.Died);
        QueueFree();        
    }

    [Rpc(
        MultiplayerApi.RpcMode.Authority,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void _Kill()
    {
        isDying = true;
        //whether sync should be visible to all peers. This will stop broadcasting to the server.
        _.PlayerInputSynchronizerComponent.PublicVisibility = false;
        //remove player from arean, but keep in tree
        //so we will move the player offscreen in process method
    }

    private async void OnDied()
    {
        Kill();
    }
}
