using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Lector/escritor de JSON pequeño (Firebase REST devuelve y recibe JSON).
/// Objetos → Dictionary&lt;string, object&gt;, listas → List&lt;object&gt;, números → double/long, true/false, null.
/// </summary>
public static class SugaruJson
{
    // ================================================================== LEER
    public static object Parse(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        int i = 0;
        object v = ParseValue(json, ref i);
        return v;
    }

    private static void Skip(string s, ref int i)
    {
        while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
    }

    private static object ParseValue(string s, ref int i)
    {
        Skip(s, ref i);
        if (i >= s.Length) return null;
        char c = s[i];
        if (c == '{') return ParseObject(s, ref i);
        if (c == '[') return ParseArray(s, ref i);
        if (c == '"') return ParseString(s, ref i);
        if (c == 't' && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
        if (c == 'f' && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
        if (c == 'n' && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
        return ParseNumber(s, ref i);
    }

    private static Dictionary<string, object> ParseObject(string s, ref int i)
    {
        var d = new Dictionary<string, object>();
        i++; // {
        Skip(s, ref i);
        if (i < s.Length && s[i] == '}') { i++; return d; }
        while (i < s.Length)
        {
            Skip(s, ref i);
            string key = ParseString(s, ref i);
            Skip(s, ref i);
            if (i < s.Length && s[i] == ':') i++;
            d[key] = ParseValue(s, ref i);
            Skip(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == '}') { i++; break; }
            break;
        }
        return d;
    }

    private static List<object> ParseArray(string s, ref int i)
    {
        var l = new List<object>();
        i++; // [
        Skip(s, ref i);
        if (i < s.Length && s[i] == ']') { i++; return l; }
        while (i < s.Length)
        {
            l.Add(ParseValue(s, ref i));
            Skip(s, ref i);
            if (i < s.Length && s[i] == ',') { i++; continue; }
            if (i < s.Length && s[i] == ']') { i++; break; }
            break;
        }
        return l;
    }

    private static string ParseString(string s, ref int i)
    {
        var sb = new StringBuilder();
        if (i < s.Length && s[i] == '"') i++;
        while (i < s.Length)
        {
            char c = s[i++];
            if (c == '"') break;
            if (c == '\\' && i < s.Length)
            {
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 <= s.Length)
                        {
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    private static object ParseNumber(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
        string n = s.Substring(start, i - start);
        if (n.IndexOf('.') < 0 && n.IndexOf('e') < 0 && n.IndexOf('E') < 0 &&
            long.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l)) return l;
        double.TryParse(n, NumberStyles.Float, CultureInfo.InvariantCulture, out double d);
        return d;
    }

    // ================================================================== ESCRIBIR
    public static string Serialize(object value)
    {
        var sb = new StringBuilder();
        Write(sb, value);
        return sb.ToString();
    }

    private static void Write(StringBuilder sb, object v)
    {
        switch (v)
        {
            case null: sb.Append("null"); break;
            case string s: WriteString(sb, s); break;
            case bool b: sb.Append(b ? "true" : "false"); break;
            case IDictionary dict:
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, e.Key.ToString());
                    sb.Append(':');
                    Write(sb, e.Value);
                }
                sb.Append('}');
                break;
            }
            case IList list:
            {
                sb.Append('[');
                for (int k = 0; k < list.Count; k++)
                {
                    if (k > 0) sb.Append(',');
                    Write(sb, list[k]);
                }
                sb.Append(']');
                break;
            }
            case float f: sb.Append(f.ToString("R", CultureInfo.InvariantCulture)); break;
            case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
            case IFormattable num: sb.Append(num.ToString(null, CultureInfo.InvariantCulture)); break;
            default: WriteString(sb, v.ToString()); break;
        }
    }

    private static void WriteString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                    else sb.Append(c);
                    break;
            }
        }
        sb.Append('"');
    }
}
