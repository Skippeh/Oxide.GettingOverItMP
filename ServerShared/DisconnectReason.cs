namespace ServerShared
{
    public enum DisconnectReason : byte
    {
        Invalid,

        /// <summary>
        /// Client failed to send handshake within the time limit.
        /// </summary>
        HandshakeTimeout,

        /// <summary>
        /// Client tried to send a handshake more than once.
        /// </summary>
        DuplicateHandshake,

        /// <summary>
        /// Client version is too old.
        /// </summary>
        VersionOlder,

        /// <summary>
        /// Client version is too new.
        /// </summary>
        VersionNewer,

        /// <summary>
        /// Client sent a message with an invalid MessageType, or serialized the data incorrectly.
        /// </summary>
        InvalidMessage,

        /// <summary>
        /// Client tried to use a name that was too long or contained invalid characters.
        /// </summary>
        InvalidName,

        /// <summary>
        /// Client tried to send any message other than ClientHandshake before having received a successful handshake response.
        /// </summary>
        NotAccepted,

        /// <summary>
        /// An error occured while trying to verify the user's steam session ticket.
        /// </summary>
        InvalidSteamSession,

        /// <summary>
        /// Client is banned from this server.
        /// </summary>
        Banned,
    }
}
