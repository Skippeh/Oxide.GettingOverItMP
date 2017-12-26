using Oxide.GettingOverItMP.Components;
using UnityEngine;

namespace Oxide.GettingOverItMP.EventArgs
{
    public class ChatMessageReceivedEventArgs : System.EventArgs
    {
        public string PlayerName { get; set; }
        public int PlayerId { get; set; }
        public string Message { get; set; }
        public Color Color { get; set; } = Color.white;
    }
}
