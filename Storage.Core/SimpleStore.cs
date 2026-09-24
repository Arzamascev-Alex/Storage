using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Text.Json;

namespace Storage.Core
{
    public class SimpleStore : IDisposable
    {
        private readonly Dictionary<string, byte[]> _storage = new();
        private readonly ReaderWriterLockSlim _lock = new();

        private long _setCount;
        private long _getCount;
        private long _deleteCount;

        public void Set(string key, UserProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(profile);

            _lock.EnterWriteLock();

            try 
            {
                _storage[key] = bytes;

                Interlocked.Increment(ref _setCount);

            }
            finally
            {
                _lock.ExitWriteLock();
            }
            
        }

        public UserProfile? Get(string key)
        {
            byte[]? bytes;

            _lock.EnterReadLock();

            try
            {
                _storage.TryGetValue(key, out bytes);

                Interlocked.Increment(ref _getCount);
            }
            finally
            {
                _lock.ExitReadLock ();
            }

            if (bytes is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<UserProfile>(bytes);
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
                    Interlocked.Read(ref _getCount),
                    Interlocked.Read(ref _deleteCount)
                );
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

    }
}
