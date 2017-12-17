using Oxide.GettingOverItMP.Components;

namespace Oxide.GettingOverItMP.EventArgs
{
    public class PlayerJoinedEventArgs : System.EventArgs
    {
        public RemotePlayer Player;
    }

    public class PlayerLeftEventArgs : System.EventArgs
    {
        public RemotePlayer Player;
    }
}
