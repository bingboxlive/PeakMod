using System;
using BepInEx;
using BepInEx.Configuration;
using BingBox.Logging;
using BingBox.Settings;
using BingBox.UI;
using BingBox.Utils;
using BingBox.WebRTC;

namespace BingBox;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static LoggerWrapper Log { get; private set; } = null!;
    public static BepInEx.PluginInfo InstanceInfo { get; private set; } = null!;
    public static Plugin Instance { get; private set; } = null!;

    private static ConfigEntry<string> _usernameConfig = null!;
    private static ConfigEntry<string> _liveUrlConfig = null!;
    private static ConfigEntry<string> _userIdConfig = null!;
    public static ConfigEntry<bool> DopplerConfig = null!;
    public static ConfigEntry<bool> DebugConfig = null!;
    public static ConfigEntry<bool> UseWebSocketAudio = null!;
    public static bool SyncRoomWithLobby { get; set; } = true;

    public BingBox.Network.SteamLobbyManager? SteamLobbyManager { get; private set; }

    public static string Username
    {
        get => _usernameConfig.Value;
        set
        {
            if (_usernameConfig.Value == value) return;
            _usernameConfig.Value = value;
            Plugin.Log.LogInfo($"Config Saved: Username = {value}");
        }
    }

    public static string LiveUrl
    {
        get => _liveUrlConfig.Value;
        set
        {
            if (_liveUrlConfig.Value == value) return;
            _liveUrlConfig.Value = value;
            if (DebugConfig != null && DebugConfig.Value)
            {
                Plugin.Log.LogInfo($"Config Saved: LiveUrl = {value}");
            }
        }
    }

    public static string UserId => _userIdConfig.Value;

    private void Awake()
    {
        Instance = this;
        Log = new LoggerWrapper(Logger);

        DependencyLoader.Init();

        _usernameConfig = Config.Bind("General", "Username", StringUtils.GetRandomDefaultUsername(), "Your BingBox username.");
        _liveUrlConfig = Config.Bind("General", "LiveUrl", "bingbox.live", "The BingBox Live URL.");
        _userIdConfig = Config.Bind("General", "UserId", "", "Unique Auto-Generated User ID. Do not edit.");

        DopplerConfig = Config.Bind("Settings", "DopplerEffect", true, "Enable Doppler Effect.");
        DebugConfig = Config.Bind("Settings", "EnableDebugging", false, "Enable Debug Logging.");
        UseWebSocketAudio = Config.Bind("Settings", "UseWebSocketAudio", false, "Use WebSocket for audio (Bypasses firewall/proxy issues).");

        if (string.IsNullOrEmpty(_userIdConfig.Value))
        {
            _userIdConfig.Value = StringUtils.GenerateRandomId();
            Plugin.Log.LogInfo($"Generated New User ID: {_userIdConfig.Value}");
        }

        string newRoomId = RoomIdManager.GenerateNewRoomId();
        if (DebugConfig.Value)
        {
            Log.LogInfo($"Generated Session Room ID: {newRoomId}");
        }

        Log.LogInfo($"BingBox Loaded - User: {_usernameConfig.Value}, ID: {_userIdConfig.Value}, Room: {newRoomId}");

        InstanceInfo = Info;
        BingBoxSettings.Initialize();
        gameObject.AddComponent<SettingsInjector>();
        gameObject.AddComponent<PauseMenuInjector>();
        gameObject.AddComponent<BingBox.Audio.BingBoxAudioManager>();

        HarmonyLib.Harmony.CreateAndPatchAll(typeof(BingBox.Audio.ItemAudioPatch), "pro.kenn.bingbox.audio");
        HarmonyLib.Harmony.CreateAndPatchAll(typeof(BingBox.Network.SteamMatchmakingPatch), "pro.kenn.bingbox.network");

        try
        {
            SetupSipSorceryLogging();
        }
        catch (System.Exception ex)
        {
            Log.LogError($"Failed to setup SIPSorcery logging: {ex}");
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void SetupSipSorceryLogging()
    {
        if (DebugConfig.Value)
        {
            SIPSorcery.LogFactory.Set(new BingBox.Logging.BepInExLoggerFactory(Logger));
            Log.LogInfo("SIPSorcery Logging Enabled");
        }
    }

    private void Start()
    {
        if (SettingsHandler.Instance != null)
        {
            SettingsHandler.Instance.AddSetting(new UsernameSetting());
            SettingsHandler.Instance.AddSetting(new LiveUrlSetting());
            SettingsHandler.Instance.AddSetting(new DopplerSetting());
            SettingsHandler.Instance.AddSetting(new EnableDebuggingSetting());
            SettingsHandler.Instance.AddSetting(new UserIdSetting());
            SettingsHandler.Instance.AddSetting(new ResetDefaultsSetting());

            if (DebugConfig.Value)
            {
                Log.LogInfo("Registered BingBox Settings (Reordered)");
            }

            gameObject.AddComponent<BingBoxWebClient>();

            try
            {
                SteamLobbyManager = new BingBox.Network.SteamLobbyManager();
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize SteamLobbyManager: {ex.Message}");
            }
        }
        else
        {
            Log.LogError("SettingsHandler.Instance is null. Cannot register settings.");
        }
    }

    private void Update()
    {
        PerformanceLogger.Update();
    }
}

