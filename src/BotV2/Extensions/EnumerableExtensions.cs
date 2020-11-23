using System;
using System.Collections.Generic;
using System.Text;

namespace BotV2.Extensions
{
    public static class EnumerableExtensions
    {

        public static IEnumerable<IEnumerable<T>> Paged<T>(this IEnumerable<T> source, int pageSize)
        {
            _ = source ?? throw new ArgumentNullException(nameof(source));
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            bool itemsLeft;
            using (var enumerator = source.GetEnumerator())
            {
                itemsLeft = enumerator.MoveNext();
                while (itemsLeft)
                {
                    yield return GetGroup(enumerator);
                }
            }

            IEnumerable<T> GetGroup(IEnumerator<T> e)
            {
                var itemsRemaining = pageSize;
                while (itemsRemaining-- > 0 && itemsLeft)
                {
                    yield return e.Current;
                    itemsLeft = e.MoveNext();
                }
            }
        }
    }
}
