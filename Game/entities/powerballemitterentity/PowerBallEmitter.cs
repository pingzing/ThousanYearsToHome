using Godot;
using System;
using ThousandYearsHome.Entities.PowerBallEntity;

namespace ThousandYearsHome.Entities.PowerBallEmitterEntity
{
    public class PowerBallEmitter : Node2D
    {
        [Export] public float BallRespawnTime = 10f;

        private Timer _respawnTimer = null!;
        private PowerBall _emittedBall = null!;
        private HornCollectibleSignalBus _signalBus = null!;

        public override void _Ready()
        {
            _signalBus = GetNode<HornCollectibleSignalBus>("/root/HornCollectibleSignalBus");
            _signalBus.Connect(nameof(HornCollectibleSignalBus.WarmthBallCollected), this, nameof(WarmthBallCollected));

            _emittedBall = GetNode<PowerBall>("PowerBall");
            _respawnTimer = GetNode<Timer>("RespawnTimer");
            _respawnTimer.WaitTime = BallRespawnTime;
        }

        public void WarmthBallCollected(PowerBall ball)
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
