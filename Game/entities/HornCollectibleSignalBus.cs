using Godot;
using ThousandYearsHome.Entities.PowerBallEntity;
using ThousandYearsHome.Entities.WarmthBallEntity;

namespace ThousandYearsHome.Entities
{
    public class HornCollectibleSignalBus : Node
    {
        [Signal] public delegate void WarmthBallCollected(WarmthBall warmthBall);
        [Signal] public delegate void PowerBallCollected(PowerBall powerBall);
    }
}


