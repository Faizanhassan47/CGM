using System.Net.Http.Json;
namespace CGM.PatientApp.Services.Cgm;
public sealed record NotificationEndpoint(long Id,string Channel,string Address,bool Enabled);
public interface INotificationEndpointService
{
    Task RegisterAsync(string channel,string address,CancellationToken ct=default);
    Task<IReadOnlyList<NotificationEndpoint>> GetAsync(CancellationToken ct=default);
    Task RemoveAsync(long id,CancellationToken ct=default);
}
public sealed class ApiNotificationEndpointService(HttpClient client):INotificationEndpointService
{
    public async Task RegisterAsync(string channel,string address,CancellationToken ct=default)
    {using var response=await client.PostAsJsonAsync("api/notification-endpoints",new{Channel=channel,Address=address,Enabled=true},ct);response.EnsureSuccessStatusCode();}
    public async Task<IReadOnlyList<NotificationEndpoint>> GetAsync(CancellationToken ct=default)=>
        await client.GetFromJsonAsync<List<NotificationEndpoint>>("api/notification-endpoints",ct)??[];
    public async Task RemoveAsync(long id,CancellationToken ct=default)
    {using var response=await client.DeleteAsync($"api/notification-endpoints/{id}",ct);response.EnsureSuccessStatusCode();}
}
