using System.Data;
using System.Reflection;
using Lidgren.Network;
using ServerShared;
using UnityEngine;

namespace Oxide.GettingOverItMP.Components
{
    public class ModUI : MonoBehaviour
    {
        private GameObject localPlayer;

        private PlayerControl control;
        private Client client;
        private ChatUI chatUi;
        private ServerBrowserUI serverBrowser;
        
        private void Start()
        {
            localPlayer = GameObject.Find("Player");
            control = localPlayer.GetComponent<PlayerControl>();
            client = GameObject.Find("GOIMP.Client").GetComponent<Client>();
            chatUi = gameObject.AddComponent<ChatUI>();
            serverBrowser = gameObject.AddComponent<ServerBrowserUI>();
        }
    }
}
