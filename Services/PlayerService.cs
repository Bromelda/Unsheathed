using Unsheathed.Interfaces;

using Unsheathed.Utilities;
using Il2CppInterop.Runtime;
using ProjectM.Network;
using System.Collections.Concurrent;
using Unity.Collections;
using Unity.Entities;
using static Unsheathed.Utilities.EntityQueries;


namespace Unsheathed.Services;
/*
internal class PlayerService
{
    static EntityManager EntityManager => Core.EntityManager;

    

    static readonly ComponentType[] _userAllComponents =
    [
        ComponentType.ReadOnly(Il2CppType.Of<User>())
    ];

    static QueryDesc _userQueryDesc;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdPlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdPlayerInfoCache => _steamIdPlayerInfoCache;

    static readonly ConcurrentDictionary<ulong, PlayerInfo> _steamIdOnlinePlayerInfoCache = [];
    public static IReadOnlyDictionary<ulong, PlayerInfo> SteamIdOnlinePlayerInfoCache => _steamIdOnlinePlayerInfoCache;
    public struct PlayerInfo(Entity userEntity = default, Entity charEntity = default, User user = default)
    {
        public User User { get; set; } = user;
        public Entity UserEntity { get; set; } = userEntity;
        public Entity CharEntity { get; set; } = charEntity;
    }
   
    public static void HandleConnection(ulong steamId, PlayerInfo playerInfo)
    {
        _steamIdOnlinePlayerInfoCache[steamId] = playerInfo;
        _steamIdPlayerInfoCache[steamId] = playerInfo;
    }
   
    public static PlayerInfo GetPlayerInfo(string playerName)
    {
        return SteamIdPlayerInfoCache.FirstOrDefault(kvp => string.Equals(kvp.Value.User.CharacterName.Value,
            playerName, StringComparison.CurrentCultureIgnoreCase)).Value;
    }
}
*/