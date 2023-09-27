using Godot;

namespace ThousandYearsHome.Entities.BlowingRockEntity
{
    public class BlowingRock : Node2D
    {
        private CollisionShape2D _rockCollisionShape = null!;

        public override void _Ready()
        {
            _rockCollisionShape = GetNode<CollisionShape2D>("RockCollisionShape");
        }

        public override void _PhysicsProcess(float delta)
        {
            // TODO: make this value customizable, and part of some init process that the emitter performs
            Vector2 direction = new Vector2(-1, 0);
            float speed = 100f;
            Position += direction * speed * delta;
        }

        public void OnCollisionBodyEntered(Node body)
        {
            if (body.Name == "Player")
            {
                // TODO: push the player back, (or let the wind do it)
                // lock their controls for a moment, and maybe sap some warmth
            }
        }
    }
}

