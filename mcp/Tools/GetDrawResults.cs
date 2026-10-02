
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Lotto.MCP.Tools;

internal sealed class GetDrawResults(ApiClient apiClient)
{
    private const string ToolName = "get_draw_results";

    private const string ToolDescription = """
       Gets Lotto draw results from newest to oldest, optionally filtered
       by date range and limited to a maximum number of results.
       """;

    [Function(nameof(GetDrawResults))]
    public Task<IReadOnlyList<DrawResultsDto>> Run(
        [McpToolTrigger(ToolName, ToolDescription)]
        ToolInvocationContext _,
        [McpToolProperty("dateFrom", "Optional start date in yyyy-MM-dd format.", isRequired: false)]
        DateTimeOffset? dateFrom,
        [McpToolProperty("dateTo", "Optional end date in yyyy-MM-dd format.", isRequired: false)]
        DateTimeOffset? dateTo,
        [McpToolProperty("top", "Optional maximum number of results to return.", isRequired: false)]
        int? top,
        CancellationToken cancellationToken)
    {
        if (top is <= 0) throw new ArgumentException("'top' must be a positive integer.", nameof(top));

        return apiClient.GetDrawResultsAsync(
            dateFrom.ToDateOnly(),
            dateTo.ToDateOnly(),
            top,
            cancellationToken);
    }
}
