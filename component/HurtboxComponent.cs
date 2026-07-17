using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class HurtboxComponent : Area2D
{
    [Export]
    public HealthComponent HealthComponent { get; set; }

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D otherArea)
    {
        if (!IsMultiplayerAuthority() || otherArea is not HitboxComponent hitboxComponent)
        {
            return;
        }

        hitboxComponent.RegisterHurtboxHit(this);
        HealthComponent.Damage(hitboxComponent.Damage);
    }
}
