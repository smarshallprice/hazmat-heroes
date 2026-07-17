using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class GameEvents : Node
{
    [Signal]
    public delegate void EnemyDiedEventHandler();

    public void EmitEnemyDied()
    {
        EmitSignal(SignalName.EnemyDied);
    }
}
