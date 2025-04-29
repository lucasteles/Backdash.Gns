// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using Backdash;
using Backdash.Options;
using GnsSharp;

/// <summary>
/// <see cref="NetcodeSessionBuilder{TInput}"/> extensions.
/// </summary>
public static class NetcodeSessionBuilderExtensions
{
    /// <summary>
    /// <para>
    /// Use GameNetworkingSockets for communication.
    /// </para>
    /// <para>
    /// You can use this method for both the Steamworks SDK and the open-source standalone GameNetworkingSockets.
    /// </para>
    /// </summary>
    /// <remarks>
    /// NOTE: Open-source standalone GameNetworkingSockets is <b>unsupported</b> at the moment.<br/>
    /// See <a href="https://github.com/nalchi-net/Backdash.Gns/issues/1">Backdash.Gns#1</a> for the reasons.
    /// </remarks>
    /// <typeparam name="TInput">Input type.</typeparam>
    /// <param name="builder">Session builder.</param>
    /// <returns>The same session <paramref name="builder"/> passed in the parameter.</returns>
    public static NetcodeSessionBuilder<TInput> UseGameNetworkingSockets<TInput>(this NetcodeSessionBuilder<TInput> builder)
        where TInput : unmanaged
    {
        return builder.ConfigureServices((ServicesConfig<TInput> services) =>
        {
            services.PeerSocketFactory = new SteamSocketFactory();
        })
        .ConfigureProtocol((ProtocolOptions protocol) =>
        {
            unsafe
            {
                protocol.ReceiveSocketAddressSize = sizeof(SteamNetworkingIdentity);
            }
        })
        .UsePlugin(new SteamSessionPlugin());
    }
}
