using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class FreeCameraController : MonoBehaviour
    {
        public float MinimumOrtoSize => initialOrtoSize * 0.1f;
        public float MaximumOrtoSize => initialOrtoSize * 10f;
        private float ZoomMultiplier => Camera.orthographicSize / initialOrtoSize;

        private Camera Camera => Camera.main;
        private float initialOrtoSize;
        
        private Vector3 mouseOrigin;

        private void Awake()
        {
            initialOrtoSize = Camera.orthographicSize;
        }
        
        private void OnDisable()
        {
            //Camera.orthographicSize = initialOrtoSize;
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                mouseOrigin = Input.mousePosition;
            }

            Vector2 translation = Vector2.zero;
            
            if (Input.GetMouseButton(1))
            {
                Vector3 worldOrigin = Camera.main.ScreenToWorldPoint(mouseOrigin);
                Vector3 worldMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                translation = (worldMouse - worldOrigin) / 20f;
            }

            if (translation.sqrMagnitude > 0)
            {
                Translate(translation);
            }

            if (!Input.GetKey(KeyCode.F3)) // Only change zoom if F3 is not being held (debug button for viewing current object under cursor)
            {
                float newOrtoSize = Camera.orthographicSize + (-Input.mouseScrollDelta.y * 1.25f);
                Camera.orthographicSize = Mathf.Clamp(newOrtoSize, MinimumOrtoSize, MaximumOrtoSize);

                // Reset zoom on middle mouse click
                if (Input.GetMouseButtonDown(2))
                {
                    Camera.orthographicSize = initialOrtoSize;
                }
            }
        }

        /// <summary>
        /// Returns the amount of pixels per unit
        /// </summary>
        private float GetPPU(Camera camera)
        {
            var screenPos = new Vector3(Screen.width, Screen.height, 0);
            Vector3 worldPos = camera.ScreenToWorldPoint(screenPos);
            Vector3 worldPosOffset = camera.ScreenToWorldPoint(screenPos + new Vector3(1, 1, 0));
            var result = (camera.transform.InverseTransformPoint(worldPos) - camera.transform.InverseTransformPoint(worldPosOffset)).magnitude;

            //Debug.Log($"{worldPos} - {worldPosOffset} = {result} ({worldPos}, {worldPosOffset}");
            //Debug.Log(result);

            return result;
        }

        private void Translate(Vector2 translation)
        {
            Camera.main.transform.Translate(translation.x, translation.y, 0, Space.Self);
        }
    }
}
