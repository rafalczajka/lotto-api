namespace Lotto.InfrastructureTests;

public sealed class FeatureTests
{
    private const string FeaturesNamespace = "Lotto.Features";

    [Fact]
    private void FeatureTypes_ShouldNot_BePlacedInTheRootFeatureNamespace()
    {
        var featureTypes = GetFeatureTypes();
        var rootFeatreNamespaceDepth = FeaturesNamespace.Split(".").Length;
        Assert.All(featureTypes, t => Assert.True(t.Namespace!.Split(".").Length > rootFeatreNamespaceDepth));
    }

    private static IEnumerable<Type> GetFeatureTypes()
    {
        var assembly = typeof(AssemblyMarker).Assembly;
        return assembly.DefinedTypes.Where(t =>
            t.Namespace is not null && t.Namespace.StartsWith($"{FeaturesNamespace}."));
    }

    private static IEnumerable<IGrouping<string, Type>> GetFeatureTypesGroupedByFeatureName() =>
        GetFeatureTypes().GroupBy(t => t.Namespace!.Split(".")[2]);
}
