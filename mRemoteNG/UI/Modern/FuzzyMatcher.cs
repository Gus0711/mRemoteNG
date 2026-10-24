#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mRemoteNG.UI.Modern
{
    /// <summary>
    /// Small fuzzy matcher (subsequence matching with bonuses for contiguous runs, word starts and
    /// prefixes), case- and accent-insensitive. Returns the matched character positions so callers
    /// can highlight them.
    /// </summary>
    public static class FuzzyMatcher
    {
        /// <summary>
        /// Matches <paramref name="pattern"/> (spaces ignored) against <paramref name="text"/>.
        /// Returns false when not every pattern character occurs in order.
        /// </summary>
        public static bool TryMatch(string pattern, string? text, out int score, out int[] positions)
        {
            score = 0;
            positions = [];
            if (string.IsNullOrEmpty(text)) return false;

            string p = Fold(pattern.Replace(" ", string.Empty));
            if (p.Length == 0) return true;
            string t = Fold(text);
            if (p.Length > t.Length) return false;

            // 1. Contiguous substring: best case, prefer an occurrence at a word start.
            int best = -1;
            int index = t.IndexOf(p, StringComparison.Ordinal);
            while (index >= 0)
            {
                if (best < 0) best = index;
                if (IsWordStart(text, index))
                {
                    best = index;
                    break;
                }

                index = t.IndexOf(p, index + 1, StringComparison.Ordinal);
            }

            if (best >= 0)
            {
                positions = new int[p.Length];
                for (int i = 0; i < p.Length; i++) positions[i] = best + i;
                score = 1000 + p.Length * 20 - best * 2 - (t.Length - p.Length);
                if (best == 0) score += 200;
                else if (IsWordStart(text, best)) score += 100;
                return true;
            }

            // 2. Subsequence: for each pattern char, prefer the next word-start occurrence when it
            //    comes before the next pattern char could be matched otherwise; keep it greedy.
            int[] found = new int[p.Length];
            int ti = 0;
            for (int pi = 0; pi < p.Length; pi++)
            {
                int plain = t.IndexOf(p[pi], ti);
                if (plain < 0) return false;

                // Look ahead for a word-start occurrence of the same char (e.g. "sw" -> "Server Web").
                int chosen = plain;
                if (!IsWordStart(text, plain) && (pi == 0 || found[pi - 1] != plain - 1))
                {
                    for (int k = plain + 1; k < t.Length; k++)
                    {
                        if (t[k] == p[pi] && IsWordStart(text, k))
                        {
                            chosen = k;
                            break;
                        }
                    }
                }

                found[pi] = chosen;
                ti = chosen + 1;
                if (ti > t.Length && pi < p.Length - 1) return false;
            }

            // Verify the remaining characters still fit after look-ahead choices (fallback to plain greedy).
            if (!IsValid(found, t, p))
            {
                ti = 0;
                for (int pi = 0; pi < p.Length; pi++)
                {
                    int next = t.IndexOf(p[pi], ti);
                    if (next < 0) return false;
                    found[pi] = next;
                    ti = next + 1;
                }
            }

            int s = 500;
            for (int i = 0; i < found.Length; i++)
            {
                if (IsWordStart(text, found[i])) s += 30;
                if (i > 0)
                {
                    int gap = found[i] - found[i - 1] - 1;
                    s += gap == 0 ? 25 : -Math.Min(gap, 10) * 3;
                }
            }

            if (found[0] == 0) s += 50;
            s -= found[0];
            s -= (t.Length - p.Length) / 2;

            score = s;
            positions = found;
            return true;
        }

        private static bool IsValid(int[] found, string t, string p)
        {
            int last = -1;
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] <= last || found[i] >= t.Length || t[found[i]] != p[i]) return false;
                last = found[i];
            }

            return true;
        }

        private static bool IsWordStart(string text, int index)
        {
            if (index <= 0) return true;
            char previous = text[index - 1];
            char current = text[index];
            if (!char.IsLetterOrDigit(previous)) return true;
            return char.IsUpper(current) && char.IsLower(previous);
        }

        /// <summary>Lower-cases and strips diacritics while keeping a 1:1 character mapping.</summary>
        private static string Fold(string value)
        {
            StringBuilder builder = new(value.Length);
            foreach (char c in value)
            {
                char lower = char.ToLowerInvariant(c);
                if (lower < 128)
                {
                    builder.Append(lower);
                    continue;
                }

                string decomposed = lower.ToString().Normalize(NormalizationForm.FormD);
                char baseChar = lower;
                foreach (char d in decomposed)
                {
                    if (CharUnicodeInfo.GetUnicodeCategory(d) != UnicodeCategory.NonSpacingMark)
                    {
                        baseChar = d;
                        break;
                    }
                }

                builder.Append(baseChar);
            }

            return builder.ToString();
        }

        /// <summary>Convenience: positions as a set for fast lookup while drawing.</summary>
        public static HashSet<int> ToSet(int[]? positions) => positions == null ? [] : [.. positions];
    }
}
