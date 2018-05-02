using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Facepunch.Steamworks;
using FluffyUnderware.DevTools.Extensions;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;
using Oxide.GettingOverIt.Types;
using Oxide.GettingOverItMP;
using Oxide.GettingOverItMP.Components;
using Oxide.GettingOverItMP.Components.CustomMaps;
using Oxide.GettingOverItMP.Components.CustomMaps.Editing;
using Oxide.GettingOverItMP.Networking;
using RootMotion.FinalIK;
using ServerShared;
using ServerShared.CustomMaps;
using ServerShared.CustomMaps.ComponentModels;
using ServerShared.CustomMaps.ComponentModels.Collision;
using ServerShared.CustomMaps.ComponentModels.Informational;
using ServerShared.CustomMaps.ComponentModels.Visual;
using Steamworks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Client = Oxide.GettingOverItMP.Components.Client;

namespace Oxide.GettingOverIt
{
    public class MPCore : GOIPlugin
    {
        public static Facepunch.Steamworks.Client SteamClient { get; private set; }

        private GameObject uiGameObject;
        private GameObject clientGameObject;
        private GameObject spectateGameObject;
        private GameObject menuUiGameObject;
        private GameObject mapManagerObject;
        private MenuUI menuUi;
        private Client client;

        private GameObject localPlayer;
        private PlayerControl localPlayerControl;
        private PoseControl localPoseControl;
        private LocalPlayer localPlayerBase;
        private MapEditManager mapEditManager;
        private MapManager mapManager;

        private IPEndPoint launchEndPoint;
        private bool firstLaunch = true;
        private bool firstEverLaunch;
        private bool updateAvailable;

        public MPCore()
        {
            Title = "Getting Over It with Bennett Foddy Multiplayer";
            Author = MPExtension.AssemblyAuthors;
            IsCorePlugin = true;
        }

        [HookMethod("Init")]
        private void Init()
        {
            Interface.Oxide.LogDebug($"Unity version: {Application.unityVersion}");
            MPContent.LoadAssetBundle();

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

            if (PlayerPrefs.GetInt("GOIMP_LaunchedBefore", 0) == 0)
            {
                PlayerPrefs.SetInt("GOIMP_LaunchedBefore", 1);
                firstEverLaunch = true;
            }

            if (!firstEverLaunch && PlayerPrefs.GetInt("GOIMP_CheckForUpdates", 0) == 1)
            {
                try
                {
                    using (var apiClient = new ApiClient())
                    {
                        Interface.Oxide.LogDebug("Checking for update...");

                        string currentVersion = MPExtension.AssemblyVersion + "_" + Application.version;
                        string latestVersion = apiClient.QueryLatestVersion(ApiClient.ModType.Client);

                        if (currentVersion != latestVersion)
                        {
                            Interface.Oxide.LogDebug($"Update available: {latestVersion}");
                            updateAvailable = true;
                        }
                    }
                }
                catch (ApiRequestFailedException ex)
                {
                    Interface.Oxide.LogError("Failed to get latest version: " + ex.Message);
                }
            }
        }

        [HookMethod("OnSceneChanged")]
        private void OnSceneChanged(SceneType sceneType, Scene scene)
        {
            Interface.Oxide.LogDebug($"Scene changed to {scene.name} ({scene.buildIndex}).");

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
                InitMapEditor();
                
                var entities = new List<MapEntityModel>();

                entities.Add(new MapEntityModel
                {
                    Id = 0,
                    Position = new Vector2(0, 160),
                    Components =
                    {
                        new EntitySpawnPointComponentModel()
                    }
                });

                entities.Add(new MapEntityModel
                {
                    Id = 1,
                    Position = new Vector3(-5, 150),
                    Components =
                    {
                        new EntityMeshComponentModel()
                        {
                            Id = 0,
                            PrefabId = "Mountain/Rock/rock_03 (3)/rock_03"
                        }
                    }
                });

                for (uint i = 0; i < 300; i += 3)
                {
                    entities.AddRange(new[]
                    {
                        new MapEntityModel
                        {
                            Id = i + 2,
                            Position = new Vector2(8f * i, 150),
                            Scale = new Vector3(10, 10, 10),
                            Components =
                            {
                                new EntityMeshComponentModel
                                {
                                    Id = 0,
                                    PrefabId = "GOIMP/Sphere"
                                },
                                new EntityCircleColliderComponentModel
                                {
                                    Id = 1,
                                    Friction = 1f,
                                    Radius = 0.5f,
                                    Material = (int) GroundCol.SoundMaterial.solidmetal
                                }
                            }
                        },
                        new MapEntityModel
                        {
                            Id = i + 3,
                            Position = new Vector2(4f * i, 150),
                            Scale = new Vector3(1, 1, 1),
                            Components =
                            {
                                new EntityMeshComponentModel
                                {
                                    Id = 0,
                                    PrefabId = "GOIMP/Cube"
                                },
                                new EntityPolygonColliderComponentModel
                                {
                                    Id = 1,
                                    Friction = 1f,
                                    Material = (int) GroundCol.SoundMaterial.metal,
                                    Points = new[]
                                    {
                                        new Vector2(-0.5f, 0.5f),
                                        new Vector2(0.5f, 0.5f),
                                        new Vector2(0.5f, -0.5f),
                                        new Vector2(-0.5f, -0.5f)
                                    }
                                }
                            }
                        }
                    });
                }

                mapManager.LoadMap(new GameMapModel
                {
                    Entities = entities
                });
                
                localPlayerBase.Teleport(new Vector2(0, 10));
            }
            else if (sceneType == SceneType.Menu)
            {
                InitMenuUI();

                if (firstLaunch)
                {
                    firstLaunch = false;

                    // Don't connect to server automatically if an update is available or if this is the first ever launch.
                    if (firstEverLaunch)
                    {
                        menuUi.ShowFirstLaunch();
                        return;
                    }

                    if (updateAvailable)
                    {
                        menuUi.ShowUpdateAvailable();
                        return;
                    }
                }
                else
                    return;

                var launchArguments = Environment.GetCommandLineArgs().Skip(1).ToArray();

                for (int i = 0; i < launchArguments.Length; i += 2)
                {
                    string argument = launchArguments[i].ToLower();
                    string value = i < launchArguments.Length - 1 ? launchArguments[i + 1].ToLower() : null;

                    if (argument == "--goimp-connect")
                    {
                        string[] ipPort = value.Split(':');
                        string ipString = ipPort[0];
                        string portString = ipPort[1];

                        IPAddress ipAddress;
                        short port;

                        if (!IPAddress.TryParse(ipString, out ipAddress))
                        {
                            Interface.Oxide.LogError($"Launch arguments contained invalid ip: {ipString}");
                            return;
                        }

                        if (!short.TryParse(portString, out port))
                        {
                            Interface.Oxide.LogError($"Launch arguments contained invalid port: {portString}");
                            return;
                        }

                        launchEndPoint = new IPEndPoint(ipAddress, port);

                        // Wait for game to finish loading then continue game.
                        var loader = GameObject.FindObjectOfType<Loader>();
                        var loadingFinishedField = typeof(Loader).GetField("loadFinished", BindingFlags.Instance | BindingFlags.NonPublic);

                        Timer.TimerInstance timerInstance = null;
                        Action timerCallback = () =>
                        {
                            bool loadingFinished = (bool) loadingFinishedField.GetValue(loader);

                            if (loadingFinished)
                            {
                                loader.ContinueGame();
                                timerInstance.Destroy();
                            }
                        };

                        timerInstance = Timer.Repeat(0, -1, timerCallback, this);
                    }
                }
            }

            if (sceneType != SceneType.Game)
            {
                DestroyMapEditor();
                DestroyClient();
                DestroyUI();
                DestroySpectator();

                if (ListenServer.Running)
                    ListenServer.Stop();
            }
            else if (sceneType != SceneType.Menu)
            {
                DestroyMenuUI();
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

        private void InitMenuUI()
        {
            menuUiGameObject = new GameObject("GOIMP.Menu.UI");
            menuUi = menuUiGameObject.AddComponent<MenuUI>();
        }

        private void DestroyMenuUI()
        {
            if (!menuUiGameObject)
                return;

            GameObject.Destroy(menuUiGameObject);
            menuUi = null;
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
            if (!uiGameObject)
                return;

            GameObject.Destroy(uiGameObject);
        }

        private void InitClient()
        {
            if (clientGameObject)
                return;

            clientGameObject = new GameObject("GOIMP.Client");
            client = clientGameObject.AddComponent<Client>();

            if (launchEndPoint != null)
            {
                client.StartCoroutine("LaunchConnect", launchEndPoint);
                launchEndPoint = null;
            }
        }

        private void InitMapEditor()
        {
            if (mapManagerObject)
                return;

            MapManager.LoadMapObjects();
            mapManagerObject = new GameObject("GOIMP.MapManager");
            mapManager = mapManagerObject.AddComponent<MapManager>();
            mapEditManager = mapManagerObject.AddComponent<MapEditManager>();
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

        private void DestroyMapEditor()
        {
            if (!mapManagerObject)
                return;

            MapManager.DestroyMapObjects();
            GameObject.Destroy(mapManagerObject);
        }

        public static void LogGameObjects(IEnumerable<GameObject> gameObjects)
        {
            Interface.Oxide.LogDebug(LogGameObjectsToString(gameObjects));
        }

        public static string LogGameObjectsToString(IEnumerable<GameObject> gameObjects, int indentLevel = 0)
        {
            var builder = new StringBuilder();
            var gameObjectList = gameObjects.ToList();

            if (indentLevel == 0)
                builder.AppendLine($"# GameObjects: {gameObjectList.Count}");

            foreach (GameObject gameObject in gameObjectList)
            {
                builder.AppendLine(indentLevel + " " + new string(' ', indentLevel) + GameObjectToString(gameObject));

                if (gameObject == null)
                    continue;

                List<GameObject> childObjects = new List<GameObject>();
                foreach (Transform childTransform in gameObject.transform)
                {
                    childObjects.Add(childTransform.gameObject);
                }

                builder.Append(LogGameObjectsToString(childObjects, indentLevel + 1));
            }

            return builder.ToString();
        }

        public static string GameObjectToString(GameObject go)
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
