using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Facepunch.Steamworks;
using FluffyUnderware.DevTools.Extensions;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.GettingOverIt.Types;
using Oxide.GettingOverItMP.Components;
using Oxide.GettingOverItMP.Networking;
using RootMotion.FinalIK;
using ServerShared;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Client = Oxide.GettingOverItMP.Components.Client;

namespace Oxide.GettingOverIt
{
    public class MPCore : GOIPlugin
    {
        public static Facepunch.Steamworks.Client SteamClient { get; private set; }
        public static DynamicConfigFile Config { get; private set; }

        private GameObject uiGameObject;
        private GameObject clientGameObject;
        private GameObject spectateGameObject;
        private Client client;

        private GameObject localPlayer;
        private PlayerControl localPlayerControl;
        private PoseControl localPoseControl;
        private MPBasePlayer localPlayerBase;

        public MPCore()
        {
            Title = "Getting Over It with Bennett Foddy Multiplayer";
            Author = MPExtension.AssemblyAuthors;
            IsCorePlugin = true;
            Config = base.Config;
        }

        [HookMethod("Init")]
        private void Init()
        {
            Application.runInBackground = true;
            Physics2D.IgnoreLayerCollision((int) LayerType.Player, (int) LayerType.Layer31); // Use layer 31 for remote players

            if (SteamClient == null)
            {
                Facepunch.Steamworks.Config.ForUnity(Application.platform.ToString());
                SteamClient = new Facepunch.Steamworks.Client(SharedConstants.SteamAppId);

                if (!SteamClient.IsValid)
                {
                    SteamClient.Dispose();
                    SteamClient = null;
                    Interface.Oxide.LogWarning("Steam is not running.");
                }
                else if (!SteamClient.IsSubscribed)
                {
                    SteamClient.Dispose();
                    SteamClient = null;
                    Interface.Oxide.LogWarning("The current steam account is not subscribed to the game.");
                }
            }
        }

        [HookMethod("OnSceneChanged")]
        private void OnSceneChanged(SceneType sceneType, Scene scene)
        {
            if (sceneType == SceneType.Game)
            {
                RemotePlayer.CreatePlayerPrefab();
                
                localPlayer = GameObject.Find("Player") ?? throw new NotImplementedException("Could not find local player");
                localPlayerControl = localPlayer.GetComponent<PlayerControl>() ?? throw new NotImplementedException("Could not find PlayerControl on local player");
                localPoseControl = localPlayer.transform.Find("dude/mixamorig:Hips").GetComponent<PoseControl>() ?? throw new NotImplementedException("Could not find PoseControl on local player");
                localPlayerBase = localPlayer.AddComponent<LocalPlayer>();

                InitSpectator();
                InitUI();
                InitClient();
            }
            else
            {
                DestroyClient();
                DestroyUI();
                DestroySpectator();

                if (ListenServer.Running)
                    ListenServer.Stop();
            }
        }

        [HookMethod("OnGameQuit")]
        private void OnGameQuit()
        {
            SteamClient?.Dispose();
        }

        protected override void Tick()
        {
            SteamClient?.Update();

            if (ListenServer.Running)
                ListenServer.Update();
        }

        private void InitUI()
        {
            if (uiGameObject)
                return;

            uiGameObject = new GameObject("GOIMP.UI");
            var modUi = uiGameObject.AddComponent<ModUI>();
            var scoreboardUi = uiGameObject.AddComponent<ScoreboardUI>();
        }

        private void DestroyUI()
        {
            if (uiGameObject == null || !uiGameObject)
                return;

            GameObject.Destroy(uiGameObject);
        }

        private void InitClient()
        {
            if (clientGameObject)
                return;

            clientGameObject = new GameObject("GOIMP.Client");
            client = clientGameObject.AddComponent<Client>();
        }

        private void DestroyClient()
        {
            if (clientGameObject == null || !clientGameObject)
                return;

            GameObject.Destroy(clientGameObject);
        }

        private void InitSpectator()
        {
            if (spectateGameObject)
                return;

            spectateGameObject = new GameObject("GOIMP.Spectator");
            spectateGameObject.AddComponent<Spectator>();
        }

        private void DestroySpectator()
        {
            if (!spectateGameObject)
                return;

            GameObject.Destroy(spectateGameObject);
        }

        public static void LogGameObjects(IEnumerable<GameObject> gameObjects, int indentLevel = 0)
        {
            var gameObjectList = gameObjects.ToList();

            if (indentLevel == 0)
                Interface.Oxide.LogDebug($"# GameObjects: {gameObjectList.Count}");

            foreach (GameObject gameObject in gameObjectList)
            {
                Interface.Oxide.LogDebug(indentLevel + " " + new string(' ', indentLevel) + GameObjectToString(gameObject));

                if (gameObject == null)
                    continue;

                List<GameObject> childObjects = new List<GameObject>();
                foreach (Transform childTransform in gameObject.transform)
                {
                    childObjects.Add(childTransform.gameObject);
                }

                LogGameObjects(childObjects, indentLevel + 1);
            }
        }

        private static string GameObjectToString(GameObject go)
        {
            if (go == null)
                return "null";

            var builder = new StringBuilder($"{go.name} ({LayerMask.LayerToName(go.layer)}) [");

            Component[] components = go.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];

                if (component is Transform)
                    continue;
                
                builder.Append(component.GetType().Name);

                if (i < components.Length - 1)
                    builder.Append(", ");
            }

            builder.Append("]");
            return builder.ToString();
        }
    }
}
