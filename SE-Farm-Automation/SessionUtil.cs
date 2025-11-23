using Sandbox.ModAPI;

namespace FarmerAutomation
{
    public class SessionUtil
    {
        /// <summary>
        /// True for anything except dedicated server
        /// </summary>
        public static readonly bool IsClient = !(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer);

        /// <summary>
        /// True for Dedicated server and self-hosted games
        /// </summary>
        public static readonly bool IsServer = MyAPIGateway.Session.IsServer;

        /// <summary>
        /// True for dedicated server
        /// </summary>
        public static readonly bool IsDedicatedServer = MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer;

        /// <summary>
        /// True if current session host is a dedicated server
        /// </summary>
        public static readonly bool IsDedicated = MyAPIGateway.Utilities.IsDedicated;
    }
}