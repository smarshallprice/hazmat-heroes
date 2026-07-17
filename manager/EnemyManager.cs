using Godot;

namespace MatchTastic;

[GlobalClass]
public partial class EnemyManager : Node
{
    [Signal]
    public delegate void RoundChangedEventHandler(int roundNumber);

    private const int RoundBaseTime = 10;
    private const int RoundGrowth = 5;
    private const float BaseEnemySpawnTime = 2.0f;
    private const float BaseEnemySpawnTimeGrowth = -0.15f;

    [Export]
    public PackedScene EnemyScene { get; set; }

    [Export]
    public Node EnemySpawnRoot { get; set; }

    [Export]
    public ReferenceRect SpawnRect { get; set; }

    public int RoundCount
    {
        get => _roundCount;
        private set
        {
            _roundCount = value;
            EmitSignal(SignalName.RoundChanged, _roundCount);
        }
    }

    private Timer _spawnIntervalTimer;
    private Timer _roundTimer;
    private GameEvents _gameEvents;
    private int _roundCount;
    private int _spawnedEnemies;

    public override void _Ready()
    {
        _spawnIntervalTimer = GetNode<Timer>("SpawnIntervalTimer");
        _roundTimer = GetNode<Timer>("RoundTimer");

        _spawnIntervalTimer.Timeout += OnSpawnIntervalTimerTimeout;
        _roundTimer.Timeout += OnRoundTimerTimeout;

        _gameEvents = GetNode<GameEvents>("/root/GameEvents");
        _gameEvents.EnemyDied += OnEnemyDied;

        if (IsMultiplayerAuthority())
        {
            BeginRound();
        }
    }

    public override void _ExitTree()
    {
        if (_gameEvents != null)
        {
            _gameEvents.EnemyDied -= OnEnemyDied;
        }
    }

    public void Synchronize(int toPeerId = -1)
    {
        if (!IsMultiplayerAuthority())
        {
            return;
        }

        var data = new Godot.Collections.Dictionary
        {
            { "round_timer_is_running", !_roundTimer.IsStopped() },
            { "round_timer_time_left", _roundTimer.TimeLeft },
            { "round_count", RoundCount },
        };

        if (toPeerId > -1 && toPeerId != 1)
        {
            RpcId(toPeerId, MethodName.ApplySynchronization, data);
        }
        else
        {
            Rpc(MethodName.ApplySynchronization, data);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ApplySynchronization(Godot.Collections.Dictionary data)
    {
        double waitTime = data["round_timer_time_left"].AsDouble();
        if (waitTime > 0.0)
        {
            _roundTimer.WaitTime = waitTime;
        }

        if ((bool)data["round_timer_is_running"])
        {
            _roundTimer.Start();
        }

        RoundCount = data["round_count"].AsInt32();
    }

    public float GetRoundTimeRemaining()
    {
        return (float)_roundTimer.TimeLeft;
    }

    private void BeginRound()
    {
        RoundCount++;
        _roundTimer.WaitTime = RoundBaseTime + ((RoundCount - 1) * RoundGrowth);
        _roundTimer.Start();

        _spawnIntervalTimer.WaitTime = BaseEnemySpawnTime + ((RoundCount - 1) * BaseEnemySpawnTimeGrowth);
        _spawnIntervalTimer.Start();

        Synchronize();
    }

    private void CheckRoundCompleted()
    {
        if (!_roundTimer.IsStopped())
        {
            return;
        }

        if (_spawnedEnemies == 0)
        {
            GD.Print("round complete");
            BeginRound();
        }
    }

    private void OnSpawnIntervalTimerTimeout()
    {
        if (IsMultiplayerAuthority())
        {
            SpawnEnemy();
            _spawnIntervalTimer.Start();
        }
    }

    private void SpawnEnemy()
    {
        Enemy enemy = EnemyScene.Instantiate<Enemy>();
        enemy.GlobalPosition = GetRandomSpawnPosition();
        EnemySpawnRoot.AddChild(enemy, true);
        _spawnedEnemies++;
    }

    private Vector2 GetRandomSpawnPosition()
    {
        float x = (float)GD.RandRange(0.0, SpawnRect.Size.X);
        float y = (float)GD.RandRange(0.0, SpawnRect.Size.Y);

        return SpawnRect.GlobalPosition + new Vector2(x, y);
    }

    private void OnRoundTimerTimeout()
    {
        if (IsMultiplayerAuthority())
        {
            _spawnIntervalTimer.Stop();
            GD.Print("round over");
            CheckRoundCompleted();
        }
    }

    private void OnEnemyDied()
    {
        _spawnedEnemies--;
        CheckRoundCompleted();
    }
}
