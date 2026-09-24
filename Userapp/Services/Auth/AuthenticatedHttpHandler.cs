using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
namespace CGM.PatientApp.Services.Auth;
public sealed class AuthenticatedHttpHandler(Uri baseAddress) : DelegatingHandler(new HttpClientHandler())
{
    private const string AccessKey="cgm_access_token", RefreshKey="cgm_refresh_token";
    private readonly SemaphoreSlim _gate=new(1,1);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        var copy=await Snapshot.Take(request,ct); var token=await SecureStorage.Default.GetAsync(AccessKey);
        if(!string.IsNullOrWhiteSpace(token)&&request.Headers.Authorization is null) request.Headers.Authorization=new("Bearer",token);
        var response=await base.SendAsync(request,ct);
        if(response.StatusCode!=HttpStatusCode.Unauthorized||string.IsNullOrWhiteSpace(token)) return response;
        response.Dispose(); if(!await Refresh(token,ct)) return new(HttpStatusCode.Unauthorized);
        var retry=copy.Create(); retry.Headers.Authorization=new("Bearer",await SecureStorage.Default.GetAsync(AccessKey));
        return await base.SendAsync(retry,ct);
    }
    private async Task<bool> Refresh(string rejected,CancellationToken ct)
    {
        await _gate.WaitAsync(ct); try {
            var current=await SecureStorage.Default.GetAsync(AccessKey); if(current!=rejected) return !string.IsNullOrWhiteSpace(current);
            var refresh=await SecureStorage.Default.GetAsync(RefreshKey); if(string.IsNullOrWhiteSpace(refresh)) return false;
            using var req=new HttpRequestMessage(HttpMethod.Post,new Uri(baseAddress,"api/auth/refresh-token")){Content=JsonContent.Create(new{RefreshToken=refresh})};
            using var res=await base.SendAsync(req,ct);
            if(!res.IsSuccessStatusCode)
            {
                Clear();
                AuthenticationStateService.Current.NotifySessionExpired();
                return false;
            }
            var body=await res.Content.ReadFromJsonAsync<RefreshDto>(cancellationToken:ct);
            if(string.IsNullOrWhiteSpace(body?.AccessToken)||string.IsNullOrWhiteSpace(body.RefreshToken))
            {
                Clear();
                AuthenticationStateService.Current.NotifySessionExpired();
                return false;
            }
            await SecureStorage.Default.SetAsync(AccessKey,body.AccessToken); await SecureStorage.Default.SetAsync(RefreshKey,body.RefreshToken); return true;
        } finally {_gate.Release();}
    }
    private static void Clear(){SecureStorage.Default.Remove(AccessKey);SecureStorage.Default.Remove(RefreshKey);}
    private sealed record RefreshDto(string? AccessToken,string? RefreshToken);
    private sealed record Snapshot(HttpMethod Method,Uri? Uri,Dictionary<string,IEnumerable<string>> Headers,byte[]? Body,string? Type)
    {
        public static async Task<Snapshot> Take(HttpRequestMessage r,CancellationToken ct)=>new(r.Method,r.RequestUri,r.Headers.ToDictionary(x=>x.Key,x=>x.Value),r.Content is null?null:await r.Content.ReadAsByteArrayAsync(ct),r.Content?.Headers.ContentType?.ToString());
        public HttpRequestMessage Create(){var r=new HttpRequestMessage(Method,Uri);foreach(var h in Headers)r.Headers.TryAddWithoutValidation(h.Key,h.Value);if(Body is not null){r.Content=new ByteArrayContent(Body);if(Type is not null)r.Content.Headers.ContentType=MediaTypeHeaderValue.Parse(Type);}return r;}
    }
}
