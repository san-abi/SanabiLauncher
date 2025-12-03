using System.Reflection;
using System.Runtime.Loader;
using HarmonyLib;
using Sanabi.Framework.Misc.Net;
using Sanabi.Framework.Patching;

namespace Sanabi.Framework.Game.Managers;

/// <summary>
///     Manages hiding assemblies from the 999999 different
///         places that list every assembly.
/// </summary>
public static class AssemblyHidingManager
{
    /// <summary>
    ///     Assemblies hidden from view.
    /// </summary>
    private static List<Assembly> _hiddenAssemblies = new();

    public static void Initialise()
    {
        HarmonyManager.Initialise();
    }

    public static void HideBasicAssemblies()
    {
        HideAssembly("Harmony", once: false);
        HideAssembly("Sanabi", once: false);
    }

    /// <summary>
    ///     Hides the assemblies whose <see cref="Assembly.FullName"/>
    ///         matches the given string.
    /// </summary>
    /// <param name="once">Only hide the first matching one?</param>
    public static void HideAssembly(string identifier, bool once = false)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            if (assembly.FullName is not { } fullName ||
                !fullName.Contains(identifier))
                continue;

            HideAssembly(assembly);
        }
    }

    /// <summary>
    ///     Hides an assembly.
    /// </summary>
    public static void HideAssembly(Assembly assembly)
    {
        _hiddenAssemblies.Add(assembly);
    }

    public static void PatchDetectionVectors()
    {
        MethodInfo?[] methods = [
            typeof(AppDomain).GetMethod(nameof(AppDomain.GetAssemblies)),
            typeof(AssemblyLoadContext).GetProperty("Assemblies")?.GetGetMethod(),
            typeof(AssemblyLoadContext).GetProperty("All")?.GetGetMethod(),
            typeof(Assembly).GetMethod(nameof(Assembly.GetTypes)),
            Assembly.GetExecutingAssembly().GetType().GetMethod(nameof(Assembly.GetReferencedAssemblies))
        ];

        var patchMethod = PatchHelpers.GetMethod(typeof(AssemblyHidingManager), "DetectionVectorPatcher");
        foreach (var method in methods)
            PatchHelpers.PatchMethod(
                targetMethod: method,
                patchMethod: patchMethod,
                HarmonyPatchType.Postfix
            );
    }

    private static AssemblyName[] HiddenAssemblyNames()
    {
        var list = new List<AssemblyName>();
        foreach (var hiddenAssembly in _hiddenAssemblies)
            list.Add(hiddenAssembly.GetName());

        return [.. list];
    }

    private static AssemblyName[] HideHiddenAssemblyNames(AssemblyName[] names)
    {
        var hiddenNames = HiddenAssemblyNames();
        return [.. names.Where(assemblyName => !hiddenNames.Contains(assemblyName))];
    }

    private static IEnumerable<Type> HideHiddenTypes(Type[] unhiddenTypes)
    {
        foreach (var type in unhiddenTypes)
        {
            if (_hiddenAssemblies.Contains(type.Assembly))
                continue;

            yield return type;
        }
    }

    private static void DetectionVectorPatcher(ref object __result)
    {
        // i hate ts
        switch (__result)
        {
            case AssemblyName[] assemblyNames:
                __result = HideHiddenAssemblyNames(assemblyNames);
                break;
            case Assembly[] originalAssemblies:
                __result = originalAssemblies.Where(assembly => !_hiddenAssemblies.Contains(assembly)).ToArray();
                break;
            case Type[] types:
                __result = HideHiddenTypes(types).ToArray();
                break;
            case IEnumerable<Assembly> assemblyEnumerable:
                __result = assemblyEnumerable.Where(assembly => !_hiddenAssemblies.Contains(assembly));
                break;
            case IEnumerable<AssemblyLoadContext> assemblyLoadContextEnumerable:
                __result = assemblyLoadContextEnumerable.Where(context => context.Name != "Assembly.Load(byte[], ...)");
                break;
            default:
                throw new InvalidOperationException($"Bad type: {__result.GetType()}");
        }
    }
}
