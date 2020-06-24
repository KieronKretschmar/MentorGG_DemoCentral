using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DemoCentral
{
    public class TimedDemoRemovalCaller : IHostedService, IDisposable
    {
        private readonly TimeSpan _interval;
        private readonly TimeSpan _allowedTimeAfterExpiration;
        private readonly IDemoRemover _demoRemover;
        private readonly ILogger<TimedDemoRemovalCaller> _logger;
        private Timer _timer;

        public TimedDemoRemovalCaller(
            TimeSpan interval,
            TimeSpan allowedTimeAfterRemoval,
            IDemoRemover demoRemover,
            ILogger<TimedDemoRemovalCaller> logger)
        {
            _interval = interval;
            _allowedTimeAfterExpiration = allowedTimeAfterRemoval;
            _demoRemover = demoRemover;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {GetType().Name} ...  - Interval {_interval.Days} days");
            _timer = new Timer(CallDemoRemoverAsync, null, TimeSpan.Zero, _interval);

            return Task.CompletedTask;
        }

        private void CallDemoRemoverAsync(object state)
        {
            _logger.LogInformation("Periodic user refresh");
            _demoRemover.RemoveExpiredDemos(_allowedTimeAfterExpiration);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopped {GetType().Name}");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

    }
}
