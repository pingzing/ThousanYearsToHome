using Godot;
using ThousandYearsHome.Entities.PlayerEntity;

namespace ThousandYearsHome.Entities
{
    public class PlayerSignalBus : Node
    {
        [Signal] public delegate void WarmthChanged(float oldValue, float newValue);
        [Signal] public delegate void PositionChanged(Vector2 oldPos, Vector2 newPos);
        [Signal] public delegate void VelocityChanged(Vector2 oldVel, Vector2 newVel);
        [Signal] public delegate void StateChanged(PlayerStateKind oldState, PlayerStateKind newState);
    }
}
