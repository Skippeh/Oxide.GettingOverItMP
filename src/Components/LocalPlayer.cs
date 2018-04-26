using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class LocalPlayer : MPBasePlayer
    {
        public float OriginalGoldness { get; private set; }
        public Color OriginalPotColor { get; private set; }

        public override string PlayerName { get; set; }

        private Spectator spectator;
        private CameraControl cameraControl;
        private PlayerControl playerControl;

        protected override void Start()
        {
            base.Start();
            
            spectator = GameObject.Find("GOIMP.Spectator").GetComponent<Spectator>() ?? throw new NotImplementedException("Could not find Spectator");
            gameObject.AddComponent<LocalPlayerDebug>();
            
            cameraControl = GameObject.Find("Main Camera").GetComponent<CameraControl>() ?? throw new NotImplementedException("Could not find CameraControl");
            playerControl = GetComponent<PlayerControl>();

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
            playerControl.PauseInput(0);
            cameraControl.enabled = true;
        }

        public void Disable()
        {
            playerControl.PauseInput(float.MinValue);
            DisableRenderers();
            cameraControl.enabled = false;
        }
    }
}
