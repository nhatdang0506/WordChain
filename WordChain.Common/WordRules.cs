
using System.Globalization;
using System.Text;

namespace WordChain.Common;

public static class WordRules
{
    // Chuẩn hóa chuỗi đầu vào: cắt khoảng trắng, chuyển về chữ thường,
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        text = text.Trim().ToLowerInvariant();
        var d = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        foreach (var ch in d)
        {
            var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (cat != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    // Lấy "từ cuối" của một câu sau khi đã Normalize.
    public static string? LastWordNormalized(string phrase)
    {
        var s = Normalize(phrase);
        if (string.IsNullOrWhiteSpace(s)) return null;
        var parts = s.Split(new char[]{' ', '\t', '\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        for (int i = parts.Length - 1; i >= 0; i--)
        {
            var token = FilterLettersDigitDash(parts[i]);
            if (!string.IsNullOrWhiteSpace(token)) return token;
        }
        return null;
    }

    // Lọc 1 token, chỉ giữ lại chữ, số, '-' và '_'.
    // Dùng bên trong LastWordNormalized để loại bỏ các ký tự linh tinh khỏi từ cuối.
    private static string FilterLettersDigitDash(string token)
    {
        var sb = new StringBuilder(token.Length);
        foreach (var ch in token)
        {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
        }
        return sb.ToString();
    }

    // Kiểm tra xem câu hiện tại (sau Normalize) có BẮT ĐẦU bằng từ "required" hay không.
    public static bool StartsWithWordNormalized(string current, string required)
    {
        var s = Normalize(current);
        if (string.IsNullOrEmpty(required)) return false;
        if (s == required) return true;
        if (s.StartsWith(required + " ")) return true;
        if (s.StartsWith(required + "-")) return true;
        if (s.StartsWith(required + "_")) return true;
        return false;
    }
    // Hàm kiểm tra 1 lượt nối chữ có hợp lệ hay không.
    public static bool IsValidChainByWord(string? previous, string current, out string? nextRequired)
    {
        nextRequired = LastWordNormalized(current);
        if (string.IsNullOrWhiteSpace(current)) return false;
        if (string.IsNullOrWhiteSpace(previous)) return true;
        var required = LastWordNormalized(previous!);
        if (required is null) return false;
        return StartsWithWordNormalized(current, required);
    }
}
