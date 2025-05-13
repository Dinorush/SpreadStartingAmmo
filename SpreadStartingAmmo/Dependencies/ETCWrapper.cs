using BepInEx.Unity.IL2CPP;

namespace SpreadStartingAmmo.Dependencies
{
    internal static class ETCWrapper
    {
        public const string GUID = "Dinorush.ExtraToolCustomization";

        public static bool HasETC { get; private set; }

        static ETCWrapper()
        {
            HasETC = IL2CPPChainloader.Instance.Plugins.ContainsKey(GUID);
        }
    }
}
