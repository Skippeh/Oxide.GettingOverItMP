using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class LocalPlayer : MPBasePlayer
    {
        public override string PlayerName { get; set; }

        private Spectator spectator;

        protected override void Start()
        {
            base.Start();

            spectator = GameObject.Find("GOIMP.Spectator").GetComponent<Spectator>() ?? throw new NotImplementedException("Could not find Spectator");
            gameObject.AddComponent<LocalPlayerDebug>();
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
    }
}
