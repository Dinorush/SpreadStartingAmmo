using BepInEx.Configuration;
using BepInEx;
using System.IO;
using GTFO.API.Utilities;

namespace SpreadStartingAmmo
{
    internal static class Configuration
    {
        private readonly static ConfigEntry<int> _basePlayerCount;
        public static int BasePlayerCount => _basePlayerCount.Value;

        private readonly static ConfigFile configFile;

        static Configuration()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg"), saveOnInit: true);
            string section = "Settings";
            _basePlayerCount = configFile.Bind(section, "Base Player Count", 4, "The standard team size used to calculate total ammo.");
        }

        internal static void Init()
        {
            LiveEditListener listener = LiveEdit.CreateListener(Paths.ConfigPath, EntryPoint.MODNAME + ".cfg", false);
            listener.FileChanged += OnFileChanged;
        }

        private static void OnFileChanged(LiveEditEventArgs _)
        {
            configFile.Reload();
        }
    }
}
