using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lotto.Features.Http.GetSyncStatus;

internal sealed class FeatureInstaller : IFeatureInstaller
{
    public void Install(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddScoped<FunctionHandler>();
    }
}
