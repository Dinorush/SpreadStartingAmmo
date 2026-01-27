using BepInEx.Unity.IL2CPP;
using GTFuckingXP.Extensions;
using System.Runtime.CompilerServices;

namespace SpreadStartingAmmo.Dependencies
{
    internal static class GtfXPWrapper
    {
        public const string PLUGIN_GUID = "Endskill.GTFuckingXP";
        public static readonly bool HasXP;

        static GtfXPWrapper()
        {
            HasXP = IL2CPPChainloader.Instance.Plugins.ContainsKey(PLUGIN_GUID);
        }

        public static (float standard, float special, float tool) GetAmmoMods()
        {
            if (!HasXP)
                return (1f, 1f, 1f);

            return GetAmmoMods_Internal();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (float standard, float special, float tool) GetAmmoMods_Internal()
        {
            (float standard, float special, float tool) = (1f, 1f, 1f);
            if (!CacheApiWrapper.TryGetCurrentLevelLayout(out var layout) || layout.StartingBuffs == null)
                return (standard, special, tool);

            foreach (var buff in layout.StartingBuffs)
            {
                switch (buff.StartBuff)
                {
                    case GTFuckingXP.Enums.StartBuff.AmmunitionMainMultiplier:
                        standard *= buff.Value;
                        break;
                    case GTFuckingXP.Enums.StartBuff.AmmunitionSpecialMultiplier:
                        special *= buff.Value;
                        break;
                    case GTFuckingXP.Enums.StartBuff.AmmunitionToolMultiplier:
                        tool *= buff.Value;
                        break;
                }
            }
            return (standard, special, tool);
        }
    }
}
