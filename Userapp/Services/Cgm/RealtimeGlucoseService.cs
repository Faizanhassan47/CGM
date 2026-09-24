using Microsoft.AspNetCore.SignalR.Client;
using CGM.PatientApp.Models;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services.Cgm;

public class RealtimeGlucoseService : IAsyncDisposable
{
    private HubConnection? _hubConnection;
    private readonly string _hubUrl;

    public event EventHandler<GlucoseMeasurement>? GlucoseReadingReceived;

    public RealtimeGlucoseService()
    {
        // Assuming the API is running on localhost or a known domain
        _hubUrl = "http://localhost:5232/hubs/glucose"; // Adjust this based on environment config
    }

    public async Task StartAsync()
    {
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<object>("ReceiveGlucoseUpdate", (data) =>
        {
            // Parse the data and trigger the event
            // data will contain { UserId, GlucoseValue, MeasurementTime, SensorId }
            // For now, we can just trigger a general refresh or parse the dict
            GlucoseReadingReceived?.Invoke(this, new GlucoseMeasurement());
        });

        try
        {
            await _hubConnection.StartAsync();
        }
        catch (Exception)
        {
            // Handle connection failure silently
        }
    }

    public async Task JoinFamilyGroupAsync(int familyId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinFamilyGroup", familyId.ToString());
        }
    }

    public async Task LeaveFamilyGroupAsync(int familyId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveFamilyGroup", familyId.ToString());
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection is not null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
