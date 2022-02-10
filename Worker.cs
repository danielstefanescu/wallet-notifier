using System;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitForNextSchedule("*/2 * * * *");
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

        }

    }
    private async Task WaitForNextSchedule(string cronExpression)
    {
        var parsedExp = CronExpression.Parse(cronExpression);
        var currentUtcTime = DateTimeOffset.UtcNow.UtcDateTime;
        var occurenceTime = parsedExp.GetNextOccurrence(currentUtcTime);
        var delay = occurenceTime.GetValueOrDefault() - currentUtcTime;
        _logger.LogInformation("The run is delayed for {delay}. Current time: {time}", delay, DateTimeOffset.Now);
        await Task.Delay(delay);
    }
}
