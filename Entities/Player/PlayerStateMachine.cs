using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ThousandYearsHome.Entities.Player
{
    public class PlayerStateMachine : Node2D
    {
        private Dictionary<PlayerState, PlayerStateBase> _states = new Dictionary<PlayerState, PlayerStateBase>();

        private Player _player = null!;
        private PlayerStateBase _currentState = null!;

        public void Init(Player player)
        {
            _player = player;
            foreach (PlayerStateBase state in GetChildren()
                                                .Cast<Node>()
                                                .Where(x => x is PlayerStateBase)
                                                .Cast<PlayerStateBase>())
            {
                _states[state.StateVariant] = state;
            }
            _currentState = _states[PlayerState.Idle];
            _currentState.Enter(_player);
        }

        public void Run()
        {
            PlayerState? nextState = _currentState?.Run(_player);
            if (nextState != null)
            {
                ChangeState(nextState.Value);
            }
        }

        public void ChangeState(PlayerState newState)
        {
            PlayerStateBase? nextState = _states[newState];
            _currentState.Exit(_player);
            _currentState = nextState;
            _currentState.Enter(_player);
        }
    }
}


