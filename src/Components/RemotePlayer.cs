using System;
using System.Collections.Generic;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
using Oxide.Core;
using Oxide.GettingOverIt.Types;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oxide.GettingOverItMP.Components
{
    public class RemotePlayer : MPBasePlayer
    {
        public static GameObject PlayerPrefab { get; private set; }
        
        public static readonly List<RemotePlayer> RemotePlayers = new List<RemotePlayer>();

        public string Name
        {
            get => nameContent.text;
            set => nameContent = new GUIContent(value);
        }

        private GUIStyle labelStyle;
        private GUIContent nameContent;

        public static void CreatePlayerPrefab()
        {
            PlayerPrefab = Instantiate(GameObject.FindObjectsOfType<GameObject>().Single(obj => obj.name == "Player") ?? throw new NotImplementedException("Could not find Player object"), Vector3.zero, Quaternion.identity);
            PlayerPrefab.SetActive(false);
            PlayerPrefab.AddComponent<RemotePlayer>();

            DestroyImmediate(PlayerPrefab.GetComponent<Saviour>());
            DestroyImmediate(PlayerPrefab.GetComponent<PlayerControl>());
            DestroyImmediate(PlayerPrefab.GetComponent<Screener>());
            DestroyImmediate(PlayerPrefab.transform.Find("dude/mixamorig:Hips").GetComponent<PoseControl>());
            DestroyImmediate(PlayerPrefab.GetComponent<HingeJoint2D>());
            DestroyImmediate(PlayerPrefab.GetComponent<Rigidbody2D>());

            DestroyImmediate(PlayerPrefab.GetComponentInChildren<PotSounds>());
            DestroyImmediate(PlayerPrefab.GetComponentInChildren<HammerCollisions>());
            DestroyImmediate(PlayerPrefab.GetComponentInChildren<PlayerSounds>());

            PlayerPrefab.name = "RemotePlayer_Prefab";
            
            // Remove all physics components
            foreach (var joint in PlayerPrefab.GetComponentsInChildren<Joint2D>())
            {
                DestroyImmediate(joint);
            }

            foreach (var rigidBody2D in PlayerPrefab.GetComponentsInChildren<Rigidbody2D>())
            {
                DestroyImmediate(rigidBody2D);
            }

            // Remove all colliders
            foreach (var collider in PlayerPrefab.GetComponentsInChildren<Collider2D>())
            {
                DestroyImmediate(collider);
            }
        }

        public static RemotePlayer CreatePlayer(string name)
        {
            var clone = Instantiate(PlayerPrefab);
            clone.SetActive(true);
            clone.name = "RemotePlayer_" + name;
            var remotePlayer = clone.GetComponent<RemotePlayer>();
            remotePlayer.Name = name;
            return remotePlayer;
        }

        protected override void Start()
        {
            base.Start();
            RemotePlayers.Add(this);
            nameContent = new GUIContent(Name);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            RemotePlayers.Remove(this);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (labelStyle == null)
                labelStyle = GUI.skin.GetStyle("Label");
            
            Vector2 screenPosition = Camera.current.WorldToScreenPoint(transform.position + transform.up * 1.5f);
            Vector2 textSize = labelStyle.CalcSize(nameContent);

            screenPosition.y = Screen.height - screenPosition.y;
            
            Rect textRect = new Rect(screenPosition.x - (textSize.x / 2f), screenPosition.y - (textSize.y / 2f), textSize.x, textSize.y);
            
            GUI.Label(textRect, nameContent);
        }
    }
}
