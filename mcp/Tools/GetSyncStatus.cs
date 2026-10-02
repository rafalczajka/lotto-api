using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Mcp;

namespace Lotto.MCP.Tools;

internal sealed class GetSyncStatus(ApiClient apiClient)
{
    private const string ToolName = "get_sync_status";

    private const string ToolDescription = """
        Gets the synchronization status between
        stored Lotto draw results and the source API.
        """;

    [Function(nameof(GetSyncStatus))]
    public Task<SyncDto> Run(
        [McpToolTrigger(ToolName, ToolDescription)]
        ToolInvocationContext _,
        CancellationToken cancellationToken)
    {
        return apiClient.GetSyncAsync(cancellationToken);
    }
}
