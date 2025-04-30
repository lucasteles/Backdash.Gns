// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using GnsSharp;

/// <summary>
/// Provides a <see cref="GnsSharp.SteamNetworkingIdentity"/> endpoint.
/// </summary>
[Serializable]
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

    /// <summary>Tries to parse a string into a value.</summary>
    /// <param name="channel">Steam endpoint channel.</param>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="s" /> or an undefined value on failure.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="s" /> was successfully parsed; otherwise, <see langword="false" />.</returns>
    public static bool TryParse(
        int channel,
        [NotNullWhen(true)] string? s,
        [MaybeNullWhen(false)] out SteamEndPoint result)
    {
        SteamNetworkingIdentity identity = default;

        if (s is not null && identity.ParseString(s))
        {
            result = new SteamEndPoint(identity, channel);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>Parses a string into a value.</summary>
    /// <param name="channel">Steam endpoint channel.</param>
    /// <param name="s">The string to parse.</param>
    /// <returns>The result of parsing <paramref name="s" />.</returns>
    public static SteamEndPoint Parse(int channel, string s)
    {
        if (!TryParse(channel, s, out var endPoint))
        {
            throw new InvalidOperationException($"Invalid Steam endpoint: {s}");
        }

        return endPoint;
    }

    /// <inheritdoc/>
    public override SocketAddress Serialize() => this.Identity.ToSocketAddress();

    /// <inheritdoc/>
    public override EndPoint Create(SocketAddress socketAddress) => throw new NotImplementedException();

    /// <inheritdoc/>
    public override string ToString() => $"{this.Identity}:ch{this.Channel}";
}
