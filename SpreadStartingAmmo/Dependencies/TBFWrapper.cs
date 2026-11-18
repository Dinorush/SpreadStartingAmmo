using BepInEx.Unity.IL2CPP;

namespace SpreadStartingAmmo.Dependencies
{
    internal static class TBFWrapper
    {
        public const string GUID = "Dinorush.ToolBugFix";

        public static bool HasTBF { get; private set; }

        static TBFWrapper()
        {
            HasTBF = IL2CPPChainloader.Instance.Plugins.ContainsKey(GUID);
        }
    }
}
