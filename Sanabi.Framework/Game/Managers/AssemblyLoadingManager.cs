using System.Reflection;
using HarmonyLib;
using Sanabi.Framework.Data;
using Sanabi.Framework.Game.Patches;
using Sanabi.Framework.Misc;
using Sanabi.Framework.Patching;
using SS14.Launcher;

namespace Sanabi.Framework.Game.Managers;

/// <summary>
///     Handles loading mods from the mods directory,
///         into the game.
/// </summary>
public static class AssemblyLoadingManager
{
    private static readonly Stack<Assembly> _assembliesPendingLoad = new();
    private static MethodInfo _modInitMethod = default!;

    [PatchEntry(PatchRunLevel.Engine)]
    private static void Start()
    {
        if (!SanabiConfig.ProcessConfig.LoadExternalMods)
            return;

        var baseModLoader = ReflectionManager.GetTypeByQualifiedName("Robust.Shared.ContentPack.BaseModLoader");
        _modInitMethod = PatchHelpers.GetMethod(baseModLoader, "InitMod")!;

        var internalModLoader = ReflectionManager.GetTypeByQualifiedName("Robust.Shared.ContentPack.ModLoader");

        PatchHelpers.PatchMethod(
            internalModLoader,
            "TryLoadModules",
            ModLoaderPostfix,
            HarmonyPatchType.Postfix
        );

        var externalDlls = Directory.GetFiles(LauncherPaths.SanabiModsPath, "*.dll", SearchOption.TopDirectoryOnly);
        if (externalDlls.Length == 0)
            return;

        foreach (var dll in externalDlls)
            _assembliesPendingLoad.Push(Assembly.LoadFrom(dll));
    }

    private static void ModLoaderPostfix(ref dynamic __instance)
    {
        while (_assembliesPendingLoad.TryPop(out var assembly))
            LoadModAssembly(ref __instance, assembly);
    }

    /// <summary>
    ///     Tries to get the entry point for a mod assembly.
    ///         This is only done so that we are compatible with Marsey
    ///         patches; ideally we use <see cref="PatchEntryAttribute"/>.
    /// </summary>
    /// <param name="assembly"></param>
    public static MethodInfo? GetModAssemblyEntryPoint(Assembly assembly)
    {
        var entryPointType = assembly.GetType("MarseyEntry");
        return entryPointType?.GetMethod("Entry", BindingFlags.Static | BindingFlags.Public);
    }

    private delegate void Forward(AssemblyName asm, string message);
    private static void LogDelegate(AssemblyName asm, string message)
    {
        SanabiLogger.LogInfo($"PRT-{asm.FullName}: {message}");
    }

    /// <summary>
    ///     Ports MarseyLogger to work with a mod assembly patch;
    ///         i.e. makes it print here.
    /// </summary>
    /// <param name="assembly">The mod assembly.</param>
    public static void PortModMarseyLogger(Assembly assembly)
    {
        if (assembly.GetType("MarseyLogger") is not { } loggerType ||
            assembly.GetType("MarseyLogger+Forward") is not { } delegateType)
            return;

        var marseyLogDelegate = Delegate.CreateDelegate(delegateType, PatchHelpers.GetMethod(LogDelegate));

        var loggerForwardDelegateType = loggerType.GetField("logDelegate");
        loggerForwardDelegateType?.SetValue(null, marseyLogDelegate);
    }

    private static void LoadModAssembly(ref dynamic modLoader, Assembly modAssembly)
    {
        AssemblyHidingManager.HideAssembly(modAssembly);
        _modInitMethod.Invoke(modLoader, (Assembly[])[modAssembly]);

        var modEntry = GetModAssemblyEntryPoint(modAssembly);
        if (modEntry != null)
        {
            PortModMarseyLogger(modAssembly);
            _ = Task.Run(() => modEntry.Invoke(null, []));
        }
    }
}
