using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TehPers.Discord.TehBot.Permissions {
    public class SavingCollection<TBase> : ICollection<TBase> {
        private readonly ICollection<TBase> _baseCollection;
        private readonly Action _save;
        private readonly Func<TBase, TBase> _addFactory;

        public event EventHandler<SavingEventArgs> Added;
        public event EventHandler<SavingEventArgs> Removed;
        public event EventHandler<SavingEventArgs> Modified;

        public SavingCollection(ICollection<TBase> baseCollection, Action save, Func<TBase, TBase> addFactory = null) {
            _baseCollection = baseCollection;
            _save = save;
            _addFactory = addFactory ?? (item => item);
        }

        public IEnumerator<TBase> GetEnumerator() {
            return _baseCollection.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(TBase item) {
            _baseCollection.Add(_addFactory(item));
            _save();
            OnAdded(new SavingEventArgs(item));
        }

        public void Clear() {
            List<TBase> items = _baseCollection.ToList();
            _baseCollection.Clear();
            _save();

            foreach (TBase item in items)
                OnRemoved(new SavingEventArgs(item));
        }

        public bool Contains(TBase item) {
            return _baseCollection.Contains(item);
        }

        public void CopyTo(TBase[] array, int arrayIndex) {
            _baseCollection.CopyTo(array, arrayIndex);
        }

        public bool Remove(TBase item) {
            bool r = _baseCollection.Remove(item);
            _save();
            OnRemoved(new SavingEventArgs(item));
            return r;
        }

        public int Count => _baseCollection.Count;

        public bool IsReadOnly => _baseCollection.IsReadOnly;

        #region Events
        protected virtual void OnAdded(SavingEventArgs e) {
            Added?.Invoke(this, e);
            OnModified(e);
        }

        protected virtual void OnRemoved(SavingEventArgs e) {
            Removed?.Invoke(this, e);
            OnModified(e);
        }

        protected virtual void OnModified(SavingEventArgs e) {
            Modified?.Invoke(this, e);
        }
        #endregion

        public class SavingEventArgs : EventArgs {

            public TBase Affected;

            public SavingEventArgs(TBase affected) {
                this.Affected = affected;
            }
        }
    }
}