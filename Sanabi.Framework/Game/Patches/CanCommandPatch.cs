using System.Reflection.Emit;
using HarmonyLib;
using Sanabi.Framework.Game.Managers;
using Sanabi.Framework.Patching;

namespace Sanabi.Framework.Game.Patches;

/// <summary>
///     Lets you use absolutely any command.
/// </summary>
public static class CanCommandPatch
{
    [PatchEntry(PatchRunLevel.Content)]
    public static void Patch()
    {

        if (!ReflectionManager.TryGetTypeByQualifiedName("Robust.Client.Console.ClientConsoleHost", out var clientConHostType))
            throw new InvalidOperationException("Couldn't resolve ClientConsoleHost!");

        PatchHelpers.PatchMethod(clientConHostType, "CanExecute", Prefix, HarmonyPatchType.Prefix);
        //PatchHelpers.PatchMethod(clientConHostType, "ExecuteCommand", Transpiler, HarmonyPatchType.Transpiler);
        // I've tried prefix'ing `HasFlag` but it doesn't work afaict. I guess it got inlined.

        if (!ReflectionManager.TryGetTypeByQualifiedName("Content.Shared.Administration.AdminData", out var adminDataType))
            throw new InvalidOperationException("Couldn't resolve AdminData!");

        PatchHelpers.PatchMethod(adminDataType, "HasFlag", Prefix, HarmonyPatchType.Prefix);

        // PatchHelpers.PatchMethod(clientAdminManType, "IsActive", Prefix, HarmonyPatchType.Prefix);
        // PatchHelpers.PatchMethod(clientAdminManType, "CanCommand", Prefix, HarmonyPatchType.Prefix);
        // PatchHelpers.PatchMethod(clientAdminManType, "CanViewVar", Prefix, HarmonyPatchType.Prefix);
        // PatchHelpers.PatchMethod(clientAdminManType, "CanAdminPlace", Prefix, HarmonyPatchType.Prefix);
        // PatchHelpers.PatchMethod(clientAdminManType, "CanScript", Prefix, HarmonyPatchType.Prefix);
        // PatchHelpers.PatchMethod(clientAdminManType, "CanAdminMenu", Prefix, HarmonyPatchType.Prefix);

        if (!ReflectionManager.TryGetTypeByQualifiedName("Robust.Client.Console.ClientConGroupController", out var clientConGroupControllerType))
            throw new InvalidOperationException("Couldn't resolve ClientConGroupController!");

        PatchHelpers.PatchMethod(clientConGroupControllerType, "CanCommand", Prefix, HarmonyPatchType.Prefix);
        PatchHelpers.PatchMethod(clientConGroupControllerType, "CanViewVar", Prefix, HarmonyPatchType.Prefix);
        PatchHelpers.PatchMethod(clientConGroupControllerType, "CanAdminPlace", Prefix, HarmonyPatchType.Prefix);
        PatchHelpers.PatchMethod(clientConGroupControllerType, "CanScript", Prefix, HarmonyPatchType.Prefix);
        PatchHelpers.PatchMethod(clientConGroupControllerType, "CanAdminMenu", Prefix, HarmonyPatchType.Prefix);
    }

    private static bool Prefix(ref bool __result)
    {
        Console.WriteLine($"Patching value to true");
        __result = true;
        return false;
    }

    private static void Postfix(ref bool __result)
    {
        Console.WriteLine($"Patching value to true but not returning");
        __result = true;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var il = new List<CodeInstruction>(instructions);

        // 1. Locate all RET instructions
        var retIndices = new List<int>();
        for (int i = 0; i < il.Count; i++)
        {
            if (il[i].opcode == OpCodes.Ret)
                retIndices.Add(i);
        }

        // 2. Ensure there is a second RET
        if (retIndices.Count >= 2)
        {
            int idx = retIndices[1];

            // Create a label at the next instruction
            Label continueLabel = il[idx + 1].labels.Count == 0
                ? il[idx + 1].labels[0]
                : new Label();

            if (!il[idx + 1].labels.Contains(continueLabel))
                il[idx + 1].labels.Add(continueLabel);

            // 3. Replace RET -> BR continueLabel
            il[idx].opcode = OpCodes.Br;
            il[idx].operand = continueLabel;
        }

        return il;
    }
}
