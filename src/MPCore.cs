using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FluffyUnderware.DevTools.Extensions;
using LiteNetLib;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.GettingOverIt.Types;
using Oxide.GettingOverItMP.Components;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Oxide.GettingOverIt
{
    public class MPCore : GOIPlugin
    {
        private GameObject uiGameObject;

        public MPCore()
        {
            Title = "Getting Over It with Bennett Foddy Multiplayer";
            Author = MPExtension.AssemblyAuthors;
            IsCorePlugin = true;
        }

        [HookMethod("Init")]
        private void Init()
        {

        }

        [HookMethod("OnSceneChanged")]
        private void OnSceneChanged(SceneType sceneType, Scene scene)
        {
            if (sceneType == SceneType.Game)
            {
                RemotePlayer.CreatePlayerPrefab();
                InitUI();
                
                Interface.Oxide.LogDebug("GameObjects:");

                var gameObjects = GameObject.FindObjectsOfType<GameObject>().Distinct().ToList();
                LogGameObjects(gameObjects);

                Interface.Oxide.LogDebug("");
            }
            else
            {
                DestroyUI();
            }
        }

        private void LogGameObjects(IEnumerable<GameObject> gameObjects, int indentLevel = 0)
        {
            foreach (GameObject gameObject in gameObjects)
            {
                if (gameObject.layer != (int) LayerType.Player)
                    continue;

                Interface.Oxide.LogDebug(new string(' ', indentLevel) + GameObjectToString(gameObject));

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

        }

        private void InitUI()
        {
            if (uiGameObject != null && uiGameObject)
                return;

            uiGameObject = new GameObject("GOIMP.UI");
            uiGameObject.AddComponent<ModUI>();
        }

        private void DestroyUI()
        {
            if (uiGameObject == null || !uiGameObject)
                return;

            GameObject.Destroy(uiGameObject);
        }

        private string GameObjectToString(GameObject go)
        {
            var builder = new StringBuilder($"{go.name} ({LayerMask.LayerToName(go.layer)})");

            foreach (var component in go.GetComponents<Component>())
            {
                if (component is Transform)
                    continue;

                builder.Append("\n- " + component.GetType().Name);
            }

            return builder.ToString();
        }
    }
}
