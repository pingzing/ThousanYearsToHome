using Godot;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Entities
{
    [Tool]
    public class PowerBall : Node2D
    {
        public const string PowerBallGroup = "PowerBallGroup";
        public const string BallDeactivatedMethod = "BallDeactivated";

        private float _speed = 150f;
        [Export(PropertyHint.Range, "0, 10000, 1")]
        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                Update();
            }
        }

        // In degrees, for easier Editor-ing.
        private float _directionAngle = 180;
        [Export(PropertyHint.Range, "0, 359, 1")]
        public float DirectionAngle
        {
            get => _directionAngle;
            set
            {
                _directionAngle = value;
                Update();
            }
        }

        private Vector2 _startPosition;
        [Export]
        public Vector2 StartPosition
        {
            get => _startPosition;
            set
            {
                if (_startPosition != value)
                {
                    _startPosition = value;
                    if (Engine.EditorHint) { Position = value; PropertyListChangedNotify(); }
                }
            }
        }

        private Node2D _ballSet = null!;
        private Node2D _ballContainer = null!;
        private VisibilityNotifier2D _visibilityNotifier = null!;
        private bool _active = false;

        public override void _Ready()
        {
            if (Engine.EditorHint)
            {
                SetNotifyTransform(true);
                return;
            }

            _ballContainer = GetParent<Node2D>();
            _ballSet = _ballContainer.GetParent<Node2D>();
            _visibilityNotifier = GetNode<VisibilityNotifier2D>("VisibilityNotifier");
            Hide();
        }

        public override void _EnterTree()
        {
            AddToGroup(PowerBallGroup);
        }

        public override void _ExitTree()
        {
            RemoveFromGroup(PowerBallGroup);
        }

        public override void _PhysicsProcess(float delta)
        {
            if (Engine.EditorHint)
            {
                return; // TODO: can preview these things moving
            }

            if (!_active)
            {
                return;
            }

            var directionRadians = Numerology.DegToRad(DirectionAngle);
            Vector2 direction = new Vector2(Mathf.Cos(directionRadians), Mathf.Sin(directionRadians));
            Position += direction * Speed * delta;
            GetTree().CallGroup(PowerBallGroup, nameof(PowerBallWatcher.BallPositionUpdated), GetInstanceId(), Position + _ballContainer.Position + _ballSet.Position);
        }

        public void Activate()
        {
            Show();
            _active = true;
        }

        public void OnCollisionAreaEntered(Area2D area)
        {
            if (area.Name == "HornBox") // TODO: Something more type safe?
            {
                var player = area.GetParent().GetParent<Player>();
                player.OnHornTouched(this);
            }

            if (area.Name == "BallKillArea" || area.Name == "HornBox")
            {
                GetNode<Polygon2D>("Ball").Hide();
                var particlesNode = GetNode<Particles2D>("ExplosionParticles");
                var touchedTimer = GetNode<Timer>("TouchedTimer");
                touchedTimer.WaitTime = particlesNode.Lifetime;
                particlesNode.Emitting = true;
                touchedTimer.Start(); // Begin a countdown that lasts as long as the explosion particles.
            }
        }

        public void OnTouchedTimerTimeout()
        {
            Deactivate();
        }

        public void ScreenEntered()
        {
            GetTree().CallGroup(PowerBallGroup, nameof(PowerBallWatcher.BallEnteredScreen), GetInstanceId());
        }

        public void ScreenExited()
        {
            GetTree().CallGroup(PowerBallGroup, nameof(PowerBallWatcher.BallExitedScreen), GetInstanceId());
        }

        // --- Internal methods ---
        private void Deactivate()
        {
            Hide();
            _active = false;
            // TODO: Maybe add an arg describiing if it was hit by the player, or a cleanup area.
            GetTree().CallGroup(PowerBallGroup, BallDeactivatedMethod, GetInstanceId());
        }

        // ---Tool stuff---

        public override void _Notification(int what)
        {
            // Make dragging the node around in the editor change its start position.            
            if (what == NotificationTransformChanged)
            {
                StartPosition = Position;
            }
        }

        public override void _Draw()
        {
            if (!Engine.EditorHint)
            {
                return;
            }

            var directionRadians = Numerology.DegToRad(DirectionAngle);
            Vector2 direction = new Vector2(Mathf.Cos(directionRadians), Mathf.Sin(directionRadians));
            Vector2 secondPont = -direction * Speed;
            DrawLine(Vector2.Zero, secondPont, new Color(1f, 1f, 0f), 2f, true);
        }
    }
}


