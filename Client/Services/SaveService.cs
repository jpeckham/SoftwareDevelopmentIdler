namespace SoftwareVSM.Client.Services;

using Microsoft.JSInterop;
using System.Text.Json;
using System.Threading.Tasks;
using SoftwareVSM.Client.GameEngine;

public class SaveService
{
    private readonly IJSRuntime _js;
    private readonly SimulationEngine _engine;

    public SaveService(IJSRuntime js, SimulationEngine engine)
    {
        _js = js;
        _engine = engine;
    }

    public async Task SaveAsync()
    {
        var json = JsonSerializer.Serialize(_engine.State);
        await _js.InvokeVoidAsync("localStorage.setItem", "vsm_save", json);
    }

    public async Task LoadAsync()
    {
        var json = await _js.InvokeAsync<string>("localStorage.getItem", "vsm_save");
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var state = JsonSerializer.Deserialize<GameState>(json);
                if (state != null)
                {
                    _engine.LoadState(state);
                }
            }
            catch { /* Ignore corrupted save data */ }
        }
    }
}
