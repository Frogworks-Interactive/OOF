using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace SamplePlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public bool OofOnFall { get; set; } = true;
        public bool OofOnDeath { get; set; } = true;
        public float Volume { get; set; } = 0.5f;
        public bool OofInBattle { get; set; } = false;

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
