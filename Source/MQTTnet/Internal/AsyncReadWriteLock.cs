using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MQTTnet.Internal
{
    public class AsyncReadWriteLock : IDisposable
    {
        private readonly object _syncRoot = new object();
        private readonly Queue<ReadWriteLockWaiter> _readWaiters = new Queue<ReadWriteLockWaiter>(64);
        private readonly Queue<ReadWriteLockWaiter> _writeWaiters = new Queue<ReadWriteLockWaiter>();
        private readonly IDisposable _readReleaser;
        private readonly IDisposable _writeReleaser;
        private readonly Task<IDisposable> _readCompletedTask;
        private readonly Task<IDisposable> _writeCompletedTask;
        private int _readLockCount;
        private bool _isLockedForWrite;
        private bool _isDisposed;

        public AsyncReadWriteLock()
        {
            _readReleaser = new Releaser(this, true);
            _writeReleaser = new Releaser(this, false);
            _readCompletedTask = Task.FromResult(_readReleaser);
            _writeCompletedTask = Task.FromResult(_writeReleaser);
        }

        public Task<IDisposable> EnterReadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncReadWriteLock));
            }

            lock (_syncRoot)
            {
                if (!_isLockedForWrite && _writeWaiters.Count == 0)
                {
                    _readLockCount++;
                    return _readCompletedTask;
                }

                var waiter = new ReadWriteLockWaiter(cancellationToken);
                _readWaiters.Enqueue(waiter);

                return waiter.Task;
            }
        }

        public Task<IDisposable> EnterWriteAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(AsyncReadWriteLock));
            }

            lock (_syncRoot)
            {
                if (!_isLockedForWrite && _readLockCount <= 0)
                {
                    _isLockedForWrite = true;
                    return _writeCompletedTask;
                }

                var waiter = new ReadWriteLockWaiter(cancellationToken);
                _writeWaiters.Enqueue(waiter);

                return waiter.Task;
            }
        }

        private void ReleaseRead()
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    return;
                }

                _readLockCount = Math.Max(_readLockCount - 1, 0);

                ApproveNextWaiter();
            }
        }

        private void ReleaseWrite()
        {
            lock (_syncRoot)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isLockedForWrite = false;

                ApproveNextWaiter();
            }
        }

        private void ApproveNextWaiter()
        {
            while (_writeWaiters.Count > 0)
            {
                var waiter = _writeWaiters.Dequeue();
                var isApproved = waiter.Approve(_writeReleaser);
                waiter.Dispose();

                if (isApproved)
                {
                    _isLockedForWrite = true;
                    return;
                }
            }
            while (_readWaiters.Count > 0)
            {
                var waiter = _readWaiters.Dequeue();
                var isApproved = waiter.Approve(_readReleaser);
                waiter.Dispose();

                if (isApproved)
                {
                    _readLockCount++;
                    return;
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        sealed class ReadWriteLockWaiter : IDisposable
        {
            private readonly CancellationTokenRegistration _cancellationTokenRegistration;
            private readonly bool _hasCancellationRegistration;
            private readonly AsyncTaskCompletionSource<IDisposable> _promise = new AsyncTaskCompletionSource<IDisposable>();

            public ReadWriteLockWaiter(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (cancellationToken.CanBeCanceled)
                {
                    _cancellationTokenRegistration = cancellationToken.Register(Cancel);
                    _hasCancellationRegistration = true;
                }
            }

            public Task<IDisposable> Task => _promise.Task;

            public bool Approve(IDisposable scope)
            {
                if (scope == null)
                {
                    throw new ArgumentNullException(nameof(scope));
                }

                if (_promise.Task.IsCompleted)
                {
                    return false;
                }

                return _promise.TrySetResult(scope);
            }

            private void Cancel()
            {
                _promise.TrySetCanceled();
            }
            
            public void Dispose()
            {
                if (_hasCancellationRegistration)
                {
                    _cancellationTokenRegistration.Dispose();
                }

                _promise.TrySetCanceled();
            }

        }

        readonly struct Releaser : IDisposable
        {
            private readonly AsyncReadWriteLock _readWriteLock;
            private readonly bool _isRead;

            public Releaser(AsyncReadWriteLock readWriteLock, bool isRead)
            {
                _readWriteLock = readWriteLock;
                _isRead = isRead;
            }

            public void Dispose()
            {
                if (_isRead)
                    _readWriteLock.ReleaseRead();
                else
                    _readWriteLock.ReleaseWrite();
            }
        }

    }
}
