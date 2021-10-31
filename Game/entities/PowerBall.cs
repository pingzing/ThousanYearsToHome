using Godot;
using ThousandYearsHome.Entities.PlayerEntity;
using ThousandYearsHome.Extensions;

namespace ThousandYearsHome.Entities
{
    [Tool]
    public class PowerBall : Node2D
    {
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

        // TODO: Add an arg that describes if this was collected or hit a cleanup-zone
        [Signal] public delegate void Hidden(PowerBall ball);

        private bool _active = false;

        public override void _Ready()
        {
            if (Engine.EditorHint)
            {
                SetNotifyTransform(true);
                return;
            }

            Hide();
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
                GetNode<Polygon2D>("Ball").Hide();
                var particlesNode = GetNode<Particles2D>("ExplosionParticles");
                var touchedTimer = GetNode<Timer>("TouchedTimer");
                touchedTimer.WaitTime = particlesNode.Lifetime;
                particlesNode.Emitting = true;                
                touchedTimer.Start(); // Begin a countdown that lasts as long as the explosion particles.
            }

            if (area.Name == "BallKillArea")
            {
                Hide();
                _active = false;
                EmitSignal(nameof(Hidden), this);
            }
        }

        public void OnTouchedTimerTimeout()
        {
            Hide();
            _active = false;
            EmitSignal(nameof(Hidden), this);
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


