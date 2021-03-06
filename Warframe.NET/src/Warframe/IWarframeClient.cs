﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Warframe.World.Models;

namespace Warframe
{
    public interface IWarframeClient
    {
        event EventHandler<HttpRequestEventArgs> MakingHttpRequest;

        Task<IEnumerable<Alert>> GetAlertsAsync(CancellationToken cancellation = default);

        Task<IEnumerable<Invasion>> GetInvasionsAsync(CancellationToken cancellation = default);

        Task<EarthCycle> GetEarthStatus(CancellationToken cancellation = default);

        Task<CetusCycle> GetCetusStatus(CancellationToken cancellation = default);

        Task<VallisCycle> GetVallisStatus(CancellationToken cancellation = default);

        Task<CambionCycle> GetCambionStatus(CancellationToken cancellation = default);
    }
}