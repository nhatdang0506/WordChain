using System.Text.Json;

namespace WordChain.Common.Messaging;

// Tập hợp các hằng "type" dùng trong JSON message
public static class MessageTypes
{
    public const string Join = "Join";
    public const string SubmitWord = "SubmitWord";
    public const string Start = "Start";
    public const string Joined = "Joined";
    public const string GameState = "GameState";
    public const string WordResult = "WordResult";
    public const string Info = "Info";
    public const string Error = "Error";
    public const string Players = "Players";
    public const string GameEnd = "GameEnd";
}

#region Client->Server
public record class JoinMessage { public string Type { get; init; } = MessageTypes.Join; public string Name { get; init; } = string.Empty; }
public record class SubmitWordMessage { public string Type { get; init; } = MessageTypes.SubmitWord; public string Word { get; init; } = string.Empty; }
public record class StartMessage { public string Type { get; init; } = MessageTypes.Start; }
#endregion

#region Server->Client
public record class JoinedMessage { public string Type { get; init; } = MessageTypes.Joined; public bool Ok { get; init; } public string Message { get; init; } = ""; public string AssignedName { get; init; } = ""; public List<string> Players { get; init; } = new(); }
public record class GameStateMessage { 
    public string Type { get; init; } = MessageTypes.GameState; 
    public string? LastWord { get; init; } 
    public string? NextRequired { get; init; } 
    public string Turn { get; init; } = ""; 
    public List<string> Players { get; init; } = new(); 
    public HashSet<string> UsedWords { get; init; } = new(); 
    public Dictionary<string,int> Lives { get; init; } = new();
    public int TurnRemainingSeconds { get; init; }
}
public record class WordResultMessage { 
    public string Type { get; init; } = MessageTypes.WordResult; 
    public bool Ok { get; init; } 
    public string Message { get; init; } = ""; 
    public string Player { get; init; } = ""; 
    public string Word { get; init; } = ""; 
    public string? NextRequired { get; init; } 
    public string NextTurn { get; init; } = ""; 
    public int TurnRemainingSeconds { get; init; } 
}
public record class InfoMessage { public string Type { get; init; } = MessageTypes.Info; public string Message { get; init; } = ""; }
public record class ErrorMessage { public string Type { get; init; } = MessageTypes.Error; public string Message { get; init; } = ""; }
public record class PlayersMessage { public string Type { get; init; } = MessageTypes.Players; public List<string> Players { get; init; } = new(); }
public record class GameEndMessage { public string Type { get; init; } = MessageTypes.GameEnd; public string Winner { get; init; } = ""; public string Message { get; init; } = ""; }
#endregion

//peektype
// Đọc nhanh chuỗi JSON và trả về giá trị trường "type" mà không cần deserialize full object.
// Dùng để switch-case chọn đúng kiểu message (Join, SubmitWord, GameState, ...).
public static class MessageSerializer
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);
    public static string Serialize<T>(T obj) => JsonSerializer.Serialize(obj, _opts);
    public static string? PeekType(string jsonUtf16) { using var doc = JsonDocument.Parse(jsonUtf16); return doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() : null; }
    public static T Deserialize<T>(string jsonUtf16) => JsonSerializer.Deserialize<T>(jsonUtf16, _opts)!;
}
