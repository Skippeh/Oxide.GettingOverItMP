using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Oxide.GettingOverItMP.Components
{
    public class LocalPlayerDebug : MonoBehaviour
    {
        private bool wasLocked;
        private bool showUi;
        private bool lastShowUi;

        private void OnGUI()
        {
            lastShowUi = showUi;
            showUi = Input.GetKey(KeyCode.F3);

            if (showUi && !lastShowUi)
            {
                wasLocked = Cursor.lockState == CursorLockMode.Locked;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (!showUi && lastShowUi && wasLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (showUi)
            {
                Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D raycastHit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero, 1000);
                GUIContent content;
                StringBuilder builder = new StringBuilder();

                if (raycastHit.transform != null)
                {
                    builder.Append("World:\n- ");
                    builder.AppendLine($"{GetPathString(raycastHit.transform.gameObject)} ({LayerMask.LayerToName(raycastHit.transform.gameObject.layer)})");
                }

                var pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                if (results.Any())
                {
                    builder.AppendLine("UI:");
                    builder.AppendLine($"- {GetPathString(results.Last().gameObject)}");
                }

                content = new GUIContent(builder.ToString());
                Vector2 size = GUI.skin.label.CalcSize(content);
                Rect rect = new Rect(Input.mousePosition.x + 10, Screen.height - Input.mousePosition.y + 10, size.x, size.y);
                Rect shadowRect = new Rect(rect);
                shadowRect.x += 1;
                shadowRect.y += 1;

                Color oldColor = GUI.color;

                GUI.color = Color.black;
                GUI.Label(shadowRect, content);

                GUI.color = Color.yellow;
                GUI.Label(rect, content);

                GUI.color = oldColor;
            }
        }

        private string GetPathString(GameObject gameObject)
        {
            if (gameObject == null)
                return "null";

            var builder = new StringBuilder();

            if (gameObject.transform.parent != null)
                builder.Append(GetPathString(gameObject.transform.parent.gameObject) + "/");

            builder.Append(gameObject.name);
            return builder.ToString();
        }
    }
}
