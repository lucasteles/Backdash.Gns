// SPDX-FileCopyrightText: Copyright 2025 Guyeon Yu <copyrat90@gmail.com>
// SPDX-License-Identifier: 0BSD

namespace Backdash.Gns;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Backdash;
using Backdash.Options;
using GnsSharp;

/// <summary>
/// Plugin to manage the lifecycle of the <a href="https://partner.steamgames.com/doc/api/ISteamNetworkingMessages#functions_manage_sessions">Steam Networking Messages sessions</a>.
/// </summary>
public sealed class SteamSessionPlugin : INetcodePlugin
{
    private readonly HashSet<SteamNetworkingIdentity> endPointIdentities = [];

    private FnSteamNetworkingMessagesSessionRequest? steamNetMsgsSessionRequest;

    private bool disposed = true;

    /// <summary>
    /// Finalizes an instance of the <see cref="SteamSessionPlugin"/> class.
    /// </summary>
    ~SteamSessionPlugin() => this.Dispose();

    /// <inheritdoc/>
    public void OnEndpointAdded(INetcodeSession session, EndPoint endPoint, NetcodePlayer player)
    {
        ThrowIfNotSteamEndpoint(endPoint);

        var steamEndPoint = (SteamEndPoint)endPoint;
        this.endPointIdentities.Add(steamEndPoint.Identity);
    }

    /// <inheritdoc/>
    public void OnEndpointClosed(INetcodeSession session, EndPoint endPoint, NetcodePlayer player)
    {
        ThrowIfNotSteamEndpoint(endPoint);

        var steamEndPoint = (SteamEndPoint)endPoint;
        this.endPointIdentities.Remove(steamEndPoint.Identity);
    }

    /// <inheritdoc/>
    public void OnSessionStart(INetcodeSession session)
    {
        ThrowIfSteamInterfacesNull();
        ThrowIfMessageSessionRequestCallbackAlreadyExists();

        // Register MessageSessionRequest callback for this session.
        this.steamNetMsgsSessionRequest = (ref SteamNetworkingMessagesSessionRequest_t req) =>
        {
            // Accept the messages session if it's one of the endpoints.
            if (this.endPointIdentities.Contains(req.IdentityRemote))
            {
                ISteamNetworkingMessages.User!.AcceptSessionWithUser(req.IdentityRemote);
            }
        };

        ISteamNetworkingUtils.User!.SetGlobalCallback_MessagesSessionRequest(this.steamNetMsgsSessionRequest);

        GC.ReRegisterForFinalize(this);
        this.disposed = false;
    }

    /// <inheritdoc/>
    public void OnSessionClose(INetcodeSession session)
    {
        this.Dispose();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        GC.SuppressFinalize(this);

        // Remove message session request callback
        if (this.steamNetMsgsSessionRequest is not null)
        {
            unsafe
            {
                delegate* unmanaged[Cdecl]<SteamNetworkingMessagesSessionRequest_t*, void> nullCallback = null;
                ISteamNetworkingUtils.User?.SetGlobalCallback_MessagesSessionRequest(nullCallback);
            }

            this.steamNetMsgsSessionRequest = null;
        }

        // Close underlying Steam Networking Messages sessions
        foreach (var identity in this.endPointIdentities)
        {
            ISteamNetworkingMessages.User?.CloseSessionWithUser(identity);
        }
    }

    private static void ThrowIfNotSteamEndpoint(EndPoint endPoint)
    {
        if (endPoint is not SteamEndPoint)
        {
            throw new ArgumentException("endpoint was not a SteamEndPoint");
        }
    }

    private static void ThrowIfSteamInterfacesNull()
    {
        if (ISteamNetworkingUtils.User is null)
        {
            throw new InvalidOperationException(
                "ISteamNetworkingUtils.User is null. Call SteamAPI.Init() or SteamAPI.InitEx() beforehand.");
        }

        if (ISteamNetworkingMessages.User is null)
        {
            throw new InvalidOperationException(
                "ISteamNetworkingMessages.User is null. Call SteamAPI.Init() or SteamAPI.InitEx() beforehand.");
        }
    }

    /// <summary>
    /// Check if there's already a MessagesSessionRequest callback registered.<br/>
    /// We shouldn't overwrite it in that case, otherwise it would mess up already ongoing message session.
    /// </summary>
    private static void ThrowIfMessageSessionRequestCallbackAlreadyExists()
    {
        unsafe
        {
            nint callbackPtr;
#if BACKDASH_GNS_PLATFORM_64
            var callbackPtrSize = (ulong)sizeof(IntPtr);
#elif BACKDASH_GNS_PLATFORM_32
            var callbackPtrSize = (uint)sizeof(nint);
#else
#error "Unknown pointer size. Define `BACKDASH_GNS_PLATFORM_64` or `BACKDASH_GNS_PLATFORM_32` according to your platform."
#endif
            Span<byte> callbackPtrSpan = new(&callbackPtr, (int)callbackPtrSize);

            var getConfigResult = ISteamNetworkingUtils.User?.GetConfigValue(
                ESteamNetworkingConfigValue.Callback_MessagesSessionRequest,
                ESteamNetworkingConfigScope.Global,
                nint.Zero,
                callbackPtrSpan,
                ref callbackPtrSize);

            Debug.Assert(
                getConfigResult is ESteamNetworkingGetConfigValueResult.OK
                    or ESteamNetworkingGetConfigValueResult.OKInherited,
                $"GetConfigValue failed with {getConfigResult}");

            if (callbackPtr != nint.Zero)
            {
                throw new InvalidOperationException("There was already MessagesSessionRequest callback registered.");
            }
        }
    }
}
