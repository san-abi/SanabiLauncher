using Sanabi.Framework.Game.Managers;

namespace Sanabi.Framework;

/// <summary>
///     Manager of all managers.
/// </summary>
public static class SanabiEntry
{
    /// <summary>
    ///     Initialise <see cref="AssemblyManager"/>, <see cref="HarmonyManager"/>,
    ///         etc..
    /// </summary>
    public static void Initialise()
    {
        //AssemblyManager.Subscribe();
        HarmonyManager.Initialise();
    }
}
