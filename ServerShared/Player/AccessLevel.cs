using System;

namespace ServerShared.Player
{
    /// <summary>
    /// Defines the access level a NetPlayer has.
    /// </summary>
    public enum AccessLevel
    {
        Player,
        Moderator,
        Admin,
        Console
    }
}
