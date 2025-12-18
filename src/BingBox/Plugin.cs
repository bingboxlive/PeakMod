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

    private static ConfigEntry<string> _usernameConfig = null!;
    private static ConfigEntry<string> _liveUrlConfig = null!;
    private static ConfigEntry<string> _userIdConfig = null!;
    public static ConfigEntry<bool> DopplerConfig = null!;
    public static ConfigEntry<bool> DebugConfig = null!;
    public static bool SyncRoomWithLobby { get; set; } = true;

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
            Plugin.Log.LogInfo($"Config Saved: LiveUrl = {value}");
        }
    }

    public static string UserId => _userIdConfig.Value;

    private void Awake()
    {
        Log = new LoggerWrapper(Logger);

        DependencyLoader.Init();

        _usernameConfig = Config.Bind("General", "Username", StringUtils.GetRandomDefaultUsername(), "Your BingBox username.");
        _liveUrlConfig = Config.Bind("General", "LiveUrl", "https://bingbox.live", "The BingBox Live URL.");
        _userIdConfig = Config.Bind("General", "UserId", "", "Unique Auto-Generated User ID. Do not edit.");

        DopplerConfig = Config.Bind("Settings", "DopplerEffect", true, "Enable Doppler Effect.");
        DebugConfig = Config.Bind("Settings", "EnableDebugging", true, "Enable Debug Logging.");

        if (string.IsNullOrEmpty(_userIdConfig.Value))
        {
            _userIdConfig.Value = StringUtils.GenerateRandomId();
            Plugin.Log.LogInfo($"Generated New User ID: {_userIdConfig.Value}");
        }

        string newRoomId = RoomIdManager.GenerateNewRoomId();
        Log.LogInfo($"Generated Session Room ID: {newRoomId}");

        Log.LogInfo($"BingBox Loaded - User: {_usernameConfig.Value}, ID: {_userIdConfig.Value}, Room: {newRoomId}");

        BingBoxSettings.Initialize();
        gameObject.AddComponent<SettingsInjector>();
        gameObject.AddComponent<PauseMenuInjector>();
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

            Log.LogInfo("Registered BingBox Settings (Reordered)");

            gameObject.AddComponent<BingBoxWebClient>();
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

