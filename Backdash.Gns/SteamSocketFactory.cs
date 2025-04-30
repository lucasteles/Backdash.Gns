// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using Backdash.Network.Client;
using Backdash.Options;
using GnsSharp;

/// <summary>
/// Factory for <see cref="SteamSocket"/>.
/// </summary>
internal sealed class SteamSocketFactory : IPeerSocketFactory
{
    /// <summary>
    /// Create a <see cref="SteamSocket"/>.
    /// </summary>
    /// <param name="channel">Channel number to use for <a href="https://partner.steamgames.com/doc/api/ISteamNetworkingMessages">Steam Networking Messages</a>.</param>
    /// <param name="options">Netcode options.</param>
    /// <returns><see cref="IPeerSocket"/> that is <see cref="SteamSocket"/>.</returns>
    public IPeerSocket Create(int channel, NetcodeOptions options)
    {
        if (ISteamNetworkingMessages.User is null)
        {
            throw new InvalidOperationException(
                "ISteamNetworkingMessages.User is null. Call SteamAPI.Init() or SteamAPI.InitEx() beforehand.");
        }

        return new SteamSocket(channel, ISteamNetworkingMessages.User);
    }
}
