using System.Reflection;

namespace BaryoDev.Umbraco.RelatedPosts.Tests;

// Answers named members; every other member returns an empty default.
public class Stub : DispatchProxy
{
    public Dictionary<string, Func<object?[], object?>> On { get; } = new();

    public static T Make<T>(params (string Member, Func<object?[], object?> Answer)[] answers)
        where T : class
    {
        var proxy = Create<T, Stub>();
        foreach (var (member, answer) in answers)
        {
            ((Stub)(object)proxy).On[member] = answer;
        }

        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (On.TryGetValue(targetMethod!.Name, out var answer))
        {
            return answer(args ?? []);
        }

        var type = targetMethod.ReturnType;
        if (type == typeof(void))
        {
            return null;
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition == typeof(IEnumerable<>) || definition == typeof(IReadOnlyList<>)
                || definition == typeof(IReadOnlyCollection<>))
            {
                return Array.CreateInstance(arguments[0], 0);
            }

            if (definition == typeof(IReadOnlyDictionary<,>) || definition == typeof(IDictionary<,>))
            {
                return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments));
            }
        }

        return null;
    }
}
