# Backdash.Gns

Backdash.Gns adds [GnsSharp](https://github.com/nalchi-net/GnsSharp) extension to [Backdash](https://github.com/lucasteles/Backdash).

With this integration, you can use Backdash with [Steam Networking Messages](https://partner.steamgames.com/doc/api/ISteamNetworkingMessages).

## State of integration

Currently, only the Steamworks SDK integration is supported.

Open-source standalone [GameNetworkingSockets](https://github.com/ValveSoftware/GameNetworkingSockets) is **not** supported yet, because of some upstream issues.\
See [Issue #1](https://github.com/nalchi-net/Backdash.Gns/issues/1) for details.

## Usage

You first need to initialize GnsSharp and call `RunCallbacks()` periodically in somewhere.\
See [GnsSharp repo](https://github.com/nalchi-net/GnsSharp) for more info about this.

After that, you can build the GameNetworkingSockets session something like this:
```cs
// Port number is used as a "Channel" number for `ISteamNetworkingMessages`.
// The smaller the better, but Backdash doesn't allow port `0`, so we're using `1` here.
int channel = 1;

// Set the peer identity  (assuming we're using Steamworks SDK for this example)
SteamNetworkingIdentity peerIdentity = default;
peerIdentity.ParseString("steamid:76561198951047696");

// one local player + one `SteamEndPoint` remote player
int playerCount = 2;
var players = new NetcodePlayer[playerCount];
players[0] = NetcodePlayer.CreateLocal()
players[1] = NetcodePlayer.CreateRemote(new SteamEndPoint(peerIdentity, channel));

NetcodeSessionBuilder<MyGameInput> builder = RollbackNetcode
    .WithInputType<MyGameInput>()
    .UseGameNetworkingSockets()    // Use GnsSharp integration (extension method)
    .WithPort(channel)             // Set the "Channel" number for `ISteamNetworkingMessages`
    .WithPlayerCount(playerCount)
    .WithPlayers(players)
    .ForRemote();

INetcodeSession<MyGameInput> session = builder.Build();
```

The rest should be the same as the [regular Backdash usage](https://lucasteles.github.io/Backdash/docs/developer_guide.html).

## License

This integration *itself* is licensed under the [0BSD](LICENSE), so you can use this freely without including an additional license file.

However, you still need to follow the licenses of the underlying projects:
* [Backdash](https://github.com/lucasteles/Backdash) is licensed under the [MIT License](https://github.com/lucasteles/Backdash/blob/master/LICENCE.md).
* [GnsSharp](https://github.com/nalchi-net/GnsSharp) is licensed under the [MIT License](https://github.com/nalchi-net/GnsSharp/blob/main/LICENSE).
