// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using System.Net;
using System.Net.Sockets;
using GnsSharp;

/// <summary>
/// Provides a <see cref="GnsSharp.SteamNetworkingIdentity"/> endpoint.
/// </summary>
public class SteamEndPoint : EndPoint
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SteamEndPoint"/> class.
    /// </summary>
    /// <param name="identity">Steam networking identity that represents the endpoint.</param>
    /// <param name="channel">Channel number to use for <a href="https://partner.steamgames.com/doc/api/ISteamNetworkingMessages">Steam Networking Messages</a>.</param>
    public SteamEndPoint(SteamNetworkingIdentity identity, int channel)
    {
        this.Identity = identity;
        this.Channel = channel;
    }

    /// <inheritdoc/>
    public override AddressFamily AddressFamily => AddressFamily.Unspecified;

    /// <summary>
    /// Gets the internal <see cref="SteamNetworkingIdentity"/>.
    /// </summary>
    public SteamNetworkingIdentity Identity { get; }

    /// <summary>
    /// Gets the channel.
    /// </summary>
    public int Channel { get; }

    /// <inheritdoc/>
    public override SocketAddress Serialize()
    {
        return this.Identity.ToSocketAddress();
    }

    /// <inheritdoc/>
    public override EndPoint Create(SocketAddress socketAddress) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override string ToString() => $"{this.Identity}:ch{this.Channel}";
}
