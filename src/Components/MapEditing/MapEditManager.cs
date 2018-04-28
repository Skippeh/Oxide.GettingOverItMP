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

        private void Update()
        {
            if (EditModeEnabled)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void EnableEditMode()
        {
            if (EditModeEnabled)
                return;

            localPlayer.Disable();
            localPlayer.FreeCamera.enabled = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            EditModeEnabled = true;
        }

        public void DisableEditMode()
        {
            if (!EditModeEnabled)
                return;

            localPlayer.Enable();
            localPlayer.FreeCamera.enabled = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            EditModeEnabled = false;
        }
    }
}
