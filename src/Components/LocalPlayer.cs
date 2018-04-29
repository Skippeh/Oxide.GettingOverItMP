using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class LocalPlayer : MPBasePlayer
    {
        public float OriginalGoldness { get; private set; }
        public Color OriginalPotColor { get; private set; }

        public override string PlayerName { get; set; }

        public FreeCameraController FreeCamera { get; private set; }

        private Spectator spectator;
        private CameraControl cameraControl;
        private PlayerControl playerControl;
        private bool physicsEnabled = true;

        protected override void Start()
        {
            base.Start();
            
            spectator = GameObject.Find("GOIMP.Spectator").GetComponent<Spectator>() ?? throw new NotImplementedException("Could not find Spectator");
            gameObject.AddComponent<LocalPlayerDebug>();
            
            cameraControl = GameObject.Find("Main Camera").GetComponent<CameraControl>() ?? throw new NotImplementedException("Could not find CameraControl");
            playerControl = GetComponent<PlayerControl>();
            FreeCamera = gameObject.AddComponent<FreeCameraController>();
            FreeCamera.enabled = false;

            OriginalGoldness = ProceduralMaterial.GetProceduralFloat("Goldness");
            OriginalPotColor = ProceduralMaterial.color;
        }

        protected override void Update()
        {
            base.Update();

            if (spectator.Spectating)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    client.SendStopSpectating();
                }

                if (Input.GetMouseButtonDown(0))
                {
                    client.SendSwitchSpectateTarget(1);
                }
                if (Input.GetMouseButtonDown(1))
                {
                    client.SendSwitchSpectateTarget(-1);
                }
            }
        }

        public void ResetPotProperties()
        {
            SetPotColor(OriginalPotColor);
            SetGoldness(OriginalGoldness);
        }

        public void Enable()
        {
            EnableRenderers();
            EnablePhysics();
            playerControl.enabled = true;
            cameraControl.enabled = true;
        }

        public void Disable()
        {
            DisableRenderers();
            DisablePhysics();
            playerControl.fakeCursor.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            playerControl.enabled = false;
            cameraControl.enabled = false;
        }

        public override void EnableRenderers()
        {
            if (!renderersEnabled)
            {
                playerControl.fakeCursor.GetComponent<SpriteRenderer>().enabled = true;
            }

            base.EnableRenderers();
        }

        public override void DisableRenderers()
        {
            if (renderersEnabled)
            {
                playerControl.fakeCursor.GetComponent<SpriteRenderer>().enabled = false;
            }

            base.DisableRenderers();
        }

        public void EnablePhysics()
        {
            if (physicsEnabled)
                return;

            foreach (Rigidbody2D rigidBody in GetComponentsInChildren<Rigidbody2D>())
            {
                rigidBody.isKinematic = false;
            }
            
            physicsEnabled = true;
        }

        public void DisablePhysics()
        {
            if (!physicsEnabled)
                return;

            foreach (Rigidbody2D rigidBody in GetComponentsInChildren<Rigidbody2D>())
            {
                rigidBody.isKinematic = true;
                rigidBody.velocity = Vector2.zero;
                rigidBody.angularVelocity = 0;
            }
            
            physicsEnabled = false;
        }
    }
}
