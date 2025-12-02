using HarmonyLib;
using Sanabi.Framework.Patching;

namespace Sanabi.Framework.Game.Patches;

/// <summary>
///     Gives you full adminflags.
/// </summary>
public static class AdminPatch
{
    [PatchEntry(PatchRunLevel.Content)]
    public static void Patch()
    {
        PatchHelpers.PatchMethod(
            "Content.Shared.Administration.AdminData",
            "HasFlag",
            Prefix,
            HarmonyPatchType.Prefix
        );
    }

    private static bool Prefix(ref bool __result, dynamic flag, bool includeDeAdmin = false)
    {
        Console.WriteLine($"Patching AdminFlags.");
        __result = true;
        return false;
    }
}
