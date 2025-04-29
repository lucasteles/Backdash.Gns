// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using GnsSharp;

/// <summary>
/// <see cref="SteamNetworkingIdentity"/> related extensions.
/// </summary>
public static class SteamNetworkingIdentityExtensions
{
    /// <summary>
    /// Converts <see cref="SteamNetworkingIdentity"/> to <see cref="SocketAddress"/>.
    /// </summary>
    /// <param name="identity">Steam Networking identity.</param>
    /// <returns><see cref="SocketAddress"/> containing <see cref="SteamNetworkingIdentity"/> internally.</returns>
    public static SocketAddress ToSocketAddress(this in SteamNetworkingIdentity identity)
    {
        unsafe
        {
            var addr = new SocketAddress(AddressFamily.Unspecified, sizeof(SteamNetworkingIdentity));

            var identitySpan = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(in identity, 1));
            identitySpan.CopyTo(addr.Buffer.Span);

            return addr;
        }
    }

    /// <summary>
    /// Reinterprets <see cref="SocketAddress"/> as <see cref="SteamNetworkingIdentity"/>.
    /// </summary>
    /// <param name="addr">Socket address containing <see cref="SteamNetworkingIdentity"/> internally.</param>
    /// <returns>Reinterpreted <see cref="SteamNetworkingIdentity"/>.</returns>
    public static ref SteamNetworkingIdentity AsSteamNetworkingIdentity(this SocketAddress addr)
    {
        return ref MemoryMarshal.AsRef<SteamNetworkingIdentity>(addr.Buffer.Span);
    }
}
