using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Entities
{
    [Tool]
    public class PowerBallSet : Node2D
    {
        private Shape2D? _triggerShape;
        [Export]
        public Shape2D? TriggerShape
        {
            get => _triggerShape;
            set
            {
                _triggerShape = value;
                if (_triggerCollisionShape2D != null)
                {
                    _triggerCollisionShape2D.Shape = value;
                }
            }
        }

        private Area2D _triggerArea = null!;
        private CollisionShape2D _triggerCollisionShape2D = null!;
        private Node2D _bulletHolder = null!;
        private List<PowerBall> _powerBalls;

        private bool _triggered = false;

        public override void _Ready()
        {
            _triggerArea = GetNode<Area2D>("TriggerArea");
            _triggerCollisionShape2D = GetNode<CollisionShape2D>("TriggerArea/TriggerShape");
            _bulletHolder = GetNode<Node2D>("Bullets");

            if (Engine.EditorHint)
            {
                ToolReady();
            }

            // Both readies
            _powerBalls = _bulletHolder.GetChildren().Cast<PowerBall>().ToList();
            foreach (var ball in _powerBalls)
            {
                ball.Connect("FreeQueued", this, nameof(OnBallFreed));
            }
        }

        public void OnTriggerAreaBodyEntered(Node node)
        {
            if (_triggered)
            {
                return;
            }

            if (node is Player)
            {
                _triggered = true;
                foreach (var ball in _powerBalls)
                {
                    ball.Activate();
                }
            }
        }

        public void OnBallFreed(PowerBall ball)
        {
            _powerBalls.Remove(ball);
            ball = null!;
            if (!_powerBalls.Any())
            {
                QueueFree();
            }
        }

        // ---Tool stuff---

        public override string _GetConfigurationWarning()
        {
            var bulletChildren = _bulletHolder.GetChildren();
            if (bulletChildren.Count == 0)
            {
                return "Bullets must have at least one child.";
            }

            foreach (var child in bulletChildren.Cast<Node2D>())
            {
                if (!(child is PowerBall))
                {
                    return $"{child.Name} is not a PowerBall."; // TODO: Expand this type more in the future, probably
                }
            }

            return "";

        }

        private void ToolReady()
        {
            if (TriggerShape == null)
            {
                TriggerShape = new RectangleShape2D();
            }
        }

    }
}