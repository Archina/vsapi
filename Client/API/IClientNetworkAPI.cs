﻿using Vintagestory.API.Common;

namespace Vintagestory.API.Client
{
    /// <summary>
    /// The state of a network channel
    /// </summary>
    public enum EnumChannelState
    {
        /// <summary>
        /// No such channel was registered
        /// </summary>
        NotFound,
        /// <summary>
        /// This channel has been registered but he server did not send the server channel information yet
        /// </summary>
        Registered,
        /// <summary>
        /// This channel has been registered client and server side. It is ready to send and receive messages
        /// </summary>
        Connected,
        /// <summary>
        /// This channel has been registered only client side. You cannot send data on this channel
        /// </summary>
        NotConnected
    }

    /// <summary>
    /// API Features to set up a network channel for custom server&lt;-&gt;client data exchange. Client side.
    /// </summary>
    public interface IClientNetworkAPI
    {
        /// <summary>   
        /// Supplies you with your very own and personal network channel with which you can send packets to the server. Use the same channelName on the client and server to have them link up.
        /// </summary>
        /// <param name="channelName">Unique channel identifier</param>
        /// <returns></returns>
        IClientNetworkChannel RegisterChannel(string channelName);

        /// <summary>
        /// Check in what state a channel currently is in
        /// </summary>
        /// <param name="channelName"></param>
        /// <returns></returns>
        EnumChannelState GetChannelState(string channelName);

        /// <summary>
        /// Sends a blockentity interaction packet to the server. For quick an easy blockentity network communication without setting up a channel first.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="packetId"></param>
        /// <param name="data"></param>
        void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null);

        /// <summary>
        /// Sends a entity interaction packet to the server. For quick an easy entity network communication without setting up a channel first.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="packetId"></param>
        /// <param name="data"></param>
        void SendEntityPacket(long entityid, int packetId, byte[] data = null);


        /// <summary>
        /// Sends a blockentity interaction packet to the server. For quick an easy blockentity network communication without setting up a channel first.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="packetId"></param>
        /// <param name="internalPacket"></param>
        void SendBlockEntityPacket(int x, int y, int z, object internalPacket);


        /// <summary>
        /// Sends a entity interaction packet to the server. For quick an easy entity network communication without setting up a channel first.
        /// </summary>
        /// <param name="entityid"></param>
        /// <param name="internalPacket"></param>
        void SendEntityPacket(long entityid, object internalPacket);


        /// <summary>
        /// Sends given packet data to the server. This let's you mess with the raw network communication and fiddle with internal engine packets if you know the protocol. For normal network communication you probably want to register your own network channel.
        /// </summary>
        /// <param name="data"></param>
        void SendArbitraryPacket(byte[] data);


        /// <summary>
        /// Sends given packet to server. For use with inventory supplied network packets only, since the packet format is not exposed to the api 
        /// </summary>
        /// <param name="packetClient">The network packet to send.</param>
        void SendPacketClient(object packetClient);

        /// <summary>
        /// Sends the current hand interaction.  
        /// </summary>
        /// <param name="mouseButton">the current mouse button press</param>
        /// <param name="blockSelection">the currently selected Block (if there is one)</param>
        /// <param name="entitySelection">the currently selected Entity (if there is one)</param>
        /// <param name="beforeUseType"></param>
        /// <param name="state">The state of the hand.</param>
        /// <param name="firstEvent">Is it the first of this events for this block? (by default the client calls the interaction every second while the player holds down the right mouse button)</param>
        /// <param name="cancelReason">The reason we cancelled the use of an item (if there is  one)</param>
        void SendHandInteraction(int mouseButton, BlockSelection blockSelection, EntitySelection entitySelection, EnumHandInteract beforeUseType, int state, bool firstEvent, EnumItemUseCancelReason cancelReason);
    }
}
