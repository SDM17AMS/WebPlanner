using System.Net.Http.Json;
using WebPlanner.Shared.DTOs;
using WebPlanner.Shared.Enums;

namespace WebPlanner.Client.Services;

public class TaskService
{
    private readonly HttpClient _http;

    public TaskService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<TaskDto>> GetTasksAsync(DateTime? date = null)
    {
        var url = date.HasValue ? $"api/tasks?date={date.Value:yyyy-MM-dd}" : "api/tasks";
        return await _http.GetFromJsonAsync<List<TaskDto>>(url) ?? new();
    }

    public async Task<TaskDto?> CreateAsync(CreateTaskRequest request)
    {
        var response = await _http.PostAsJsonAsync("api/tasks", request);
        return await response.Content.ReadFromJsonAsync<TaskDto>();
    }

    public async Task<bool> UpdateStatusAsync(Guid id, PlannerTaskStatus status)
    {
        var response = await _http.PatchAsync($"api/tasks/{id}/status?status={status}", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/tasks/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAsync(Guid id, CreateTaskRequest request)
    {
        var response = await _http.PutAsJsonAsync($"api/tasks/{id}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<string> GetJournalAsync(DateTime date)
    {
        var response = await _http.GetFromJsonAsync<JournalResponse>($"api/journal/{date:yyyy-MM-dd}");
        return response?.content ?? "";
    }

    public async Task SaveJournalAsync(DateTime date, string content)
    {
        await _http.PutAsJsonAsync($"api/journal/{date:yyyy-MM-dd}", new { Content = content });
    }

    public class JournalResponse
    {
        public string date { get; set; } = "";
        public string content { get; set; } = "";
    }

    public async Task<bool> GetHabitAsync(DateTime date)
    {
        var response = await _http.GetFromJsonAsync<HabitResponse>($"api/habits/{date:yyyy-MM-dd}");
        return response?.completed ?? false;
    }

    public async Task<bool> ToggleHabitAsync(DateTime date, bool completed)
    {
        if (completed)
        {
            var response = await _http.PostAsync($"api/habits/{date:yyyy-MM-dd}", null);
            return response.IsSuccessStatusCode;
        }
        else
        {
            var response = await _http.DeleteAsync($"api/habits/{date:yyyy-MM-dd}");
            return response.IsSuccessStatusCode;
        }
    }

    public class HabitResponse
    {
        public bool completed { get; set; }
    }
}