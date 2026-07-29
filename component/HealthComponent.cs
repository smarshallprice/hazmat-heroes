using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class HealthComponent : Node
{
    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void DamagedEventHandler();

    [Export]
    public int MaxHealth { get; set; } = 1;

    public int CurrentHealth => _currentHealth;

    private int _currentHealth;

    public override void _Ready()
    {
        _currentHealth = MaxHealth;
    }

    public void Damage(int amount)
    {
        _currentHealth = Mathf.Clamp(_currentHealth - amount, 0, MaxHealth);
        EmitSignal(SignalName.Damaged);
        if (_currentHealth <= 0)
        {
            EmitSignal(SignalName.Died);
        }
    }
}
