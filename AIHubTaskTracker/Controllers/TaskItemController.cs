using AIHubTaskTracker.Data;
using AIHubTaskTracker.DTOs;
using AIHubTaskTracker.Models;
using AIHubTaskTracker.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/v1/tasks")]
public class TasksItemController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ClickUpService _clickUp;
    private readonly TelegramService _telegram;

    public TasksItemController(AppDbContext db, ClickUpService clickUp, TelegramService telegram)
    {
        _db = db;
        _clickUp = clickUp;
        _telegram = telegram;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskCreateDto dto)
    {
        try
        {
            string statusValue = dto.status ?? "TO DO";

            var task = new TaskItem
            {
                title = dto.title,
                description = dto.description,
                assigner_id = dto.assigner_id,
                assignee_id = dto.assignee_id,
                collaborators = dto.collaborators ?? new List<int>(),
                expected_output = dto.expected_output,
                deadline = dto.deadline,
                status = statusValue,
                progress_percentage = dto.progress_percentage,
                notion_link = dto.notion_link,
                clickup_id = dto.clickup_id,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _db.Tasks.Add(task);
            await _db.SaveChangesAsync();

            // Bỏ tạo task ClickUp trực tiếp từ backend
            await _telegram.SendMessageAsync($"✅ Task mới được tạo:\n*{task.title}*\nNgười giao: `{task.assigner_id}` → Người nhận: `{task.assignee_id}`");

            return Ok(task);
        }
        catch (Exception ex)
        {
            await _telegram.SendMessageAsync($"❌ Lỗi khi tạo task: {ex.Message}");
            return StatusCode(500, new { message = "Tạo task thất bại", error = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TaskUpdateDto dto)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        var oldStatus = task.status;
        var oldProgress = task.progress_percentage;
        string NormalizeStatus(string? status) => status?.Trim().ToLower() switch
        {
            "todo" => "To Do",
            "inprogress" => "In Progress",
            "completed" => "Completed",
            _ => task.status
        };
        // Cập nhật dữ liệu
        task.title = dto.title ?? task.title;
        task.description = dto.description ?? task.description;
        task.status = dto.status ?? task.status;
        task.expected_output = dto.expected_output ?? task.expected_output;
        task.deadline = dto.deadline ?? task.deadline;
        task.progress_percentage = dto.progress_percentage ?? task.progress_percentage;
        task.notion_link = dto.notion_link ?? task.notion_link;
        task.updated_at = DateTime.UtcNow;

        // Đồng bộ với ClickUp nếu có
        if (!string.IsNullOrEmpty(task.clickup_id))
        {
            try
            {
                await _clickUp.UpdateTaskAsync(task);
            }
            catch (HttpRequestException ex)
            {
                // Log và thông báo Telegram nhưng không crash PUT
                Console.WriteLine($"ClickUp update failed: {ex.Message}");
                await _telegram.SendMessageAsync($"⚠️ ClickUp update thất bại cho task *{task.title}*: {ex.Message}");
            }
        }

        await _db.SaveChangesAsync();

        // Gửi log Telegram
        if (oldStatus != task.status)
        {
            await _telegram.SendMessageAsync($"*{task.title}* đổi trạng thái: `{oldStatus}` → `{task.status}`");
        }
        else if (oldProgress != task.progress_percentage)
        {
            await _telegram.SendMessageAsync($"*{task.title}* cập nhật tiến độ: `{oldProgress}%` → `{task.progress_percentage}%`");
        }
        else
        {
            await _telegram.SendMessageAsync($"Task *{task.title}* vừa được cập nhật nội dung.");
        }

        // Nếu Completed thì gửi thông báo đặc biệt
        if (task.status == "Completed")
        {
            await _telegram.SendMessageAsync($"Hoàn thành – Task *{task.title}* đã done và đồng bộ ClickUp!");
        }

        return Ok(task);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? assignee_id)
    {
        var query = _db.Tasks.AsQueryable();

        if (assignee_id.HasValue && assignee_id.Value != 0)
            query = query.Where(t => t.assignee_id == assignee_id.Value);

        var tasks = await query.OrderByDescending(t => t.updated_at).ToListAsync();
        return Ok(tasks);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.task_id == id);
        if (task == null)
            return NotFound(new { message = $"Task ID {id} không tồn tại." });

        return Ok(task);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        var clickUpIdToDelete = task.clickup_id;
        string title = task.title;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(clickUpIdToDelete))
        {
            try
            {
                await _clickUp.DeleteTaskAsync(clickUpIdToDelete);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"ClickUp deletion failed: {ex.Message}");
                await _telegram.SendMessageAsync($"⚠️ ClickUp deletion thất bại cho task *{title}*: {ex.Message}");
            }
        }

        await _telegram.SendMessageAsync($"Task *{title}* đã bị xóa.");

        return Ok(new { message = "Xoá task thành công" });
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldTasks()
    {
        try
        {
            var oldTasks = await _db.Tasks
                .Where(t => t.title.Contains("[AIHUB_BACKEND]") ||
                            t.title.Contains("[ahub_backend]"))
                .ToListAsync();

            var count = oldTasks.Count;

            foreach (var task in oldTasks)
            {
                if (!string.IsNullOrEmpty(task.clickup_id))
                {
                    try
                    {
                        await _clickUp.DeleteTaskAsync(task.clickup_id);
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"ClickUp deletion failed: {ex.Message}");
                        await _telegram.SendMessageAsync($"⚠️ ClickUp deletion thất bại cho task *{task.title}*: {ex.Message}");
                    }
                }

                _db.Tasks.Remove(task);
            }

            await _db.SaveChangesAsync();
            await _telegram.SendMessageAsync($" Đã dọn dẹp {count} tasks cũ có tag [AIHUB_BACKEND]");

            return Ok(new { message = $"Đã xóa {count} tasks cũ", count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
