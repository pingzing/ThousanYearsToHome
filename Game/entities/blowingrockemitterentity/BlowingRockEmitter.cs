using Godot;
using ThousandYearsHome.Entities.BlowingRockEntity;

namespace ThousandYearsHome.Entities.BlowingRockEmitterEntity
{

    [Tool]
    public class BlowingRockEmitter : Node2D
    {
        private bool _isActive = false;
        [Export] public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                if (_isActive)
                {
                    _spawnTimer.Start();
                }
                else
                {
                    _spawnTimer.Stop();
                }
            }
        }

        // In seconds.
        private float _rockSpawnTime = 5f;
        [Export] public float RockSpawnTime
        {
            get => _rockSpawnTime;
            set { _rockSpawnTime = value; Update(); }
        }

        // In degrees.
        private float _spawnAngle = 0f;
        [Export(PropertyHint.Range, "0, 359, 1")]
        public float SpawnAngle
        {
            get => _spawnAngle;
            set { _spawnAngle = value; Update(); }
        }

        private Timer _spawnTimer = null!;
        private DynamicFont _debugFont = null!;
        private PackedScene _blowingRockScene = null!;

        // Called when the node enters the scene tree for the first time.
        public override void _Ready()
        {
            _spawnTimer = GetNode<Timer>("SpawnTimer");
            _spawnTimer.WaitTime = RockSpawnTime;
            _blowingRockScene = ResourceLoader.Load<PackedScene>("res://entities/blowingrockentity/BlowingRock.tscn");
        }

        public void OnSpawnTimerTimeout()
        {
            BlowingRock blowingRock = _blowingRockScene.Instance<BlowingRock>();
            AddChild(blowingRock);
        }

        public override void _Draw()
        {
            if (!Engine.EditorHint)
            {
                return;
            }

            if (_debugFont == null)
            {
                _debugFont = (DynamicFont)GD.Load("res://fonts/Equipment-base.tres");
            }

            float arrowLength = 10f;
            float angleRadians = Mathf.Deg2Rad(SpawnAngle);
            float targetX = arrowLength * Mathf.Cos(angleRadians);
            float targetY = arrowLength * Mathf.Sin(angleRadians);
            Vector2 target = new Vector2(targetX, targetY);

            float armLength = 7f;
            float armAngle1Radians = Mathf.Deg2Rad(SpawnAngle + 180 + 35);
            float arm1TargetX = target.x + (armLength * Mathf.Cos(armAngle1Radians));
            float arm1TargetY = target.y + (armLength * Mathf.Sin(armAngle1Radians));
            Vector2 arm1Target = new Vector2(arm1TargetX, arm1TargetY);

            float armAngle2Radians = Mathf.Deg2Rad(SpawnAngle + 180 - 35);
            float arm2TargetX = target.x + (armLength * Mathf.Cos(armAngle2Radians));
            float arm2TargetY = target.y + (armLength * Mathf.Sin(armAngle2Radians));
            Vector2 arm2Target = new Vector2(arm2TargetX, arm2TargetY);
            
            DrawLine(Vector2.Zero, target, Colors.Green, width: 2);
            DrawLine(target, arm1Target, Colors.Green, width: 2);
            DrawLine(target, arm2Target, Colors.Green, width: 2);

            DrawString(_debugFont, new Vector2(0, 10), $"{RockSpawnTime}s");
        }
    }
}