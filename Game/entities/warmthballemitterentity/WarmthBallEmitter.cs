using Godot;
using System;

namespace ThousandYearsHome.Entities.WarmthBallEntity
{
    public class WarmthBallEmitter : Node2D
    {
        [Export] public float BallRespawnTime = 10f;

        private Timer _respawnTimer = null!;
        private WarmthBall _emittedBall = null!;
        private HornCollectibleSignalBus _signalBus = null!;

        public override void _Ready()
        {
            _signalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _signalBus.Connect(nameof(HornCollectibleSignalBus.WarmthBallCollected), this, nameof(WarmthBallCollected));

            _emittedBall = GetNode<WarmthBall>("WarmthBall");
            _respawnTimer = GetNode<Timer>("RespawnTimer");
            _respawnTimer.WaitTime = BallRespawnTime;
        }

        public void WarmthBallCollected(WarmthBall ball)
        {
            if (ball != _emittedBall)
            {
                return;
            }

            _respawnTimer.Start(BallRespawnTime);
        }

        public void OnRespawnTimeout()
        {
            _emittedBall.Respawn();
        }
    }
}
