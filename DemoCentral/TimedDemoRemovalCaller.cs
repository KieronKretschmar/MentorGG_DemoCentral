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
        private readonly TimeSpan _allowance;
        private readonly IDemoRemover _demoRemover;
        private readonly ILogger<TimedDemoRemovalCaller> _logger;
        private Timer _timer;

        /// <summary>
        /// Initial Timer Delay, in seconds.
        /// </summary>
        private const int INITIAL_DELAY = 10;

        public TimedDemoRemovalCaller(
            TimeSpan interval,
            TimeSpan allowance,
            IDemoRemover demoRemover,
            ILogger<TimedDemoRemovalCaller> logger)
        {
            _interval = interval;
            _allowance = allowance;
            _demoRemover = demoRemover;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Starting {GetType().Name}. Interval:  [ {_interval} ]");
            _timer = new Timer(CallDemoRemoverAsync, null, TimeSpan.FromSeconds(INITIAL_DELAY), _interval);

            return Task.CompletedTask;
        }

        private async void CallDemoRemoverAsync(object state)
        {
            await _demoRemover.RemoveExpiredDemos(_allowance);
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
