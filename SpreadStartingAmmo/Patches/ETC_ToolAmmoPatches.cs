using HarmonyLib;
using Player;
using System;

namespace SpreadStartingAmmo.Patches
{
    internal static class ETC_ToolAmmoPatches
    {
        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.UpdateBulletsInPack))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Unclamp_UpdateBullets(PlayerAmmoStorage __instance, AmmoType ammoType, int bulletCount, ref float __result)
        {
            // Only care about tools
            if (ammoType != AmmoType.Class) return true;

            InventorySlotAmmo inventorySlotAmmo = __instance.GetInventorySlotAmmo(ammoType);
            float ammo = inventorySlotAmmo.AmmoInPack;
            float maxAmmo = inventorySlotAmmo.AmmoMaxCap;
            float newAmmo = bulletCount * inventorySlotAmmo.CostOfBullet;

            // If it doesn't exceed capacity, we don't care
            if (ammo + newAmmo < maxAmmo) return true;

            float result;
            // Prevent gains from going above max cap or current ammo.
            // Otherwise you could place mines, get tool, and pick them up to overflow.
            if (newAmmo > 0 && ammo > 0)
            {
                if (ammo >= maxAmmo || newAmmo >= maxAmmo)
                    result = inventorySlotAmmo.AmmoInPack = Math.Max(ammo, newAmmo);
                else
                    result = inventorySlotAmmo.AmmoInPack = maxAmmo;
            }
            else
                result = inventorySlotAmmo.AmmoInPack += newAmmo;

            inventorySlotAmmo.OnBulletsUpdateCallback?.Invoke(inventorySlotAmmo.BulletsInPack);
            __instance.NeedsSync = true;
            __instance.UpdateSlotAmmoUI(inventorySlotAmmo);
            __result = result;
            return false;
        }

        [HarmonyPatch(typeof(PlayerAmmoStorage), nameof(PlayerAmmoStorage.UpdateAmmoInPack))]
        [HarmonyWrapSafe]
        [HarmonyPrefix]
        private static bool Unclamp_UpdateAmmo(PlayerAmmoStorage __instance, AmmoType ammoType, float delta, ref float __result)
        {
            // Only care about tools
            if (ammoType != AmmoType.Class) return true;

            InventorySlotAmmo inventorySlotAmmo = __instance.GetInventorySlotAmmo(ammoType);
            float ammo = inventorySlotAmmo.AmmoInPack;
            float maxAmmo = inventorySlotAmmo.AmmoMaxCap;
            float newAmmo = delta;

            // If it doesn't exceed capacity, we don't care
            if (ammo + delta < maxAmmo) return true;

            float result;
            // Sentry could have ammo even while down due to GtfXP adding ammo,
            // so still need to prevent overflow
            if (newAmmo > 0 && ammo > 0)
            {
                if (ammo >= maxAmmo || newAmmo >= maxAmmo)
                    result = inventorySlotAmmo.AmmoInPack = Math.Max(ammo, newAmmo);
                else
                    result = inventorySlotAmmo.AmmoInPack = maxAmmo;
            }
            else
                result = inventorySlotAmmo.AmmoInPack += newAmmo;

            inventorySlotAmmo.OnBulletsUpdateCallback?.Invoke(inventorySlotAmmo.BulletsInPack);
            __instance.NeedsSync = true;
            __instance.UpdateSlotAmmoUI(inventorySlotAmmo);
            __result = result;
            return false;
        }
    }
}
