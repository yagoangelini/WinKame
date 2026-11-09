// WinKame, a morandi.cloud product. Copyright (C) 2026 Thales de Oliveira Morandi.
// SPDX-License-Identifier: GPL-3.0-or-later. See LICENSE and NOTICE.

namespace WinKame.Core;

/// <summary>Reads the recipients of a "mailto:" link (RFC 6068), for the "copy the address" email handler.</summary>
public static class MailtoLink
{
    /// <summary>
    /// "mailto:ana@x.com,bia@y.com?cc=c@z.com&amp;subject=Oi" -> ["ana@x.com", "bia@y.com"].
    /// Addresses in a "to" field count too; cc and bcc don't (they aren't the destination).
    /// </summary>
    public static IReadOnlyList<string> Recipients(string? link)
    {
        var s = (link ?? "").Trim().Trim('"');
        if (!s.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) return [];
        s = s["mailto:".Length..];
        var q = s.IndexOf('?');
        var path = q < 0 ? s : s[..q];
        var query = q < 0 ? "" : s[(q + 1)..];

        var result = new List<string>();
        void Add(string part)
        {
            foreach (var raw in Uri.UnescapeDataString(part.Replace('+', ' ')).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var a = raw.Trim();
                if (a.Length > 0 && !result.Contains(a, StringComparer.OrdinalIgnoreCase)) result.Add(a);
            }
        }
        // In the path "+" is a real character (ana+news@x.com), so it is only unescaped, never turned into a space.
        foreach (var raw in Uri.UnescapeDataString(path).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
        {
            var a = raw.Trim();
            if (a.Length > 0 && !result.Contains(a, StringComparer.OrdinalIgnoreCase)) result.Add(a);
        }
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = pair.IndexOf('=');
            if (eq > 0 && pair[..eq].Equals("to", StringComparison.OrdinalIgnoreCase)) Add(pair[(eq + 1)..]);
        }
        return result;
    }
}
