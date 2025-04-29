// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Backdash.Network.Client;
using GnsSharp;

/// <summary>
/// <a href="https://partner.steamgames.com/doc/api/ISteamNetworkingMessages">Steam Networking Messages</a> socket interface.
/// </summary>
public sealed class SteamSocket : IPeerSocket
{
    private readonly ISteamNetworkingMessages steamNetMsgs;

    private readonly int channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="SteamSocket"/> class.
    /// </summary>
    /// <param name="channel">Channel number to use for <see cref="ISteamNetworkingMessages.SendMessageToUser"/>.</param>
    /// <param name="steamNetMsgs">Steam networking messages interface to use.</param>
    public SteamSocket(int channel, ISteamNetworkingMessages steamNetMsgs)
    {
        this.channel = channel;
        this.steamNetMsgs = steamNetMsgs;
    }

    /// <summary>
    /// Gets the channel number to use for <a href="https://partner.steamgames.com/doc/api/ISteamNetworkingMessages">Steam Networking Messages</a>.
    /// </summary>
    public int Port => this.channel;

    /// <inheritdoc cref="Socket.AddressFamily"/>
    public AddressFamily AddressFamily => AddressFamily.Unspecified;

    /// <summary>
    /// Receives a datagram into the data buffer, using the specified SocketFlags, and stores the endpoint.
    /// </summary>
    /// <param name="buffer">The buffer for the received data.</param>
    /// <param name="address">
    /// A <see cref="SocketAddress "/> instance that gets updated with the value of the remote peer
    /// when this method returns.
    /// The internal representation of <see cref="SocketAddress.Buffer"/> is <see cref="SteamNetworkingIdentity"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns><see cref="ValueTask"/> containing receive task.</returns>
    public ValueTask<int> ReceiveFromAsync(Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken)
    {
        return new ValueTask<int>(Task.Run<int>(
            async () =>
            {
                IntPtr[] msgPtrs = new IntPtr[1];

                try
                {
                    while (true)
                    {
                        int msgReceived = this.steamNetMsgs.ReceiveMessagesOnChannel(this.Port, msgPtrs);
                        if (msgReceived != 0)
                        {
                            break;
                        }

                        await Task.Delay(1, cancellationToken);
                    }

                    int payloadSize;
                    unsafe
                    {
                        ref readonly var msg = ref new ReadOnlySpan<SteamNetworkingMessage_t>((void*)msgPtrs[0], 1)[0];
                        payloadSize = msg.Size;

                        ReadOnlySpan<byte> payload = new((void*)msg.Data, msg.Size);
                        payload.CopyTo(buffer.Span);

                        ref var identity = ref address.AsSteamNetworkingIdentity();

                        identity = msg.IdentityPeer;
                    }

                    return payloadSize;
                }
                finally
                {
                    if (msgPtrs[0] != IntPtr.Zero)
                    {
                        SteamNetworkingMessage_t.Release(msgPtrs[0]);
                    }
                }
            },
            cancellationToken));
    }

    /// <inheritdoc />
    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <summary>
    /// Sends data to the specified steam networking identity host.
    /// </summary>
    /// <param name="buffer">The buffer for the data to send.</param>
    /// <param name="socketAddress">
    /// The remote host to which to send the data.
    /// The internal representation of <see cref="SocketAddress.Buffer"/> must be <see cref="SteamNetworkingIdentity"/>.
    /// </param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns><see cref="ValueTask"/> containing send task.</returns>
    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken)
    {
        ref SteamNetworkingIdentity identity = ref socketAddress.AsSteamNetworkingIdentity();

        EResult result = this.steamNetMsgs.SendMessageToUser(identity, buffer.Span, ESteamNetworkingSendType.UnreliableNoNagle | ESteamNetworkingSendType.AutoRestartBrokenSession, this.Port);

        if (result != EResult.OK)
        {
            return ValueTask.FromException<int>(new SteamEResultException(result));
        }

        return ValueTask.FromResult<int>(buffer.Length);
    }

    /// <inheritdoc/>
    public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public void Close()
    {
    }
}
