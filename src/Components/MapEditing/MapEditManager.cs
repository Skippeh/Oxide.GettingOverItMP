using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components.MapEditing
{
    public class MapEditManager : MonoBehaviour
    {
        public bool EditModeEnabled { get; private set; }

        private LocalPlayer localPlayer;

        private void Start()
        {
            localPlayer = GameObject.Find("Player").GetComponent<LocalPlayer>() ?? throw new NotImplementedException("Could not find LocalPlayer");
        }

        public void EnableEditMode()
        {
            if (EditModeEnabled)
                return;

            localPlayer.Disable();

            EditModeEnabled = true;
        }

        public void DisableEditMode()
        {
            if (!EditModeEnabled)
                return;

            localPlayer.Enable();

            EditModeEnabled = false;
        }
    }
}
