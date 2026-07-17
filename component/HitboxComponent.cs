using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class HitboxComponent : Area2D
{
    [Signal]
    public delegate void HitHurtboxEventHandler(HurtboxComponent hurtboxComponent);

    public int Damage { get; set; } = 1;

    public void RegisterHurtboxHit(HurtboxComponent hurtboxComponent)
    {
        EmitSignal(SignalName.HitHurtbox, hurtboxComponent);
    }
}
