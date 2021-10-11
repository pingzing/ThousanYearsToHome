using Godot;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Entities
{
    [Tool]
    public class PowerBall : Node2D
    {
        [Export] private float Speed = 150f;
        [Export] private Vector2 Direction = Vector2.Left;

        private bool _active = false;

        public override void _Ready()
        {
            if (Engine.EditorHint)
            {
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

            Position += Direction * Speed * delta;
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
                QueueFree();
            }
        }

        public void OnTouchedTimerTimeout()
        {
            // Remove the node at the end of the countdown.
            QueueFree();
        }
    }
}


