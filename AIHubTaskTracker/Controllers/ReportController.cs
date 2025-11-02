using AIHubTaskTracker.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportsController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _botToken;
    private readonly string _chatId;

    public ReportsController(
        AppDbContext db,
        ILogger<ReportsController> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        _botToken = config["Telegram:BotToken"]!;
        _chatId = config["Telegram:ChatId"]!;
    }

    // POST /api/v1/reports/telegram-trigger
    [HttpPost("telegram-trigger")]
    public async Task<IActionResult> TelegramTrigger()
    {
        try
        {
            // Lấy 10 log mới nhất
            var logs = await _db.Logs
                .OrderByDescending(l => l.created_at)
                .Take(10)
                .ToListAsync();

            if (!logs.Any())
                return NotFound(new { message = "Không có log nào để gửi." });

            // Tạo nội dung báo cáo
            var text = new StringBuilder();
            text.AppendLine("📢 *BÁO CÁO HỆ THỐNG – LOG GẦN NHẤT*");
            text.AppendLine();
            foreach (var log in logs)
            {
                text.AppendLine($"🧩 `{log.created_at:dd/MM HH:mm}` – *{log.log_type}* ({log.severity})");
                text.AppendLine($"{log.content}");
                text.AppendLine();
            }

            var payload = new
            {
                chat_id = _chatId,
                text = text.ToString(),
                parse_mode = "Markdown"
            };

            var response = await _httpClient.PostAsync(
                $"https://api.telegram.org/bot{_botToken}/sendMessage",
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("✅ Telegram report sent successfully with {Count} logs", logs.Count);

            return Ok(new { message = "Telegram report sent successfully", count = logs.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to trigger Telegram report");
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
