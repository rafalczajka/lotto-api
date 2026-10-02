using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Lotto.MCP.Tools;

internal sealed class GetLatestDrawResults(ApiClient apiClient)
{
    private const string ToolName = "get_latest_draw_results";

    private const string ToolDescription = "Gets the latest Lotto draw results.";

    [Function(nameof(GetLatestDrawResults))]
    public Task<DrawResultsDto> Run(
        [McpToolTrigger(ToolName, ToolDescription)]
        ToolInvocationContext _,
        CancellationToken cancellationToken)
    {
        return apiClient.GetLatestDrawResultsAsync(cancellationToken);
    }
}
