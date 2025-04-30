// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public async ValueTask<int> ReceiveFromAsync(
        Memory<byte> buffer, SocketAddress address, CancellationToken cancellationToken)
    {
        const int numberOfSyncSpins = 10;
        SpinWait spin = default;

        for (var i = 0; i < numberOfSyncSpins; i++)
        {
            if (this.TryReceiveMessageOnChannel(buffer.Span, in address, out var size))
            {
                return size;
            }

            if (spin.NextSpinWillYield)
            {
                spin.SpinOnce();
                continue;
            }

            break;
        }

        return await this.ReceiveMessageOnChannelAsync(buffer, address, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask<SocketReceiveFromResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

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
    public ValueTask<int> SendToAsync(
        ReadOnlyMemory<byte> buffer, SocketAddress socketAddress, CancellationToken cancellationToken)
    {
        ref var identity = ref socketAddress.AsSteamNetworkingIdentity();

        var result = this.steamNetMsgs.SendMessageToUser(
            identity,
            buffer.Span,
            ESteamNetworkingSendType.UnreliableNoNagle | ESteamNetworkingSendType.AutoRestartBrokenSession,
            this.Port);

        if (result is not EResult.OK)
        {
            return ValueTask.FromException<int>(new SteamEResultException(result));
        }

        return ValueTask.FromResult(buffer.Length);
    }

    /// <inheritdoc/>
    public ValueTask<int> SendToAsync(
        ReadOnlyMemory<byte> buffer, EndPoint remoteEndPoint, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public void Close()
    {
    }

    private unsafe bool TryReceiveMessageOnChannel(Span<byte> buffer, in SocketAddress address, out int payloadSize)
    {
        Span<nint> msgPtrs = stackalloc nint[1];

        try
        {
            var msgReceived = this.steamNetMsgs.ReceiveMessagesOnChannel(this.Port, msgPtrs);

            if (msgReceived is 0)
            {
                payloadSize = 0;
                return false;
            }

            ref readonly var msg = ref Unsafe.AsRef<SteamNetworkingMessage_t>(msgPtrs[0].ToPointer());
            payloadSize = msg.Size;

            ReadOnlySpan<byte> payload = new(msg.Data.ToPointer(), msg.Size);
            payload.CopyTo(buffer);

            ref var identity = ref address.AsSteamNetworkingIdentity();
            identity = msg.IdentityPeer;
            return true;
        }
        finally
        {
            if (msgPtrs[0] != nint.Zero)
            {
                SteamNetworkingMessage_t.Release(msgPtrs[0]);
            }
        }
    }

    private Task<int> ReceiveMessageOnChannelAsync(
        Memory<byte> buffer,
        SocketAddress address,
        CancellationToken cancellationToken) =>
        Task.Run(
            async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (this.TryReceiveMessageOnChannel(buffer.Span, in address, out var size))
                    {
                        return size;
                    }

                    await Task.Yield();
                }

                return 0;
            },
            cancellationToken);
}
