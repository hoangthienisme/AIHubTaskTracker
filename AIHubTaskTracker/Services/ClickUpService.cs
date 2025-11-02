using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIHubTaskTracker.Models;

public class ClickUpService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    private readonly string _listId;

    public ClickUpService(HttpClient httpClient, string apiToken, string listId)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
        _listId = listId ?? throw new ArgumentNullException(nameof(listId));
        if (!_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
        }
    }

    public async Task<string?> CreateTaskAsync(TaskItem task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));

        long? unixDeadlineMs = task.deadline.HasValue
            ? new DateTimeOffset(task.deadline.Value.ToUniversalTime()).ToUnixTimeMilliseconds()
            : (long?)null;

        var body = new
        {
            name = task.title,
            description = task.description,
            assignees = Array.Empty<int>(), 
            status = task.status,
            due_date = unixDeadlineMs
        };

        var jsonPayload = JsonSerializer.Serialize(body);
        var url = $"https://api.clickup.com/api/v2/list/{_listId}/task";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            var respText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ClickUp Create Error ({response.StatusCode}): {respText}");
                throw new HttpRequestException($"ClickUp creation failed. Status: {response.StatusCode}. Response: {respText}");
            }

            using var doc = JsonDocument.Parse(respText);
            if (doc.RootElement.TryGetProperty("id", out var idElement))
                return idElement.GetString();

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClickUp Create Exception: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> UpdateTaskAsync(TaskItem task)
    {
        if (task == null) throw new ArgumentNullException(nameof(task));
        if (string.IsNullOrEmpty(task.clickup_id)) return false;

        long? unixDeadlineMs = task.deadline.HasValue
            ? new DateTimeOffset(task.deadline.Value.ToUniversalTime()).ToUnixTimeMilliseconds()
            : (long?)null;

        var updatePayload = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(task.title)) updatePayload["name"] = task.title;
        if (!string.IsNullOrEmpty(task.description)) updatePayload["description"] = task.description;
        if (!string.IsNullOrEmpty(task.status)) updatePayload["status"] = task.status;
        updatePayload["due_date"] = unixDeadlineMs;

        var jsonPayload = JsonSerializer.Serialize(updatePayload);
        var url = $"https://api.clickup.com/api/v2/task/{task.clickup_id}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            var respText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ClickUp Update Error ({response.StatusCode}): {respText}");
                throw new HttpRequestException($"ClickUp update failed. Status: {response.StatusCode}. Response: {respText}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClickUp Update Exception: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> DeleteTaskAsync(string clickupId)
    {
        if (string.IsNullOrEmpty(clickupId)) return false;

        var url = $"https://api.clickup.com/api/v2/task/{clickupId}";

        using var request = new HttpRequestMessage(HttpMethod.Delete, url);

        try
        {
            var response = await _httpClient.SendAsync(request);
            var respText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                Console.WriteLine($"ClickUp Delete Error ({response.StatusCode}): {respText}");
                throw new HttpRequestException($"ClickUp deletion failed. Status: {response.StatusCode}. Response: {respText}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClickUp Delete Exception: {ex.Message}");
            throw;
        }
    }
    public async Task<bool> AddTagToTaskAsync(string clickupId, string tagName)
    {
        if (string.IsNullOrEmpty(clickupId) || string.IsNullOrEmpty(tagName)) return false;

        var encodedTagName = Uri.EscapeDataString(tagName);
        var url = $"https://api.clickup.com/api/v2/task/{clickupId}/tag/{encodedTagName}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };

        try
        {
            var response = await _httpClient.SendAsync(request);
            var respText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"ClickUp Tagging Error ({response.StatusCode}) for tag '{tagName}': {respText}");
                throw new HttpRequestException($"ClickUp add tag failed. Status: {response.StatusCode}. Response: {respText}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ClickUp AddTag Exception: {ex.Message}");
            throw;
        }
    }
}
