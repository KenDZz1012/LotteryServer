using LotteryServer.Interfaces;
using LotteryServer.Models.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LotteryServer.Cronjob
{
    public class ResultNorthCronjob : BackgroundService
    {
        private CrontabSchedule _schedule;
        private DateTime _nextRun;
        public readonly IResultRepository _result;
        private readonly ILogger<ResultNorthCronjob> _logger;
        public ResultNorthCronjob(IResultRepository resultRepository, ILogger<ResultNorthCronjob> logger)
        {
            _result = resultRepository;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                if (now.Hour == 18 && now.Minute == 59)
                {
                    await _result.AutoAddResultNorth();
                    await _result.AutoAddResultSouth();
                    await _result.AutoAddResultTrung();
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stop service");
            return Task.CompletedTask;
        }

    }
}
