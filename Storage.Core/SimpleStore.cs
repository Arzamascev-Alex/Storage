using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace Storage.Core
{
    public class SimpleStore : IDisposable
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private long _setCount;
        private long _getCount;
        private long _deleteCount;

        public void Set(string key, byte[] value)
        {
            _lock.EnterWriteLock();

            try 
            {
                _storage[key] = value;

                Interlocked.Increment(ref _setCount);

            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
        }

        public byte[]? Get(string key)
        {
            _lock.EnterReadLock();

            try
            {
                _storage.TryGetValue(key, out byte[]? value);

                Interlocked.Increment(ref _getCount);

                return value;
            }
            finally
            {
                _lock.ExitWriteLock ();
            }
            
        }

        public void Delete(string key)
        {
            _lock.EnterWriteLock();

            try
            {
                _storage.Remove(key);

                Interlocked.Increment(ref _deleteCount);
            }
            finally
            {
                _lock.ExitWriteLock ();
            }

        }

        public (long setCount, long getCount, long deleteCount) GetStatistics()
        {
            return
                (
                    Interlocked.Read(ref _setCount),
                    Interlocked.Read(ref _setCount),
                    Interlocked.Read(ref _deleteCount)
                );
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

    }
}
