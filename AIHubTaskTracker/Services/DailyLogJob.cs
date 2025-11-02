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
        _logger.LogInformation("DailyLogJob started at: {time}", DateTime.Now);

        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

        while (!stoppingToken.IsCancellationRequested)
        {
            var nowVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);

            // Tính thời gian còn lại đến 22:00:00 hôm nay
            var today22 = nowVN.Date.AddHours(22);
            TimeSpan delayTime;

            if (nowVN < today22)
            {
                delayTime = today22 - nowVN;
            }
            else
            {
                // Nếu đã qua 22h hôm nay, delay đến 22h ngày mai
                delayTime = today22.AddDays(1) - nowVN;
            }

            _logger.LogInformation("Next daily log will send in {delay} minutes", delayTime.TotalMinutes);

            // Delay đến 22h tiếp theo
            await Task.Delay(delayTime, stoppingToken);

            // Gửi báo cáo
            await SendDailyLogReport();
        }
    }

    private async Task SendDailyLogReport()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Lấy thời gian VN hiện tại
        var vnZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        var nowVN = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnZone);
        var todayVN = nowVN.Date;
        var todayUtc = TimeZoneInfo.ConvertTimeToUtc(todayVN, vnZone);

        var logs = await db.Logs
            .Include(l => l.user)
            .Where(l => l.created_at >= todayUtc)
            .OrderByDescending(l => l.created_at)
            .Take(20) // chỉ lấy 20 log gần nhất
            .ToListAsync();

        if (!logs.Any())
        {
            await _telegram.SendMessageAsync("*No logs recorded today.*");
            _logger.LogInformation("No logs to send at {time}", DateTime.Now);
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine("*DAILY LOG REPORT*");
        sb.AppendLine($"_{DateTime.Now:dd/MM/yyyy HH:mm}_\n");

        foreach (var log in logs)
        {
            sb.AppendLine($"- [{log.severity}] {log.content} (by {log.user?.full_name ?? "Unknown"})");
        }

        await _telegram.SendMessageAsync(sb.ToString());
        _logger.LogInformation("Sent daily log report with {Count} logs", logs.Count);
    }
}
