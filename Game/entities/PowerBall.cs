using Godot;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Entities
{
    public class PowerBall : Node2D
    {
        [Export] private float Speed = 150f;
        [Export] private Vector2 Direction = Vector2.Left;

        public override void _Ready()
        {

        }

        public override void _PhysicsProcess(float delta)
        {
            Position += Direction * Speed * delta;
        }

        public void OnCollisionAreaEntered(Area2D area)
        {
            if (area.Name == "HornBox")
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


