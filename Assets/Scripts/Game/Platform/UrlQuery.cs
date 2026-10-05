using System;

namespace ColoringBoot.Game
{
    // 주소 쿼리 읽기 (?stage=Grape&stats) — QA · 플레이테스트용. WebGL만 주소가 있다(에디터에서는 빈 문자열)
    internal static class UrlQuery
    {
        // key가 있으면 true. value는 '=' 뒤(없으면 빈 문자열)
        public static bool TryGet(string url, string key, out string value)
        {
            value = null;
            int start = url.IndexOf('?');
            if (start < 0) return false;
            string query = url.Substring(start + 1);
            int hash = query.IndexOf('#');
            if (hash >= 0) query = query.Substring(0, hash);
            foreach (string part in query.Split('&'))
            {
                int equals = part.IndexOf('=');
                string name = equals < 0 ? part : part.Substring(0, equals);
                if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase)) continue;
                value = equals < 0 ? "" : Uri.UnescapeDataString(part.Substring(equals + 1));
                return true;
            }
            return false;
        }
    }
}
