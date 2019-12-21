using System;

namespace MQTTnet.Internal
{
    public class Disposable : IDisposable
    {
        protected bool IsDisposed => _isDisposed;

        protected void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }


        #region IDisposable Support
       
        private bool _isDisposed = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Disposable()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);

            _isDisposed = true;
        }
        #endregion
    }
}
