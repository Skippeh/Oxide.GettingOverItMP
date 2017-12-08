using System;
using Oxide.Core;
using ServerShared.Player;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public abstract class MPBasePlayer : MonoBehaviour
    {
        protected Animator dudeAnim;
        protected Transform handle;
        protected Transform slider;
        
        protected virtual void Start()
        {
            Interface.Oxide.LogDebug($"{GetType().Name} Start");

            dudeAnim = transform.Find("dude")?.GetComponent<Animator>() ?? throw new NotImplementedException("Could not find dude");
            handle = transform.Find("Hub/Slider/Handle") ?? throw new NotImplementedException("Could not find Hub/Slider/Handle");
            slider = transform.Find("Hub/Slider") ?? throw new NotImplementedException("Could not find Hub/Slider");
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
            if (dudeAnim == null || handle == null || slider == null)
            {
                Interface.Oxide.LogError($"dudeAnim, handle, or slider is null ({dudeAnim == null} {handle == null} {slider == null}");
                return default(PlayerMove);
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
    }
}
