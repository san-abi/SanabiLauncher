using Splat;
using SS14.Launcher.Models.Data;
using SS14.Launcher.Utility;
using SS14.Common.Data.CVars;
using System.Diagnostics;
using Sanabi.Framework.Data;
using ReactiveUI;
using System;

namespace SS14.Launcher.ViewModels.MainWindowTabs;

public class PatchesTabViewModel : MainWindowTabViewModel
{
    public DataManager Cfg { get; }

    public PatchesTabViewModel()
    {
        Cfg = Locator.Current.GetRequiredService<DataManager>();
    }

    private void SetAndCommitCvar<T>(CVarDef<T> cVarDef, T newValue)
    {
        Cfg.SetCVar(cVarDef, newValue);
        Cfg.CommitConfig();
    }

    public static void OpenModDirectory()
    {
        Process.Start(new ProcessStartInfo
        {
            UseShellExecute = true,
            FileName = LauncherPaths.SanabiModsPath
        });
    }

    /// <summary>
    ///     Regenerates <see cref="SanabiCVars.SpoofedHwidSeed"/>
    ///         to something random.
    /// </summary>
    public void RegenerateHwidSeed()
    {
        var bytes = (Span<byte>)stackalloc byte[8];
        new Random().NextBytes(bytes);

        // setting cvar is redundant here
        SpoofedHwidSeedText = BitConverter.ToUInt64(bytes).ToString();
    }

    public override string Name => "Patches";

    public bool PatchingEnabled
    {
        get => Cfg.GetCVar(SanabiCVars.PatchingEnabled);
        set => SetAndCommitCvar(SanabiCVars.PatchingEnabled, value);
    }

    public bool PatchingLevel
    {
        get => Cfg.GetCVar(SanabiCVars.PatchingLevel);
        set => SetAndCommitCvar(SanabiCVars.PatchingLevel, value);
    }

    public bool HwidPatchEnabled
    {
        get => Cfg.GetCVar(SanabiCVars.HwidPatchEnabled);
        set => SetAndCommitCvar(SanabiCVars.HwidPatchEnabled, value);
    }

    public string SpoofedHwidSeedText
    {
        get => BitConverter.ToUInt64(BitConverter.GetBytes(Cfg.GetActiveAccountCVarOrDefault(SanabiAccountCVars.SpoofedHwidSeed)), 0).ToString();
        set
        {
            Console.WriteLine($"Parsing {value}");
            if (ulong.TryParse(value, out var ulongValue) &&
                Cfg.TrySetActiveAccountCVar(SanabiAccountCVars.SpoofedHwidSeed, BitConverter.ToInt64(BitConverter.GetBytes(ulongValue), 0)))
            {
                Cfg.CommitConfig();
            }

            this.RaisePropertyChanged(propertyName: nameof(SpoofedHwidSeedText));
        }
    }

    public bool LoadInternalMods
    {
        get => Cfg.GetCVar(SanabiCVars.LoadInternalMods);
        set => SetAndCommitCvar(SanabiCVars.LoadInternalMods, value);
    }

    public bool LoadExternalMods
    {
        get => Cfg.GetCVar(SanabiCVars.LoadExternalMods);
        set => SetAndCommitCvar(SanabiCVars.LoadExternalMods, value);
    }
}
