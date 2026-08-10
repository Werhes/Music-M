using System.Net.Http.Json;
using MusicX.Core.Models;
using MusicX.Shared;
using Newtonsoft.Json;
using NLog;

namespace MusicX.Core.Services;

public class BackendConnectionService
{
    private readonly Logger _logger;
    private readonly string _appVersion;
    private string? _token;
    private long _userId;
    public readonly HttpClient Client = new();

    public string Host { get; set; } = "http://31.76.43.138:5000";

    public BackendConnectionService(Logger logger, string appVersion)
    {
        _logger = logger;
        _appVersion = appVersion;
    }

    public async void ReportMetric(string eventName, string? source = null)
    {
        try
        {
            // in case initial token request failed
            _token ??= await GetTokenAsync(_userId);
            
            using var response = await Client.PostAsJsonAsync($"/metrics/{eventName}/report", new ReportMetricRequest(_appVersion, source));
            response.EnsureSuccessStatusCode();
        }
        catch (Exception e)
        {
            _logger.Error(e, "Error while reporting event metric {0}", eventName);
        }
    }

    public async Task<string> GetTokenAsync(long userId)
    {
        _userId = userId;
        
        if (_token is not null) return _token;
        _logger.Info("Получение временного токена Слушать вместе");

        if (Client.BaseAddress is null)
        {
            var host = await GetHostAsync();
            Client.BaseAddress = new(host);
        }
        
        using var response = await Client.PostAsJsonAsync("/token", userId);
        response.EnsureSuccessStatusCode();

        _token = await response.Content.ReadFromJsonAsync<string>() ??
                       throw new NullReferenceException("Got null response for token request");
        
        Client.DefaultRequestHeaders.Authorization = new("Bearer", _token);
        
        return _token;
    }

    private async Task<string> GetHostAsync()
    {
        if (!string.IsNullOrEmpty(Host))
            return Host;

        try
        {
            _logger.Info("Получение адресса сервера Послушать вместе");
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync("https://fooxboy.blob.core.windows.net/musicx/ListenTogetherServers.json");

                var contents = await response.Content.ReadAsStringAsync();

                var servers = JsonConvert.DeserializeObject<ListenTogetherServersModel>(contents);

                return Host =
#if DEBUG
                    servers.Test;
#else
                    servers.Production;
#endif
            }
        }
        catch(Exception)
        {
            return "http://212.192.40.71:5000";
        }
            
    }
}