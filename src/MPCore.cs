using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FluffyUnderware.DevTools.Extensions;
using LiteNetLib;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.GettingOverIt.Types;
using Oxide.GettingOverItMP.Components;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oxide.GettingOverIt
{
    public class MPCore : GOIPlugin
    {
        private GameObject uiGameObject;

        private GameObject localPlayer;
        private PlayerControl localPlayerControl;
        private PoseControl localPoseControl;
        private MPBasePlayer localPlayerBase;
        private RemotePlayer ghostPlayer;

        public MPCore()
        {
            Title = "Getting Over It with Bennett Foddy Multiplayer";
            Author = MPExtension.AssemblyAuthors;
            IsCorePlugin = true;
        }

        [HookMethod("Init")]
        private void Init()
        {
            Physics2D.IgnoreLayerCollision((int) LayerType.Player, (int) LayerType.Layer31); // Use layer 31 for remote players
        }

        [HookMethod("OnSceneChanged")]
        private void OnSceneChanged(SceneType sceneType, Scene scene)
        {
            if (sceneType == SceneType.Game)
            {
                RemotePlayer.CreatePlayerPrefab();

                Interface.Oxide.LogDebug("Created RemotePlayer prefab:");
                LogGameObjects(new[] {RemotePlayer.PlayerPrefab});
                
                localPlayer = GameObject.Find("Player") ?? throw new NotImplementedException("Could not find local player");
                localPlayerControl = localPlayer.GetComponent<PlayerControl>() ?? throw new NotImplementedException("Could not find PlayerControl on local player");
                localPoseControl = localPlayer.transform.Find("dude/mixamorig:Hips").GetComponent<PoseControl>() ?? throw new NotImplementedException("Could not find PoseControl on local player");
                localPlayer.AddComponent<PlayerDebug>();
                localPlayerBase = localPlayer.AddComponent<LocalPlayer>();
                
                // Create debug ghost player
                ghostPlayer = RemotePlayer.CreatePlayer("Ghost");

                InitUI();
            }
            else
            {
                DestroyUI();
            }
        }

        private void LogGameObjects(IEnumerable<GameObject> gameObjects, int indentLevel = 0)
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

        protected override void Tick()
        {
            if (ghostPlayer != null)
            {
                ghostPlayer.ApplyMove(localPlayerBase.CreateMove());
            }
        }

        private void InitUI()
        {
            if (uiGameObject != null && uiGameObject)
                return;

            uiGameObject = new GameObject("GOIMP.UI");
            var modUi = uiGameObject.AddComponent<ModUI>();
            modUi.LocalPlayer = localPlayer;
        }

        private void DestroyUI()
        {
            if (uiGameObject == null || !uiGameObject)
                return;

            GameObject.Destroy(uiGameObject);
        }

        private string GameObjectToString(GameObject go)
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
