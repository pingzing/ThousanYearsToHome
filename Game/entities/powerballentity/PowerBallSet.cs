using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Entities.PowerBallEntity
{
    [Tool]
    public class PowerBallSet : Node2D
    {
        private Node2D _bulletHolder = null!;
        private List<PowerBall> _powerBalls = null!;

        private bool _triggered = false;

        public override void _Ready()
        {
            _bulletHolder = GetNode<Node2D>("Bullets");

            // Both readies
            _powerBalls = _bulletHolder.GetChildren().Cast<PowerBall>().ToList();
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

        // ---Tool stuff---

        public override string _GetConfigurationWarning()
        {
            var bulletChildren = _bulletHolder.GetChildren();
            if (bulletChildren.Count == 0)
            {
                return "The 'Bullets' node must have at least one child.";
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
    }
}