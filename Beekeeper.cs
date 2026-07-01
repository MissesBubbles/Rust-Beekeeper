using System;
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
            public bool PluginEnabled = true;
            public bool DebugMode = false;

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
            if (args == null || args.Length == 0 || args[0].ToLower() == "help")
            {
                SendHelp(player);
                return;
            }

            string subCommand = args[0].ToLower();

            switch (subCommand)
            {
                case "spawn":
                    player.ChatMessage("Beekeeper spawn command coming next.");
                    break;

                case "remove":
                    player.ChatMessage("Beekeeper remove command coming later.");
                    break;

                case "move":
                    player.ChatMessage("Beekeeper move command coming later.");
                    break;

                case "reload":
                    player.ChatMessage("Beekeeper reload command coming later.");
                    break;

                default:
                    player.ChatMessage("Unknown beekeeper command. Use /beekeeper help");
                    break;
            }
        }

        private void SendHelp(BasePlayer player)
        {
            player.ChatMessage("<color=#ffd479>Beekeeper Commands</color>");
            player.ChatMessage("/beekeeper help");
            player.ChatMessage("/beekeeper spawn");
            player.ChatMessage("/beekeeper remove");
            player.ChatMessage("/beekeeper move");
            player.ChatMessage("/beekeeper reload");
        }

        #endregion

        #region Hooks

        private void Init()
        {
            LoadConfigData();
            LoadData();
            RegisterPermissions();

            Puts("Beekeeper v0.2.0 loaded.");
        }

        private void Unload()
        {
            SaveData();
        }

        #endregion
    }
}