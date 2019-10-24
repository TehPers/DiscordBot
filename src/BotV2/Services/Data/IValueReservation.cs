using System;
using System.Threading.Tasks;
using BotV2.Models;

namespace BotV2.Services.Data
{
    public interface IValueReservation<T> : IAsyncDisposable
    {
        Task<Option<T>> Set(T value);

        Task<Option<T>> Get();

        Task<bool> Delete();

        Task<bool> ExtendReservation(TimeSpan addedTime);
    }
}
