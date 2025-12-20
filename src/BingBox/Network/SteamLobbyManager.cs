using System;
using Steamworks;
using BingBox.Utils;
using UnityEngine;

namespace BingBox.Network;

public class SteamLobbyManager
{
    private Callback<LobbyCreated_t> _lobbyCreated = null!;
    private Callback<LobbyEnter_t> _lobbyEntered = null!;
    private Callback<LobbyDataUpdate_t> _lobbyDataUpdated = null!;
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate = null!;

    private const string ROOM_ID_KEY = "BingBoxRoomId";

    public SteamLobbyManager()
    {
        if (!SteamAPI.IsSteamRunning())
        {
            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogError("[SteamLobbyManager] Steam is NOT running. Lobby sync will be disabled.");
            return;
        }

        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdated = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        if (Plugin.DebugConfig.Value)
            Plugin.Log.LogInfo("[SteamLobbyManager] Initialized Steam callbacks.");

        RoomIdManager.OnRoomIdChanged += OnLocalRoomIdChanged;
    }

    private void OnLobbyCreated(LobbyCreated_t param)
    {
        if (param.m_eResult != EResult.k_EResultOK) return;

        var lobbyId = new CSteamID(param.m_ulSteamIDLobby);
        if (Plugin.DebugConfig.Value)
            Plugin.Log.LogInfo($"[SteamLobbyManager] Lobby Created! ID: {lobbyId}");

        SetRoomIdToLobby(lobbyId, RoomIdManager.CurrentRoomId);
    }

    private void OnLobbyEntered(LobbyEnter_t param)
    {
        var lobbyId = new CSteamID(param.m_ulSteamIDLobby);

        if (Plugin.DebugConfig.Value)
            Plugin.Log.LogInfo($"[SteamLobbyManager] Entered Lobby: {lobbyId}");

        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
        CSteamID myId = SteamUser.GetSteamID();

        if (ownerId == myId)
        {
            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogInfo("[SteamLobbyManager] We are Lobby Owner. Syncing Local Room ID to Lobby.");
            SetRoomIdToLobby(lobbyId, RoomIdManager.CurrentRoomId);
        }
        else
        {
            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogInfo("[SteamLobbyManager] We are Client. checking for Room ID from Lobby.");
            SyncRoomIdFromLobby(lobbyId);
        }
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t param)
    {
        if (param.m_ulSteamIDLobby == param.m_ulSteamIDMember)
        {
            var lobbyId = new CSteamID(param.m_ulSteamIDLobby);
            SyncRoomIdFromLobby(lobbyId);
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t param)
    {
        if (param.m_ulSteamIDUserChanged == SteamUser.GetSteamID().m_SteamID)
        {
            uint state = param.m_rgfChatMemberStateChange;
            if ((state & 0x0002) != 0 || (state & 0x0004) != 0 || (state & 0x0008) != 0 || (state & 0x0010) != 0)
            {
                _currentLobbyId = CSteamID.Nil;

                string newId = RoomIdManager.GenerateNewRoomId();
                RoomIdManager.SetRoomId(newId);

                if (Plugin.DebugConfig.Value)
                    Plugin.Log.LogInfo($"[SteamLobbyManager] Local user left lobby. Regenerated Room ID: {newId}");
            }
        }
    }

    private void OnLocalRoomIdChanged(string newRoomId)
    {
        if (_currentLobbyId != CSteamID.Nil && Plugin.SyncRoomWithLobby)
        {
            if (SteamMatchmaking.GetLobbyOwner(_currentLobbyId) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyData(_currentLobbyId, ROOM_ID_KEY, newRoomId);
                if (Plugin.DebugConfig.Value)
                    Plugin.Log.LogInfo($"[SteamLobbyManager] Local Room ID changed. Updated Lobby Data: {newRoomId}");
            }
        }
    }

    private CSteamID _currentLobbyId = CSteamID.Nil;

    private void SetRoomIdToLobby(CSteamID lobbyId, string roomId)
    {
        _currentLobbyId = lobbyId;
        if (Plugin.SyncRoomWithLobby)
        {
            SteamMatchmaking.SetLobbyData(lobbyId, ROOM_ID_KEY, roomId);
            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogInfo($"[SteamLobbyManager] Set Lobby Data: {ROOM_ID_KEY} = {roomId}");
        }
    }

    private void SyncRoomIdFromLobby(CSteamID lobbyId)
    {
        _currentLobbyId = lobbyId;
        if (!Plugin.SyncRoomWithLobby) return;

        string data = SteamMatchmaking.GetLobbyData(lobbyId, ROOM_ID_KEY);
        if (!string.IsNullOrEmpty(data))
        {
            if (data == RoomIdManager.CurrentRoomId) return;

            if (Plugin.DebugConfig.Value)
                Plugin.Log.LogInfo($"[SteamLobbyManager] Received Room ID from Lobby: {data}");

            RoomIdManager.SetRoomId(data);
        }
    }
    public void OnLocalUserLeftLobby()
    {
        _currentLobbyId = CSteamID.Nil;

        string newId = RoomIdManager.GenerateNewRoomId();
        RoomIdManager.SetRoomId(newId);

        if (Plugin.DebugConfig.Value)
            Plugin.Log.LogInfo($"[SteamLobbyManager] Intercepted LeaveLobby. Regenerated Room ID: {newId}");
    }
}
