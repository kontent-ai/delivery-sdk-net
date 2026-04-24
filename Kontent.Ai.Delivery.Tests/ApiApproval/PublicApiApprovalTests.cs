using System.Reflection;
using System.Text;

namespace Kontent.Ai.Delivery.Tests.ApiApproval;

public class PublicApiApprovalTests
{
    [Fact]
    public Task PublicApi_ShouldNotChangeUnexpectedly()
    {
        var assembly = typeof(Kontent.Ai.Delivery.Configuration.DeliveryClientBuilder).Assembly;
        var publicApi = GetPublicApiSurface(assembly);
        return Verify(publicApi);
    }

    [Fact]
    public Task CachingPublicApi_ShouldNotChangeUnexpectedly()
    {
        var assembly = typeof(Kontent.Ai.Delivery.Caching.MemoryCacheManager).Assembly;
        var publicApi = GetPublicApiSurface(assembly);
        return Verify(publicApi);
    }

    private static string GetPublicApiSurface(Assembly assembly)
    {
        var sb = new StringBuilder();

        var publicTypes = assembly.GetExportedTypes()
            .OrderBy(t => t.Namespace)
            .ThenBy(t => t.Name);

        foreach (var type in publicTypes)
        {
            sb.AppendLine($"// {type.Namespace}");
            sb.AppendLine(GetTypeSignature(type));

            foreach (var member in GetPublicMembers(type))
            {
                sb.AppendLine($"    {member}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string GetTypeSignature(Type type)
    {
        string kind;
        if (type.IsInterface)
        {
            kind = "interface";
        }
        else if (type.IsEnum)
        {
            kind = "enum";
        }
        else if (type.IsValueType)
        {
            kind = "struct";
        }
        else
        {
            kind = "class";
        }

        var modifiers = type.IsSealed && !type.IsValueType ? "sealed " : "";
        var baseTypes = GetBaseTypes(type);

        return $"public {modifiers}{kind} {type.Name}{baseTypes}";
    }

    private static string GetBaseTypes(Type type)
    {
        var bases = new List<string>();

        if (type.BaseType is not null && type.BaseType != typeof(object) && type.BaseType != typeof(ValueType))
            bases.Add(type.BaseType.Name);

        bases.AddRange(type.GetInterfaces()
            .Where(i => !type.BaseType?.GetInterfaces().Contains(i) ?? true)
            .Select(i => i.Name));

        return bases.Count > 0 ? " : " + string.Join(", ", bases) : "";
    }

    private static IEnumerable<string> GetPublicMembers(Type type)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .OrderBy(p => p.Name))
        {
            var getter = prop.GetMethod?.IsPublic == true ? "get; " : "";
            var setter = prop.SetMethod?.IsPublic == true ? "set; " : "";
            var init = prop.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
                .Any(m => m.Name == "IsExternalInit") == true ? "init; " : "";
            yield return $"{prop.PropertyType.Name} {prop.Name} {{ {getter}{setter}{init}}}";
        }

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Concat(type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => !m.IsSpecialName)
            .OrderBy(m => m.Name))
        {
            var parameters = string.Join(", ", method.GetParameters()
                .Select(p => $"{p.ParameterType.Name} {p.Name}"));
            yield return $"{method.ReturnType.Name} {method.Name}({parameters})";
        }
    }
}
