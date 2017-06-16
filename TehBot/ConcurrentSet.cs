using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Discord.TehBot
{
    public class ConcurrentSet<T> : ISet<T> {
        private readonly ConcurrentDictionary<T, byte> _storage;

        public ConcurrentSet() {
            _storage = new ConcurrentDictionary<T, byte>();
        }

        public ConcurrentSet(IEnumerable<T> collection) {
            _storage = new ConcurrentDictionary<T, byte>(collection.ToDictionary(e => e, e => (byte) 0));
        }

        public IEnumerator<T> GetEnumerator() => _storage.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _storage.Keys.GetEnumerator();

        public void Add(T item) => (this as ISet<T>).Add(item);

        public void UnionWith(IEnumerable<T> other) {
            foreach (T element in other)
                _storage.TryAdd(element, 0);
        }

        public void IntersectWith(IEnumerable<T> other) {
            HashSet<T> trimmed = new HashSet<T>(_storage.Keys);
            trimmed.ExceptWith(other);
            foreach (T element in trimmed)
                _storage.TryRemove(element, out byte _);
        }

        public void ExceptWith(IEnumerable<T> other) {
            HashSet<T> trimmed = new HashSet<T>(_storage.Keys);
            trimmed.IntersectWith(other);
            foreach (T element in trimmed)
                _storage.TryRemove(element, out byte _);
        }

        public void SymmetricExceptWith(IEnumerable<T> other) {
            HashSet<T> trimmed = new HashSet<T>(_storage.Keys);
            trimmed.IntersectWith(other);

            HashSet<T> added = new HashSet<T>(other);
            added.ExceptWith(_storage.Keys);

            foreach (T element in trimmed)
                Remove(element);
            
            foreach (T element in added)
                Add(element);
        }

        public bool IsSubsetOf(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).IsSupersetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).IsProperSupersetOf(other);

        public bool IsProperSubsetOf(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).IsProperSubsetOf(other);

        public bool Overlaps(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) => new HashSet<T>(_storage.Keys).SetEquals(other);

        bool ISet<T>.Add(T item) => _storage.TryAdd(item, 0);

        public void Clear() => _storage.Clear();

        public bool Contains(T item) => _storage.ContainsKey(item);

        public void CopyTo(T[] array, int arrayIndex) => _storage.Keys.CopyTo(array, arrayIndex);

        public bool Remove(T item) => _storage.TryRemove(item, out byte _);

        public int Count => _storage.Count;
        public bool IsReadOnly => false;
    }
}
