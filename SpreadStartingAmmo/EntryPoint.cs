using BepInEx;
using BepInEx.Unity.IL2CPP;
using GTFO.API;
using HarmonyLib;
using SpreadStartingAmmo.Dependencies;
using SpreadStartingAmmo.Patches;

namespace SpreadStartingAmmo
{
    [BepInPlugin("Dinorush." + MODNAME, MODNAME, "1.0.6")]
    [BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(TBFWrapper.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    internal sealed class EntryPoint : BasePlugin
    {
        public const string MODNAME = "SpreadStartingAmmo";

        public override void Load()
        {
            Configuration.Init();
            var harmony = new Harmony(MODNAME);
            harmony.PatchAll();
            if (!TBFWrapper.HasTBF)
                harmony.PatchAll(typeof(TBF_ToolAmmoPatches));
            LevelAPI.OnEnterLevel += AmmoSpreadManager.OnLevelStart;
            LevelAPI.OnLevelCleanup += AmmoSpreadManager.OnLevelCleanup;
            Log.LogMessage("Loaded " + MODNAME);
        }
    }
}