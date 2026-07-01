using Microsoft.JSInterop;

namespace WebPlanner.Client.Services;

public class ThemeService
{
    private readonly IJSRuntime _js;
    private bool _isDark = true;

    public ThemeService(IJSRuntime js)
    {
        _js = js;
    }

    public bool IsDark => _isDark;

    public async Task InitAsync()
    {
        var saved = await _js.InvokeAsync<string?>("localStorage.getItem", "theme");
        _isDark = saved != "light";
        await ApplyAsync();
    }

    public async Task ToggleAsync()
    {
        _isDark = !_isDark;
        await ApplyAsync();
    }

    private async Task ApplyAsync()
    {
        await _js.InvokeVoidAsync("eval", $"document.documentElement.setAttribute('data-theme', '{(_isDark ? "dark" : "light")}')");
        await _js.InvokeVoidAsync("localStorage.setItem", "theme", _isDark ? "dark" : "light");
    }
}