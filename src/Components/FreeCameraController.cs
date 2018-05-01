using System;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class FreeCameraController : MonoBehaviour
    {
        public float CameraSpeed = 10;
        public float SpeedBoostMultiplier = 2f;

        public float MinimumOrtoSize => initialOrtoSize * 0.1f;
        public float MaximumOrtoSize => initialOrtoSize * 10f;
        private float ZoomMultiplier => Camera.orthographicSize / initialOrtoSize;

        private Camera Camera => Camera.main;
        private float initialOrtoSize;
        
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
            float boost = (Input.GetKey(KeyCode.LeftShift) ? SpeedBoostMultiplier : 1) * ZoomMultiplier;

            Vector2 translation = Vector2.zero;

            if (Input.GetKey(KeyCode.W))
            {
                translation.y += 1;
            }

            if (Input.GetKey(KeyCode.S))
            {
                translation.y -= 1;
            }

            if (Input.GetKey(KeyCode.A))
            {
                translation.x -= 1;
            }

            if (Input.GetKey(KeyCode.D))
            {
                translation.x += 1;
            }

            if (translation.sqrMagnitude > 0)
            {
                translation = translation.normalized * CameraSpeed * boost;
                Translate(translation * Time.deltaTime);
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

        private void Translate(Vector2 translation)
        {
            Camera.main.transform.Translate(translation.x, translation.y, 0, Space.Self);
        }
    }
}
