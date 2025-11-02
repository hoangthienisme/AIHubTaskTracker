using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using AIHubTaskTracker.Data;
using System.Text;
using AIHubTaskTracker.Services;

public class DailyLogJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DailyLogJob> _logger;
    private readonly TelegramService _telegram;

    public DailyLogJob(IServiceProvider services, ILogger<DailyLogJob> logger, TelegramService telegram)
    {
        _services = services;
        _logger = logger;
        _telegram = telegram;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(" DailyLogJob started at: {time}", DateTime.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now.Hour == 22)
            {
                await SendDailyLogReport();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }

    private async Task SendDailyLogReport()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);
        var todayVN = nowVN.Date;
        var todayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVN, vnZone);

        var logs = await db.Logs
            .Include(l => l.user)
            .Where(l => l.created_at >= todayUtc)
            .OrderByDescending(l => l.created_at)
            .ToListAsync();

        if (!logs.Any())
        {
            await _telegram.SendMessageAsync(" *No logs recorded today.*");
            _logger.LogInformation("No logs to send at {time}", DateTime.Now);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine(" *DAILY LOG REPORT*");
        sb.AppendLine($" {DateTime.Now:dd/MM/yyyy HH:mm}\n");

        foreach (var log in logs.Take(20)) // chỉ lấy 20 log gần nhất
        {
            sb.AppendLine($"- [{log.severity}] {log.content} (by {log.user?.full_name ?? "Unknown"})");
        }

        await _telegram.SendMessageAsync(sb.ToString());
        _logger.LogInformation(" Sent daily log report with {Count} logs", logs.Count);
    }
}
