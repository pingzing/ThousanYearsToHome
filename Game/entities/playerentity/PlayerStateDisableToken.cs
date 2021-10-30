using System;

namespace ThousandYearsHome.Entities.PlayerEntity
{
    public class PlayerStateDisableToken : IDisposable
    {
        private bool _disposedValue;
        private Player? _player;

        public PlayerStateDisableToken(Player player)
        {
            _player = player;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _player?.ReenableStateMachine(this);
                }

                _player = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
