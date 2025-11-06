using GameData;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SpreadStartingAmmo
{
    internal static class AmmoSpreadManager
    {
        private static readonly HashSet<ulong> _playerLookups = new();

        public static void PullStartingAmmo(PlayerAgent player, out List<float> ammoMods)
        {
            if (SNet.HubPlayerCount == 0)
            {
                DinoLogger.Error($"Tried to pull starting ammo when there were no players in the lobby!");
                ammoMods = new() { 1f, 1f, 1f };
                return;
            }

            float mod = (float)Configuration.BasePlayerCount / (_playerLookups.Count > 0 ? _playerLookups.Count : SNet.HubPlayerCount);
            ammoMods = new() { mod, mod, mod };
            // If this is part of the players on drop, give them the full modifier (or if they already existed, skip them)
            if (_playerLookups.Count == 0 || _playerLookups.Contains(player.Owner.Lookup)) return;

            // NOT one of the players on drop! Need to pull ammo from other players!
            _playerLookups.Add(player.Owner.Lookup);
            mod = (float)Configuration.BasePlayerCount / _playerLookups.Count;
            for (int i = 0; i < ammoMods.Count; i++)
                ammoMods[i] = mod;

            var playerData = PlayerDataBlock.GetBlock(1u);
            var specialOverride = RundownManager.ActiveExpedition.SpecialOverrideData;
            List<float> defaults = new()
            {
                playerData.AmmoStandardInitial * specialOverride.StandardAmmoAtExpeditionStart,
                playerData.AmmoSpecialInitial * specialOverride.SpecialAmmoAtExpeditionStart,
                playerData.AmmoClassInitial * specialOverride.ToolAmmoAtExpeditionStart
            };
            List<float> targets = defaults.ConvertAll(ammo => mod * ammo);
            if (targets.All(x => x == 0)) return; // If players start with no ammo, we don't need to do anything.

            List<(PlayerBackpack backpack, List<float> ammoList)> ammoPerSync = new(); // The ammo each player has (per slot).
            List<int> valid = new() { 0, 0, 0 }; // The number of players that have ammo (per slot).

            foreach (var otherPlayer in PlayerManager.PlayerAgentsInLevel)
            {
                if (otherPlayer.Pointer == player.Pointer || !PlayerBackpackManager.TryGetBackpack(otherPlayer.Owner, out var backpack)) continue;

                var storage = backpack.AmmoStorage;
                if (otherPlayer.IsLocallyOwned || (SNet.IsMaster && otherPlayer.Owner.IsBot))
                    ToggleMagsInReserves(backpack, putInReserves: true);

                List<float> list = new() { storage.StandardAmmo.AmmoInPack, storage.SpecialAmmo.AmmoInPack, storage.ClassAmmo.AmmoInPack };
                if (backpack.TryGetBackpackItem(InventorySlot.GearClass, out var item) && item.Instance != null && item.Instance.ItemDataBlock.BlockToolAmmoRefill) // Biotracker guard
                    list[2] = 0;

                ammoPerSync.Add((backpack, list));
                for (int i = 0; i < list.Count; i++)
                    if (list[i] > 0)
                        valid[i]++;
            }

            // If nobody has the ammo to give, we don't need to run calculations/networking code.
            if (valid.All(x => x == 0))
            {
                for (int i = 0; i < ammoMods.Count; i++)
                    ammoMods[i] = 0;
                return;
            }

            // Equally pull ammo from every player until the target is reached or none remains.
            List<float> targetLeft = new(targets);
            for (int i = 0; i < 3; i++)
            {
                while (valid[i] > 0 && targetLeft[i] > 0)
                {
                    float pull = targetLeft[i] / valid[i];
                    foreach ((_, var ammoList) in ammoPerSync)
                    {
                        if (ammoList[i] <= 0) continue;

                        float pulled = Math.Min(ammoList[i], pull);
                        ammoList[i] -= pulled;
                        targetLeft[i] -= pulled;
                        if (ammoList[i] <= 0)
                            valid[i]--;
                    }
                }
            }

            // Take ammo from the current player if they're not the one being given ammo
            if (!player.IsLocallyOwned)
            {
                var localBackpack = PlayerBackpackManager.LocalBackpack;
                var localList = ammoPerSync.First(x => x.backpack.Pointer == localBackpack.Pointer).ammoList;
                var localStorage = localBackpack.AmmoStorage;
                localStorage.SetAmmo(AmmoType.Standard, localList[0]);
                localStorage.SetAmmo(AmmoType.Special, localList[1]);
                localStorage.SetAmmo(AmmoType.Class, localList[2]);
                ToggleMagsInReserves(localBackpack, putInReserves: false);
            }
            else if (SNet.IsMaster)
            {
                foreach ((var backpack, var ammoList) in ammoPerSync)
                {
                    if (!backpack.Owner.IsBot) continue;
                    var storage = backpack.AmmoStorage;
                    storage.SetAmmo(AmmoType.Standard, ammoList[0]);
                    storage.SetAmmo(AmmoType.Special, ammoList[1]);
                    storage.SetAmmo(AmmoType.Class, ammoList[2]);
                    ToggleMagsInReserves(backpack, putInReserves: false);
                }
            }

            // Convert ammo amounts to modifiers.
            for (int i = 0; i < ammoMods.Count; i++)
                ammoMods[i] = (targets[i] - targetLeft[i]) / defaults[i];
        }

        private static void ToggleMagsInReserves(PlayerBackpack backpack, bool putInReserves)
        {
            PlayerAmmoStorage storage = backpack.AmmoStorage;
            void ToggleForSlot(InventorySlot slot)
            {
                if (!backpack.TryGetBackpackItem(slot, out var bpItem)) return;

                var item = bpItem.Instance.Cast<ItemEquippable>();
                var slotAmmo = storage.GetInventorySlotAmmo(slot);

                if (slotAmmo.BulletClipSize == 0) return;

                if (putInReserves) // Will be toggled back in the same frame, don't need to take it out of the clip
                    slotAmmo.AmmoInPack += item.GetCurrentClip() * slotAmmo.CostOfBullet;
                else
                {
                    int newClip = Math.Min(slotAmmo.BulletsInPack, item.GetCurrentClip());
                    item.SetCurrentClip(newClip);
                    slotAmmo.AmmoInPack -= newClip * slotAmmo.CostOfBullet;
                }
            }

            ToggleForSlot(InventorySlot.GearStandard);
            ToggleForSlot(InventorySlot.GearSpecial);
            ToggleForSlot(InventorySlot.GearClass);
        }

        public static void OnLevelStart()
        {
            foreach (var player in SNet.SessionHub.PlayersInSession)
                _playerLookups.Add(player.Lookup);
        }

        public static void OnLevelCleanup()
        {
            _playerLookups.Clear();
        }
    }
}
