using Godot;
using System.Collections.Generic;
using System.Linq;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class PlayerStateMachine : Node2D
    {
        private Dictionary<PlayerStateKind, PlayerStateBase> _states = new Dictionary<PlayerStateKind, PlayerStateBase>();

        private Player _player = null!;
        private PlayerStateBase _currentState = null!;
        public PlayerStateBase CurrentState => _currentState;

        public void Init(Player player)
        {
            _player = player;
            foreach (PlayerStateBase state in GetChildren()
                                                .Cast<Node>()
                                                .Where(x => x is PlayerStateBase)
                                                .Cast<PlayerStateBase>())
            {
                _states[state.StateKind] = state;
            }
            _currentState = _states[PlayerStateKind.Idle];
            _currentState.Enter(_player);
        }

        public PlayerStateKind Run()
        {
            PlayerStateKind? nextStateKind = _currentState?.Run(_player);
            if (nextStateKind != null)
            {
                PlayerStateBase? nextState = _states[nextStateKind.Value];
                _currentState!.Exit(_player);
                _currentState = nextState;
                _currentState.Enter(_player);
            }

            return _currentState!.StateKind;
        }
    }
}


