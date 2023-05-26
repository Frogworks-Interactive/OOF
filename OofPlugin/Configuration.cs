using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace OofPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool OofOnFall { get; set; } = true;
        public bool OofOnFallMounted { get; set; } = true;
        public bool OofOnFallBattle { get; set; } = true;
        public bool OofOnDeath { get; set; } = true;
        public bool OofOnDeathBattle { get; set; } = true;
        public bool OofOnDeathSelf { get; set; } = true;
        public bool OofOnDeathParty { get; set; } = true;
        public bool OofOnDeathAlliance { get; set; } = true;
        public float Volume { get; set; } = 0.5f;
        public string DefaultSoundImportPath { get; set; } = string.Empty;

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
