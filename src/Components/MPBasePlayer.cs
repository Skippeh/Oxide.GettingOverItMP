using System;
using Oxide.Core;
using ServerShared.Player;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public abstract class MPBasePlayer : MonoBehaviour
    {
        public int Id;
        public abstract string PlayerName { get; set; }
        public int Wins { get; set; }

        protected Animator dudeAnim;
        protected Transform handle;
        protected Transform slider;
        protected Client client;
        protected Transform lookTarget;
        protected Transform tip;

        protected Material PotMaterial { get; private set; }
        private bool renderersEnabled = true;
        private float goldness;

        protected virtual void Start()
        {
            Interface.Oxide.LogDebug($"{GetType().Name} Start");

            dudeAnim = transform.Find("dude")?.GetComponent<Animator>() ?? throw new NotImplementedException("Could not find dude");
            handle = transform.Find("Hub/Slider/Handle") ?? throw new NotImplementedException("Could not find Hub/Slider/Handle");
            slider = transform.Find("Hub/Slider") ?? throw new NotImplementedException("Could not find Hub/Slider");
            lookTarget = transform.Find("dude/LookTarget") ?? throw new NotImplementedException("Could not find LookTarget");
            tip = transform.Find("Hub/Slider/Handle/PoleMiddle/Tip") ?? throw new NotImplementedException("Could not find tip");

            client = GameObject.Find("GOIMP.Client").GetComponent<Client>() ?? throw new NotImplementedException("Could not find Client");

            gameObject.AddComponent<PlayerDebug>();

            var potObject = transform.Find("Pot/Mesh");
            var potRenderer = potObject.GetComponent<MeshRenderer>();
            PotMaterial = potRenderer.material;
        }

        protected virtual void OnDestroy()
        {
        }

        protected virtual void OnGUI()
        {
        }

        protected virtual void Update()
        {
            
        }

        public PlayerMove CreateMove()
        {
            if (!dudeAnim || !handle|| !slider)
            {
                Interface.Oxide.LogError($"dudeAnim, handle, or slider is not valid ({!!dudeAnim} {!!handle} {!!slider}");
                throw new NotImplementedException();
            }

            return new PlayerMove
            {
                AnimationAngle = dudeAnim.GetFloat("Angle"),
                AnimationExtension = dudeAnim.GetFloat("Extension"),

                HandlePosition = handle.position,
                HandleRotation = handle.rotation,

                SliderPosition = slider.position,
                SliderRotation = slider.rotation,

                Position = transform.position,
                Rotation = transform.rotation
            };
        }

        public void SetGoldness(float goldness)
        {
            goldness = Mathf.Clamp01(goldness);

            if (Math.Abs(goldness - (double) this.goldness) < 0.001f)
                return;

            this.goldness = goldness;
            PotMaterial.SetFloat("_Goldness", goldness);
        }

        /// <summary>Sets the material color on the pot. Note that the color will be blended with the pot's texture color.</summary>
        public void SetPotColor(Color color)
        {
            // The pot material doesn't expose a _Color parameter so this code doesn't actually do anything right now.
            PotMaterial.color = color;
        }

        public void EnableRenderers()
        {
            if (renderersEnabled)
                return;

            foreach (var meshRenderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                meshRenderer.enabled = true;
            }

            renderersEnabled = true;
        }

        public void DisableRenderers()
        {
            if (!renderersEnabled)
                return;

            foreach (var meshRenderer in gameObject.GetComponentsInChildren<Renderer>())
            {
                meshRenderer.enabled = false;
            }

            renderersEnabled = false;
        }
    }
}
