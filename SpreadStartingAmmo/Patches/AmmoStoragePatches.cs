using HarmonyLib;
using Player;

namespace SpreadStartingAmmo.Patches
{
    [HarmonyPatch]
    internal static class AmmoStoragePatches
    {
        [HarmonyPatch(typeof(InventorySlotAmmo), nameof(InventorySlotAmmo.AddAmmo))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool FixAmmoOverflow(InventorySlotAmmo __instance, float ammoAmount)
        {
            float ammo = __instance.AmmoInPack;
            float maxAmmo = __instance.AmmoMaxCap;

            // If it doesn't exceed capacity, we don't care
            if (ammo + ammoAmount < maxAmmo) return true;

            // If the existing ammo is bigger than max ammo, keeps it, otherwise gets set to max ammo.
            if (ammoAmount > 0 && ammo > 0)
            {
                if (ammo >= maxAmmo)
                    return false;
                else
                    __instance.AmmoInPack = maxAmmo;
            }
            else
                __instance.AmmoInPack += ammoAmount;

            __instance.OnBulletsUpdateCallback?.Invoke(__instance.BulletsInPack);
            return false;
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.AddLevelDefaultAmmoModifications))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Pre_SetStorage(PlayerAmmoStorage __instance, PlayerAgent owner)
        {
            if (RundownManager.ActiveExpedition == null) return true;

            AmmoSpreadManager.PullStartingAmmo(owner, out var ammoMods);

            var idArr = __instance.m_ammoModificationIDs;
            var specialOverride = RundownManager.ActiveExpedition.SpecialOverrideData;

            if (idArr[0] != 0)
                AgentModifierManager.ClearModifierChange(idArr[0]);
            idArr[0] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoStandard, specialOverride.StandardAmmoAtExpeditionStart * ammoMods[0] - 1f);
            if (idArr[1] != 0)
                AgentModifierManager.ClearModifierChange(idArr[1]);
            idArr[1] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoSpecial, specialOverride.SpecialAmmoAtExpeditionStart * ammoMods[1] - 1f);
            if (idArr[2] != 0)
                AgentModifierManager.ClearModifierChange(idArr[2]);
            idArr[2] = AgentModifierManager.AddModifierValue(owner, AgentModifier.InitialAmmoTool, specialOverride.ToolAmmoAtExpeditionStart * ammoMods[2] - 1f);

            return false;
        }
    }
}
