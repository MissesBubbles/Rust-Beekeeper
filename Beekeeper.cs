using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Beekeeper", "YourName", "0.1.0")]
    [Description("Configurable beekeeper NPC vendor and bee progression system.")]
    public class Beekeeper : RustPlugin
    {
        private PluginConfig config;

        private class PluginConfig
        {
            public string NpcName = "Beekeeper";
            public bool EnableHoneySelling = true;
            public int HoneyJarsRequiredToSell = 500;
            public string SellRewardItem = "scrap";
            public int SellRewardAmount = 20;
            public int DailySellLimitPerPlayer = 1;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        private void Init()
        {
            config = Config.ReadObject<PluginConfig>();

            if (config == null)
            {
                LoadDefaultConfig();
            }

            Puts("Beekeeper V0.1 loaded.");
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }
    }
}