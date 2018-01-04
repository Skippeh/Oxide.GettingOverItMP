using System;
using System.Linq;
using FluffyUnderware.DevTools.Extensions;
using Oxide.Core;
using Oxide.GettingOverIt.Types;
using ServerShared.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oxide.GettingOverItMP.Components
{
    public class RemotePlayer : MPBasePlayer
    {
        public static GameObject PlayerPrefab { get; private set; }
        
        public override string PlayerName
        {
            get => nameContent.text;
            set => nameContent = new GUIContent(value);
        }

        private GUIStyle labelStyle;
        private GUIContent nameContent;
        
        private PlayerMove targetMove;
        private PlayerMove lastMove;
        private float interpTarget;
        private float interpElapsed;

        private static LocalPlayer localPlayer;

        private float lastReceiveMoveTime;

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
            DestroyImmediate(PlayerPrefab.transform.Find("PotCollider/Sensor").gameObject);

            var cameras = PlayerPrefab.GetComponentsInChildren<Camera>();
            foreach (var camera in cameras)
                DestroyImmediate(camera);

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

        public static RemotePlayer CreatePlayer(string name, int id)
        {
            var clone = Instantiate(PlayerPrefab);
            clone.SetActive(true);
            clone.name = "RemotePlayer_" + id;
            var remotePlayer = clone.GetComponent<RemotePlayer>();
            remotePlayer.PlayerName = name;
            remotePlayer.Id = id;
            return remotePlayer;
        }

        public void ApplyMove(PlayerMove move)
        {
            ApplyMove(move, client.LastReceiveDelta);
        }

        public void ApplyMove(PlayerMove move, float interpTime)
        {
            if (dudeAnim == null || handle == null || slider == null)
            {
                Interface.Oxide.LogError($"dudeAnim, handle, or slider is null ({!!dudeAnim} {!!handle} {!!slider})");
                return;
            }

            lastReceiveMoveTime = Time.time;

            if (interpTime > 0)
            {
                interpElapsed = 0;
                interpTarget = interpTime;
                lastMove = CreateMove();
                targetMove = move;
            }
            else
            {
                interpTarget = -1;
                ApplyMoveInterp(ref move, 1);
            }
        }
        
        protected override void Start()
        {
            base.Start();
            nameContent = new GUIContent(PlayerName);

            if (localPlayer == null)
            {
                localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>();
            }
        }

        protected override void Update()
        {
            base.Update();

            if (interpTarget > 0)
            {
                interpElapsed += Time.deltaTime;

                float t = Mathf.Clamp01(interpElapsed / interpTarget);
                ApplyMoveInterp(ref targetMove, t);
            }

            lookTarget.position = Vector3.Lerp(lookTarget.position, tip.position, 7 * Time.deltaTime);
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            if (labelStyle == null)
                labelStyle = GUI.skin.GetStyle("Label");

            Vector2 textSize;
            Vector2 screenPosition = Camera.current.WorldToScreenPoint(transform.position + transform.up * 1.5f);
            screenPosition.y = Screen.height - screenPosition.y;

            GUIContent nameContent;
            if (screenPosition.x < 0 || screenPosition.y < 0 || screenPosition.x >= Screen.width || screenPosition.y >= Screen.height)
            {
                Vector3 cameraPosition = Camera.main.transform.position;
                cameraPosition.z = 0;

                nameContent = new GUIContent(this.nameContent);
                nameContent.text += $" ({Vector3.Distance(cameraPosition, transform.position):0.0}m)";
                textSize = labelStyle.CalcSize(nameContent);

                screenPosition.x = Mathf.Clamp(screenPosition.x, textSize.x / 2, Screen.width - textSize.x / 2);
                screenPosition.y = Mathf.Clamp(screenPosition.y, textSize.y / 2, Screen.height - textSize.y / 2);
            }
            else
            {
                nameContent = this.nameContent;
                textSize = labelStyle.CalcSize(nameContent);
            }

            Rect textRect = new Rect(screenPosition.x - (textSize.x / 2f), screenPosition.y - (textSize.y / 2f), textSize.x, textSize.y);
            Rect shadowRect = new Rect(textRect);
            shadowRect.position += Vector2.one;

            Color oldColor = GUI.color;

            GUI.color = Color.black;
            GUI.Label(shadowRect, nameContent);

            GUI.color = Color.white;
            GUI.Label(textRect, nameContent);

            GUI.color = oldColor;
        }

        private void ApplyMoveInterp(ref PlayerMove move, float t)
        {
            dudeAnim.SetFloat("Angle", Mathf.LerpAngle(lastMove.AnimationAngle, targetMove.AnimationAngle, t));
            dudeAnim.SetFloat("Extension", Mathf.Lerp(lastMove.AnimationExtension, targetMove.AnimationExtension, t));
            dudeAnim.Update(Time.deltaTime);

            transform.position = Vector3.Lerp(lastMove.Position, targetMove.Position, t);
            transform.rotation = Quaternion.Lerp(lastMove.Rotation, targetMove.Rotation, t);

            handle.position = Vector3.Lerp(lastMove.HandlePosition, targetMove.HandlePosition, t);
            handle.rotation = Quaternion.Lerp(lastMove.HandleRotation, targetMove.HandleRotation, t);

            slider.position = Vector3.Lerp(lastMove.SliderPosition, targetMove.SliderPosition, t);
            slider.rotation = Quaternion.Lerp(lastMove.SliderRotation, targetMove.SliderRotation, t);

            Vector3 cameraPosition = Camera.main.transform.position;
            cameraPosition.z = 0;
            
            // Hide players that are 15+ meters away
            if ((transform.position - cameraPosition).sqrMagnitude >= 15 * 15)
            {
                DisableRenderers();
            }
            else
            {
                EnableRenderers();
            }

            // Todo: fix hands not being positioned correctly.
        }
    }
}
