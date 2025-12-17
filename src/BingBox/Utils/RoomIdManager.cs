using System;

namespace BingBox.Utils;

public static class RoomIdManager
{
    public static string CurrentRoomId { get; private set; } = "?????";

    public static string GenerateNewRoomId()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        char[] stringChars = new char[5];
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        CurrentRoomId = new string(stringChars);
        return CurrentRoomId;
    }
}
