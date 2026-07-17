using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class PlayerInputSynchronizerComponent : MultiplayerSynchronizer
{
    [Export]
    public Node2D AimRoot { get; set; }

    [Export]
    public Vector2 MovementVector { get; set; } = Vector2.Zero;

    [Export]
    public Vector2 AimVector { get; set; } = Vector2.Right;

    [Export]
    public bool IsAttackPressed { get; set; }

    public override void _Process(double _delta)
    {
        if (IsMultiplayerAuthority())
        {
            GatherInput();
        }
    }

    public void GatherInput()
    {
        MovementVector = Input.GetVector("move_left", "move_right", "move_up", "move_down");
        AimVector = AimRoot.GlobalPosition.DirectionTo(AimRoot.GetGlobalMousePosition());
        IsAttackPressed = Input.IsActionPressed("attack");
    }
}
