using System;
using System.Collections.Generic;
using System.Text;

namespace Storage.Core
{
    public class SimpleStore
    {
        private readonly Dictionary<string, byte[]> _storage = new();

        public void Set(string key, byte[] value)
        {
            _storage[key] = value;
        }

        public byte[]? Get(string key)
        {
            return _storage.TryGetValue(key, out byte[]? value)
                ? value 
                : null;
        }

        public void Delete(string key)
        {
            _storage.Remove(key);
        }
    }
}
