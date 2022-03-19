using Godot;
using System;

namespace ThousandYearsHome.Entities.PowerBallEntity
{
    public class PowerBall : Node2D
    {
        private HornCollectibleSignalBus _signalBus = null!;
        private CollisionShape2D _collisionShape = null!;
        private Sprite _sprite = null!;

        public override void _Ready()
        {
            _signalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _collisionShape = GetNode<CollisionShape2D>("CollisionArea/CollisionShape");
            _sprite = GetNode<Sprite>("Sprite");
        }

        public void OnCollisionAreaEntered(Area2D area)
        {
            if (area.Name == "HornBox") // TODO: Something a bit more robust
            {
                _signalBus.EmitSignal(nameof(HornCollectibleSignalBus.PowerBallCollected), this);
                _collisionShape.SetDeferred("disabled", true);
                _sprite.Hide();
                // Disable collision, hide sprite, play animation
            }
        }

        public void Respawn()
        {
            // Re-enable collision, re-show sprite
            _collisionShape.SetDeferred("disabled", false);
            _sprite.Show();
        }
    }
}


