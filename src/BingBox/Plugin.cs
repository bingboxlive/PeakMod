using BepInEx;
using BepInEx.Configuration;
using BingBox.Logging;
using BingBox.Settings;

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
    public static string Username
    {
        get => _usernameConfig.Value;
        set
        {
            if (_usernameConfig.Value != value)
            {
                _usernameConfig.Value = value;
                Plugin.Log.LogInfo($"Config Saved: Username = {value}");
            }
        }
    }

    public static string LiveUrl
    {
        get => _liveUrlConfig.Value;
        set
        {
            if (_liveUrlConfig.Value != value)
            {
                _liveUrlConfig.Value = value;
                Plugin.Log.LogInfo($"Config Saved: LiveUrl = {value}");
            }
        }
    }

    public static string UserId
    {
        get => _userIdConfig.Value;
    }

    private void Awake()
    {
        _usernameConfig = Config.Bind("General", "Username", GetRandomDefaultUsername(), "Your BingBox username.");
        _liveUrlConfig = Config.Bind("General", "LiveUrl", "https://bingbox.live", "The BingBox Live URL.");
        _userIdConfig = Config.Bind("General", "UserId", "", "Unique Auto-Generated User ID. Do not edit.");

        DopplerConfig = Config.Bind("Settings", "DopplerEffect", true, "Enable Doppler Effect.");
        DebugConfig = Config.Bind("Settings", "EnableDebugging", true, "Enable Debug Logging.");
        if (string.IsNullOrEmpty(_userIdConfig.Value))
        {
            _userIdConfig.Value = GenerateRandomId();
            Plugin.Log.LogInfo($"Generated New User ID: {_userIdConfig.Value}");
        }

        string newRoomId = RoomIdManager.GenerateNewRoomId();
        Log.LogInfo($"Generated Session Room ID: {newRoomId}");

        Log.LogInfo($"BingBox Loaded - User: {_usernameConfig.Value}, ID: {_userIdConfig.Value}, Room: {newRoomId}");

        BingBoxSettings.Initialize();
        gameObject.AddComponent<SettingsInjector>();
    }

    private string GenerateRandomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        char[] stringChars = new char[5];
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }

    private string GetRandomDefaultUsername()
    {
        string[] names = new[]
        {
            "bing bong", "bong bing", "ding dong", "ping pong", "sing song", "wong tong",
            "bang bong", "bung bong", "bing bang", "king kong", "zing zong", "bling bong",
            "king bong", "bink bonk", "blang blong", "zong zing", "bonkers bings",
            "bingle dangle", "bongle bing", "bingo bongo", "pong ping", "dong ding",
            "song sing", "long strong", "gong song", "bongus", "bingerton", "bongeroni",
            "bingly", "boing boing", "boing bong", "bing boing", "bingle", "bongo",
            "dingle dong", "dongle ding", "ringle rong", "rongle ring", "pinglet",
            "pongus", "kling klong", "klong kling", "bingus", "bungo", "blingus",
            "blongo", "zingle zongle", "zongle zingle", "binkus bonkus", "bingus bongus",
            "bango bango", "bango bingo", "bigga bonga", "bim bom", "blim blom",
            "bring brong", "bip bop", "click clack", "clink clonk", "crink cronk",
            "dingo dongo", "dink donk", "fingle fangle", "flim flam", "fling flong",
            "gling glong", "hingle hangle", "jingle jangle", "jing jong", "kink konk",
            "ling long", "ming mong", "ning nong", "pink ponk", "pling plong",
            "prang prong", "quing quong", "ring rong", "shing shong", "sing songy",
            "sking skong", "sling slong", "sting stong", "swing swong", "thring throng",
            "ting tong", "tring trong", "ving vong", "bimp bomp", "wing wong",
            "wingle wangle", "ying yong", "zig zag", "zing zang", "zink zonk",
            "zip zap", "zippity zong", "zongle", "zingle", "binger bonger"
        };
        var random = new System.Random();
        return names[random.Next(names.Length)];
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

