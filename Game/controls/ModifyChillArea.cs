using Godot;
using System;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Controls
{
    [Tool]
    public class ModifyChillArea : Area2D
    {

        // Exports

        private float _chillModifier = 0f;
        /// <summary>
        /// The amount that entering this area will adjust the warmth drain per tick.
        /// Can be positive or negative (i.e., a suitably negative value will result in 
        /// an area that grants a wamrth <em>gain</em> per tick).
        /// </summary>
        [Export]        
        public float ChillModifier
        {
            get => _chillModifier;
            set
            {
                _chillModifier = value;
                DebugDraw();
            }
        }

        // Local nodes
        private CollisionShape2D _modifyChillShape = null!;

        public override void _Ready()
        {
            _modifyChillShape = GetNode<CollisionShape2D>("ModifyChillShape");
        }

        public void ModifyChillAreaBodyEntered(Node body)
        {
            if (!(body is Player player))
            {
                return;
            }

            player.WarmthDrainPerTick += ChillModifier;
        }

        public void ModifyChillAreaBodyExited(Node body)
        {
            if (!(body is Player player))
            {
                return;
            }

            player.WarmthDrainPerTick -= ChillModifier;
        }

        private void DebugDraw()
        {
            if (Engine.EditorHint || GetTree().DebugCollisionsHint)
            {
                if (_modifyChillShape != null)
                {
                    if (_chillModifier > 0) { _modifyChillShape.Modulate = new Color("#ff0a48db"); }
                    if (_chillModifier < 0) { _modifyChillShape.Modulate = new Color("#ffdb930a"); }
                    if (_chillModifier == 0) { _modifyChillShape.Modulate = new Color("#ffffffff"); }
                    Update();
                }
            }
        }
    }
}