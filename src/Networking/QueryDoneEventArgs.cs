using ServerShared.Networking;

namespace Oxide.GettingOverItMP.Networking
{
    public class QueryDoneEventArgs : System.EventArgs
    {
        public bool Successful;
        public ServerInfo ServerInfo;
    }
}
