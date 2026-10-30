using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Worker;

public sealed class WorkerApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _apiKey;

    public WorkerApiClient(HttpClient httpClient, string apiBaseUrl, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiBaseUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        if (apiKey.Length < 32)
        {
            throw new ArgumentException("Worker API key must contain at least 32 characters.", nameof(apiKey));
        }

        _httpClient = httpClient;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        _apiKey = apiKey;
    }

    public Task<HttpResponseMessage?> ConfirmRecordingAsync(
        ConfirmarGravacaoCartaoDto request,
        CancellationToken cancellationToken = default) =>
        PostWithRetryAsync("/api/cartao/confirmar-gravacao", request, cancellationToken);

    public Task<HttpResponseMessage?> ReportRecordingFailureAsync(
        FalharGravacaoCartaoDto request,
        CancellationToken cancellationToken = default) =>
        PostWithRetryAsync("/api/cartao/falhar-gravacao", request, cancellationToken);

    public Task<HttpResponseMessage> VerifyAccessAsync(
        string uid,
        string guid,
        CancellationToken cancellationToken = default) =>
        VerifyAccessAsync(uid, guid, LogAcesso.LocalPadrao, cancellationToken);

    public Task<HttpResponseMessage> VerifyAccessAsync(
        string uid,
        string guid,
        string local,
        CancellationToken cancellationToken = default) =>
        SendAuthorizedAsync(
            "/api/acesso/verificar",
            new { uid, guid, local },
            cancellationToken);

    private async Task<HttpResponseMessage?> PostWithRetryAsync<TRequest>(
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var response = await SendAuthorizedAsync(route, payload, cancellationToken);
                if (response.IsSuccessStatusCode ||
                    ((int)response.StatusCode < 500 &&
                     response.StatusCode != HttpStatusCode.TooManyRequests))
                {
                    return response;
                }

                response.Dispose();
            }
            catch (HttpRequestException) when (attempt < 3)
            {
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < 3)
            {
            }

            if (attempt < 3)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }

        return null;
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync<TRequest>(
        string route,
        TRequest payload,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}{route}")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Worker-Key", _apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return await _httpClient.SendAsync(request, cancellationToken);
    }
}
