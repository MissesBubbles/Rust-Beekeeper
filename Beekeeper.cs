using System.Collections.Generic;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
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
            public string PrefabPath = "assets/prefabs/npc/bandit/shopkeepers/bandit_shopkeeper.prefab";
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
        private readonly Dictionary<ulong, float> interactionCooldowns = new Dictionary<ulong, float>();

        private const string BeekeeperUiName = "BeekeeperUI";

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

                case "greet":
                    OpenBeekeeperUI(player);
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

        [ConsoleCommand("beekeeper.close")]
        private void CloseBeekeeperCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null)
                return;

            CloseBeekeeperUI(player);
        }

        private void SendHelp(BasePlayer player)
        {
            player.ChatMessage("<color=#FFD700>══════════════════════════════════════</color>");
            player.ChatMessage("<size=18><color=#FFD700>Beekeeper v0.2.0</color></size>");
            player.ChatMessage("<color=#C0C0C0>Created by MissesBubbles</color>");
            player.ChatMessage(" ");
            player.ChatMessage("<color=#55FF55>Admin Commands</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper help</color> <color=#FFFFFF>- Shows this menu</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper spawn</color> <color=#FFFFFF>- Save the Beekeeper NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper move</color> <color=#FFFFFF>- Move the existing NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper remove</color> <color=#FFFFFF>- Remove the NPC location</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper reload</color> <color=#FFFFFF>- Reload config and data</color>");
            player.ChatMessage("<color=#FFD700>/beekeeper greet</color> <color=#FFFFFF>- Test Beekeeper dialogue UI</color>");
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

            Vector3 position = player.transform.position;
            Vector3 rotation = player.transform.rotation.eulerAngles;

            bool spawned = SpawnNPC(position, rotation);

            if (!spawned)
            {
                player.ChatMessage("<color=#ff5555>Failed to spawn Beekeeper NPC. Check the server console.</color>");
                return;
            }

            storedData.HasBeekeeper = true;
            storedData.Position = new Vector3Data(position);
            storedData.Rotation = new Vector3Data(rotation);

            SaveData();

            player.ChatMessage("<color=#55ff55>Beekeeper spawned and location saved.</color>");
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

        private bool SpawnNPC(Vector3 position, Vector3 rotation)
        {
            Puts("SpawnNPC() called.");

            if (beekeeperNpc != null && !beekeeperNpc.IsDestroyed)
            {
                beekeeperNpc.Kill();
                beekeeperNpc = null;
            }

            BaseEntity entity = GameManager.server.CreateEntity(
                config.NPC.PrefabPath,
                position,
                Quaternion.Euler(rotation)
            );

            if (entity == null)
            {
                PrintError($"Failed to create NPC using prefab: {config.NPC.PrefabPath}");
                return false;
            }

            beekeeperNpc = entity as BasePlayer;

            if (beekeeperNpc == null)
            {
                PrintError($"Entity was created but is not a BasePlayer. Type: {entity.GetType().Name}");
                entity.Kill();
                return false;
            }

            beekeeperNpc.displayName = config.NPC.Name;
            entity.enableSaving = false;
            entity.Spawn();
            beekeeperNpc = entity as BasePlayer;
            beekeeperNpc.CancelInvoke();
            beekeeperNpc.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, false);
            beekeeperNpc.transform.position = position;
            beekeeperNpc.transform.rotation = Quaternion.Euler(rotation);

            Puts($"Spawned '{config.NPC.Name}' at {position}");
            return true;
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

        #region UI

        private void OpenBeekeeperUI(BasePlayer player)
        {
            CloseBeekeeperUI(player);

            CuiElementContainer container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                Image =
                {
                    Color = "0.08 0.06 0.03 0.92"
                },
                RectTransform =
                {
                    AnchorMin = "0.32 0.30",
                    AnchorMax = "0.68 0.68"
                },
                CursorEnabled = true
            }, "Overlay", BeekeeperUiName);

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = config.NPC.Name,
                    FontSize = 24,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1.0 0.82 0.25 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.05 0.82",
                    AnchorMax = "0.95 0.96"
                }
            }, BeekeeperUiName);

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = GetRandomGreeting(),
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.10 0.45",
                    AnchorMax = "0.90 0.76"
                }
            }, BeekeeperUiName);

            container.Add(new CuiButton
            {
                Button =
                {
                    Color = "0.55 0.20 0.10 1",
                    Command = "beekeeper.close"
                },
                Text =
                {
                    Text = "Leave",
                    FontSize = 16,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.35 0.12",
                    AnchorMax = "0.65 0.25"
                }
            }, BeekeeperUiName);

            CuiHelper.AddUi(player, container);
        }

        private void CloseBeekeeperUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, BeekeeperUiName);
        }

        #endregion

        #region Interaction

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (player == null || input == null)
                return;

            if (!input.WasJustPressed(BUTTON.USE))
                return;

            if (beekeeperNpc == null || beekeeperNpc.IsDestroyed)
                return;

            float lastUse;
            if (interactionCooldowns.TryGetValue(player.userID, out lastUse))
            {
                if (Time.realtimeSinceStartup - lastUse < 1.5f)
                    return;
            }

            RaycastHit hit;

            if (!Physics.Raycast(player.eyes.HeadRay(), out hit, 3f))
                return;

            BaseEntity entity = hit.GetEntity();

            if (entity == null)
                return;

            if (entity != beekeeperNpc)
                return;

            interactionCooldowns[player.userID] = Time.realtimeSinceStartup;

            OpenBeekeeperUI(player);
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
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CloseBeekeeperUI(player);
            }

            SaveData();
        }

        #endregion
    }
}