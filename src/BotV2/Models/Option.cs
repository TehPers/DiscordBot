using System;

namespace BotV2.Models
{
    public struct Option<T>
    {
        public bool HasValue { get; }
        private readonly T _value;

        public Option(T value)
        {
            this.HasValue = true;
            this._value = value;
        }

        public bool TryGetValue(out T value)
        {
            value = this._value;
            return this.HasValue;
        }

        public Option<T> Where(Func<T, bool> predicate)
        {
            _ = predicate ?? throw new ArgumentNullException(nameof(predicate));
            return this.HasValue && predicate(this._value) ? this : default;
        }

        public Option<T2> Select<T2>(Func<T, T2> transform)
        {
            _ = transform ?? throw new ArgumentNullException(nameof(transform));
            return this.HasValue ? new Option<T2>(transform(this._value)) : default;
        }
    }
}