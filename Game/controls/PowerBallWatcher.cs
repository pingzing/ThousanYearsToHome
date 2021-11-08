using Godot;
using System;
using System.Collections.Generic;

namespace ThousandYearsHome.Entities
{
    public class PowerBallWatcher : Control
    {
        private Dictionary<ulong, Vector2> _trackedBalls = new Dictionary<ulong, Vector2>();

        public override void _EnterTree()
        {
            AddToGroup(PowerBall.PowerBallGroup);
        }

        public override void _ExitTree()
        {
            RemoveFromGroup(PowerBall.PowerBallGroup);
        }

        public override void _Ready()
        {
            
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _Process(float delta)
        {
            foreach (var kvp in _trackedBalls)
            {
                // TODO: Draw an arrow point toward each ball at the nearest edge of the screen
            }
        }

        public void BallPositionUpdated(ulong ballId, Vector2 position)
        {
            _trackedBalls[ballId] = position;
        }

        public void BallDeactivated(ulong ballId)
        {
            _trackedBalls.Remove(ballId);
        }

        public void BallEnteredScreen(ulong ballId)
        {
            // Update its entry in the list so we stop drawing its arrow
        }

        public void BallExitedScreen(ulong ballId)
        {
            // Update its entry in the list so we resume drawing its arrow
        }
    }
}