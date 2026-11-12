using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Lotto.InfrastructureTests;

public sealed class TypesVisibilityTests
{
    [Fact]
    private void AllTypes_Should_BeInternal()
    {
        var visibleTypes = GetNotGeneratedCode().Where(t => !IsInternal(t) && t.Name != "Program");
        Assert.Empty(visibleTypes);
    }

    [Fact]
    private void AllNonAbstractTypes_Should_BeSealed()
    {
        var nonSealedTypes = GetNotGeneratedCode()
            .Where(t => t is { IsAbstract: false, IsSealed: false } &&t.Name != "Program");
        Assert.Empty(nonSealedTypes);
    }

    private static IEnumerable<Type> GetNotGeneratedCode()
    {
        var assembly = typeof(AssemblyMarker).Assembly;
        return assembly.GetTypes().Where(t => !IsGeneratedCode(t) && t.Name != nameof(AssemblyMarker));
    }

    private static bool IsGeneratedCode(MemberInfo t) =>
        t.IsDefined(typeof(CompilerGeneratedAttribute), false) ||
        t.IsDefined(typeof(GeneratedCodeAttribute), false) ||
        t.IsDefined(typeof(DebuggerNonUserCodeAttribute), false);

    private static bool IsInternal(Type t) => t is
    {
        IsVisible: false,
        IsPublic: false,
        IsNotPublic: true,
        IsNested: false,
        IsNestedPublic: false,
        IsNestedFamily: false,
        IsNestedPrivate: false,
        IsNestedAssembly: false,
        IsNestedFamORAssem: false,
        IsNestedFamANDAssem: false
    };
}
