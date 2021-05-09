using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class Spectator : MonoBehaviour
    {
        public bool Spectating => Target != null;
        public RemotePlayer Target { get; private set; }

        private LocalPlayer localPlayer;
        private CameraControl cameraControl;
        private Client client;

        private GUIContent text1;
        private GUIContent text2;

        private GUIStyle style1;
        private GUIStyle style2;
        
        private void Start()
        {
            localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>() ?? throw new NotImplementedException("Could not find LocalPlayer");
            cameraControl = GameObject.Find("Main Camera").GetComponent<CameraControl>() ?? throw new NotImplementedException("Could not find CameraControl");
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>() ?? throw new NotImplementedException("Could not find Client");

            text1 = new GUIContent("[SPECTATING]");
            text2 = new GUIContent("Press [SPACE] to stop spectating");
        }

        private void Update()
        {
            if (Target != null)
            {
                Vector3 targetPosition = Target.transform.position;
                targetPosition.z = Camera.main.transform.position.z;

                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, targetPosition, 10f * Time.deltaTime);
            }
        }

        private void OnGUI()
        {
            if (style1 == null)
            {
                style1 = new GUIStyle(GUI.skin.GetStyle("Label"));
                style2 = new GUIStyle(style1);

                style1.fontSize = 20;
            }

            if (!Spectating)
                return;
            
            Vector2 size1 = style1.CalcSize(text1);
            Vector2 size2 = style2.CalcSize(text2);

            Rect rect1 = new Rect(Screen.width / 2f - size1.x / 2f, 15, size1.x, size1.y);
            Rect rect2 = new Rect(Screen.width / 2f - size2.x / 2f, 15 + size1.y, size2.x, size2.y);
            Rect background = new Rect(rect2.x - 5, rect1.y - 3, size2.x + 10, size1.y + size2.y + 6);

            GUI.Box(background, "");
            GUI.Label(rect1, text1, style1);
            GUI.Label(rect2, text2, style2);
        }

        public void SpectatePlayer(RemotePlayer player)
        {
            if (Target == player)
                return;

            Target = player;

            Vector3 cameraLocation = Camera.main.transform.position;
            float oldZ = cameraLocation.z;

            Vector3 cameraZ0 = cameraLocation;
            cameraZ0.z = 0;

            if (player != null)
            {
                DisableLocalPlayer();

                // Teleport camera if target is 20+ meters away
                if ((cameraZ0 - player.transform.position).sqrMagnitude > 20 * 20)
                {
                    cameraLocation = player.transform.position;
                    cameraLocation.z = oldZ;

                    Camera.main.transform.position = cameraLocation;
                }
            }
            else
            {
                EnableLocalPlayer();

                if ((cameraZ0 - localPlayer.transform.position).sqrMagnitude > 20 * 20)
                {
                    cameraLocation = localPlayer.transform.position;
                    cameraLocation.z = oldZ;

                    Camera.main.transform.position = cameraLocation;
                }
            }
        }

        public void StopSpectating()
        {
            SpectatePlayer(null);
        }

        private void EnableLocalPlayer()
        {
            localPlayer.EnableRenderers();
            localPlayer.GetComponent<PlayerControl>().PauseInput(0);
            cameraControl.enabled = true;
        }

        private void DisableLocalPlayer()
        {
            localPlayer.GetComponent<PlayerControl>().PauseInput(float.MinValue);
            localPlayer.DisableRenderers();
            cameraControl.enabled = false;
        }
    }
}
