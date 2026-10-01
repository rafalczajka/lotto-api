using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Lotto.MCP;

internal sealed class ApiClient(HttpClient httpClient)
{
    private const string DateFormat = "yyyy-MM-dd";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<DrawResultsDto>> GetDrawResultsAsync(
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/draw-results{BuildQueryString(dateFrom, dateTo, top)}";
        return await GetAsync<DrawResultsDto[]>(requestUri, cancellationToken) ?? [];
    }

    public Task<DrawResultsDto> GetDrawResultsByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var requestUri = $"api/draw-results/{date.ToString(DateFormat)}";
        return GetRequiredAsync<DrawResultsDto>(requestUri, cancellationToken);
    }

    public Task<DrawResultsDto> GetLatestDrawResultsAsync(CancellationToken cancellationToken = default)
    {
        return GetRequiredAsync<DrawResultsDto>("api/draw-results/latest", cancellationToken);
    }

    public async Task<SyncDto> GetSyncAsync(CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<SyncDto>("api/sync", cancellationToken);
    }

    private async Task<T?> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return default;

        response.EnsureSuccessStatusCode();

        return await Deserialize<T>(response, cancellationToken);
    }

    private async Task<T> GetRequiredAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(requestUri, cancellationToken);

        response.EnsureSuccessStatusCode();

        return await Deserialize<T>(response, cancellationToken) ??
               throw new InvalidOperationException("Failed to retrieve required information.");
    }

    private static string BuildQueryString(DateOnly? dateFrom = null, DateOnly? dateTo = null, int? top = null)
    {
        var query = new List<string>();

        if (dateFrom is not null)
            query.Add($"dateFrom={dateFrom.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}");

        if (dateTo is not null)
            query.Add($"dateTo={dateTo.Value.ToString(DateFormat, CultureInfo.InvariantCulture)}");

        if (top is not null)
            query.Add($"top={top.Value}");

        return query.Count > 0
            ? $"?{string.Join("&", query)}"
            : "";
    }

    private static async Task<T?> Deserialize<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
    }
}

internal sealed record DrawResultsDto(
    string DrawDate,
    IReadOnlyList<int> LottoNumbers,
    IReadOnlyList<int> PlusNumbers);

internal sealed record SyncDto(
    string? LatestSyncDate,
    string LatestDrawDate,
    bool IsUpToDate);
