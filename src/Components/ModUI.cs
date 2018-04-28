using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        private ChatUI chatUi;
        private ServerBrowserUI serverBrowser;
        
        private void Start()
        {
            chatUi = gameObject.AddComponent<ChatUI>();
            serverBrowser = gameObject.AddComponent<ServerBrowserUI>();
        }
    }
}
