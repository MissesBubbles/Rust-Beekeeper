using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Beekeeper", "MissesBubbles", "0.2.0")]
    [Description("A configurable beekeeper NPC and bee progression system.")]
    public class Beekeeper : RustPlugin
    {
        #region Configuration

        private PluginConfig config;

        private class PluginConfig
        {
            public GeneralSettings General = new GeneralSettings();
            public NPCSettings NPC = new NPCSettings();
            public DialogueSettings Dialogue = new DialogueSettings();
            public HoneySellingSettings HoneySelling = new HoneySellingSettings();
        }

        private class GeneralSettings
        {
            public bool PluginEnabled = true;
            public bool DebugMode = false;
        }

        private class NPCSettings
        {
            public string Name = "Beekeeper";
            public float Health = 100f;
            public bool Invincible = true;

            public string PrefabPath = "assets/rust.ai/agents/npcplayer/humannpc/scientist/scientistnpc_full_any.prefab";
        }

        private class DialogueSettings
        {
            public List<string> Greetings = new List<string>
            {
                "The bees have been busy today.",
                "Welcome, friend. Mind the hives.",
                "The queen watches over every colony."
            };

            public List<string> Farewells = new List<string>
            {
                "May your hives flourish.",
                "Travel safely.",
                "Mind the stingers."
            };

            public List<string> PurchaseMessages = new List<string>
            {
                "Spend wisely.",
                "Good choice.",
                "Every beekeeper starts somewhere."
            };

            public List<string> NoScrapMessages = new List<string>
            {
                "You'll need more scrap.",
                "Quality costs scrap.",
                "My bees don't work for free."
            };

            public List<string> HoneySoldMessages = new List<string>
            {
                "Excellent harvest.",
                "Sweet work.",
                "Your bees are treating you well."
            };

            public List<string> NoHoneyMessages = new List<string>
            {
                "You have no honey to sell.",
                "Come back after a harvest.",
                "No jars... no trade."
            };

            public List<string> RareItemMessages = new List<string>
            {
                "Handle that nucleus carefully.",
                "Now THAT is a rare find.",
                "The queen smiles upon you."
            };

            public List<string> ApiaryTips = new List<string>
            {
                "Healthy bees make healthy hives.",
                "Wildflowers help every colony.",
                "Neglected hives won't survive forever."
            };

            public List<string> IdleMessages = new List<string>
            {
                "The bees never truly sleep...",
                "Buzz... buzz...",
                "A quiet hive is rarely a healthy hive."
            };
        }

        private class HoneySellingSettings
        {
            public bool Enabled = true;
            public int HoneyJarsRequired = 500;
            public string RewardItem = "scrap";
            public int RewardAmount = 20;
            public int DailySellLimitPerPlayer = 1;
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void LoadConfigData()
        {
            config = Config.ReadObject<PluginConfig>();

            if (config == null)
            {
                PrintWarning("Config was empty or invalid. Creating a new default config.");
                LoadDefaultConfig();
            }
        }

        #endregion

        #region Data

        private StoredData storedData;
        private BasePlayer beekeeperNpc;

        private class StoredData
        {
            public bool HasBeekeeper = false;
            public Vector3Data Position = new Vector3Data();
            public Vector3Data Rotation = new Vector3Data();
        }

        private class Vector3Data
        {
            public float X;
            public float Y;
            public float Z;

            public Vector3Data() { }

            public Vector3Data(Vector3 vector)
            {
                X = vector.x;
                Y = vector.y;
                Z = vector.z;
            }

            public Vector3 ToVector3()
            {
                return new Vector3(X, Y, Z);
            }
        }

        private void LoadData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);

            if (storedData == null)
            {
                storedData = new StoredData();
                SaveData();
            }
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, storedData);
        }

        #endregion

        #region Permissions

        private const string PermissionAdmin = "beekeeper.admin";
        private const string PermissionUse = "beekeeper.use";

        private void RegisterPermissions()
        {
            permission.RegisterPermission(PermissionAdmin, this);
            permission.RegisterPermission(PermissionUse, this);
        }

        #endregion

        #region Commands

        [ChatCommand("beekeeper")]
        private void BeekeeperCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionAdmin))
            {
                player.ChatMessage("<color=#ff5555>You do not have permission to use Beekeeper admin commands.</color>");
                return;
            }

            if (args == null || args.Length == 0)
            {
                SendHelp(player);
                return;
            }

            string subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "help":
                    SendHelp(player);
                    break;

                case "spawn":
                    SpawnBeekeeper(player);
                    break;

                case "remove":
                    RemoveBeekeeper(player);
                    break;

                case "move":
                    MoveBeekeeper(player);
                    break;

                case "reload":
                    ReloadPlugin(player);
                    break;

                default:
                    player.ChatMessage("<color=#ff5555>Unknown beekeeper command. Use /beekeeper help</color>");
                    break;
            }
        }

        private void SendHelp(BasePlayer player)
        {
            player.ChatMessage("<color=#FFD700>══════════════════════════════════════</color>");
            player.ChatMessage("<size=18><color=#FFD700>🐝 Beekeeper v0.2.0</color></size>");
            player.ChatMessage("<color=#C0C0C0>Created by MissesBubbles</color>");
            player.ChatMessage(" ");
            player.ChatMessage("<color=#55FF55>Admin Commands</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper help</color> <color=#FFFFFF>- Shows this menu</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper spawn</color> <color=#FFFFFF>- Save the Beekeeper NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper move</color> <color=#FFFFFF>- Move the existing NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper remove</color> <color=#FFFFFF>- Remove the NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper reload</color> <color=#FFFFFF>- Reload config and data</color>");
            player.ChatMessage("<color=#FFD700>══════════════════════════════════════</color>");
        }

        #endregion

        #region Beekeeper Management

        private void SpawnBeekeeper(BasePlayer player)
        {
            if (storedData.HasBeekeeper)
            {
                player.ChatMessage("<color=#ffaa00>A Beekeeper already exists. Use /beekeeper move to relocate him.</color>");
                return;
            }

            storedData.HasBeekeeper = true;
            storedData.Position = new Vector3Data(player.transform.position);
            storedData.Rotation = new Vector3Data(player.transform.rotation.eulerAngles);

            SaveData();

            SpawnNPC(storedData.Position.ToVector3(), storedData.Rotation.ToVector3());

            player.ChatMessage("<color=#55ff55>Beekeeper location saved.</color>");
            player.ChatMessage("<color=#ffd479>NPC spawn method has been called.</color>");
        }

        private void RemoveBeekeeper(BasePlayer player)
        {
            if (!storedData.HasBeekeeper)
            {
                player.ChatMessage("<color=#ffaa00>No Beekeeper has been placed.</color>");
                return;
            }

            if (beekeeperNpc != null && !beekeeperNpc.IsDestroyed)
            {
                beekeeperNpc.Kill();
                beekeeperNpc = null;
            }

            storedData = new StoredData();

            SaveData();

            player.ChatMessage("<color=#55ff55>Beekeeper removed successfully.</color>");
        }

        private void MoveBeekeeper(BasePlayer player)
        {
            if (!storedData.HasBeekeeper)
            {
                player.ChatMessage("<color=#ffaa00>No Beekeeper exists yet. Use /beekeeper spawn first.</color>");
                return;
            }

            if (beekeeperNpc != null && !beekeeperNpc.IsDestroyed)
            {
                beekeeperNpc.Kill();
                beekeeperNpc = null;
            }

            storedData.Position = new Vector3Data(player.transform.position);
            storedData.Rotation = new Vector3Data(player.transform.rotation.eulerAngles);

            SaveData();

            SpawnNPC(storedData.Position.ToVector3(), storedData.Rotation.ToVector3());

            player.ChatMessage("<color=#55ff55>Beekeeper moved successfully.</color>");
        }

        private void ReloadPlugin(BasePlayer player)
        {
            LoadConfigData();
            LoadData();

            if (beekeeperNpc != null && !beekeeperNpc.IsDestroyed)
            {
                beekeeperNpc.Kill();
                beekeeperNpc = null;
            }

            if (storedData.HasBeekeeper)
            {
                SpawnNPC(storedData.Position.ToVector3(), storedData.Rotation.ToVector3());
            }

            player.ChatMessage("<color=#55ff55>Beekeeper configuration, data, and NPC reloaded.</color>");
        }

        #endregion

        #region NPC

        private void SpawnNPC(Vector3 position, Vector3 rotation)
        {
            Puts("SpawnNPC() called.");

            if (beekeeperNpc != null && !beekeeperNpc.IsDestroyed)
            {
                beekeeperNpc.Kill();
                beekeeperNpc = null;
            }

            beekeeperNpc = GameManager.server.CreateEntity(
                config.NPC.PrefabPath,
                position,
                Quaternion.Euler(rotation)
            ) as BasePlayer;

            if (beekeeperNpc == null)
            {
                PrintError("Failed to create Beekeeper NPC.");
                return;
            }

            beekeeperNpc.displayName = config.NPC.Name;
            beekeeperNpc.health = config.NPC.Health;

            beekeeperNpc.Spawn();

            Puts($"Spawned '{config.NPC.Name}'");
            Puts($"Position: {position}");
            Puts($"Rotation: {rotation}");
        }

        #endregion

        #region Dialogue

        private string GetRandomGreeting()
        {
            if (config.Dialogue.Greetings == null || config.Dialogue.Greetings.Count == 0)
                return "Hello.";

            int index = UnityEngine.Random.Range(0, config.Dialogue.Greetings.Count);

            return config.Dialogue.Greetings[index];
        }

        private void GreetPlayer(BasePlayer player)
        {
            player.ChatMessage($"<color=#FFD700>{config.NPC.Name}</color>: {GetRandomGreeting()}");
        }

        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfigData();
            LoadData();
            RegisterPermissions();

            if (storedData.HasBeekeeper)
            {
                SpawnNPC(storedData.Position.ToVector3(), storedData.Rotation.ToVector3());
            }

            Puts("Beekeeper v0.2.0 loaded.");
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion
    }
}