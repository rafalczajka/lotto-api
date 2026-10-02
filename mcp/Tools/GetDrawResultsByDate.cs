using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Lotto.MCP.Tools;

internal sealed class GetDrawResultsByDate(ApiClient apiClient)
{
    private const string ToolName = "get_draw_results_by_date";

    private const string ToolDescription = "Gets Lotto draw results for a specific date.";

    [Function(nameof(GetDrawResultsByDate))]
    public Task<DrawResultsDto> Run(
        [McpToolTrigger(ToolName, ToolDescription)]
        ToolInvocationContext _,
        [McpToolProperty("date", "Draw date in yyyy-MM-dd format.", isRequired: true)]
        DateTimeOffset date,
        CancellationToken cancellationToken)
    {
        return apiClient.GetDrawResultsByDateAsync(date.ToDateOnly(), cancellationToken);
    }
}
