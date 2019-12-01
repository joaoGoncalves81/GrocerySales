using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SalesWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEntryPointSalesWorkerService _entrySalesWorkerService;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public Worker(ILogger<Worker> logger,
                      IEntryPointSalesWorkerService entrySalesWorkerService,
                      IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            _entrySalesWorkerService = entrySalesWorkerService;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await _entrySalesWorkerService.ExecuteAsync();
            
            _logger.LogInformation("Worker finished at:  {time}", DateTimeOffset.Now);
            _hostApplicationLifetime.StopApplication();
        }
    }
}
