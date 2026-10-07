using System;

namespace ColoringBoot.Game
{
    // 주소 쿼리 읽기 (?stage=Grape&stats) — QA · 플레이테스트용. WebGL만 주소가 있다(에디터에서는 빈 문자열)
    internal static class UrlQuery
    {
        // QA 주소 기능을 켜는 테스트 주소 (2026-10-07 — 출시 주소(앱인토스 등)에서는 받은 링크 하나로 진행이 지워지거나 해금을 건너뛰지 않게)
        private static readonly string[] _qaHosts = { "jhseawater.github.io", "localhost", "127.0.0.1" };

        // 게임이 테스트 주소(GitHub Pages · 로컬)에서 열렸는가. 주소가 없으면(에디터) false
        public static bool IsQaHost(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri)) return false;
            foreach (string host in _qaHosts)
            {
                if (string.Equals(uri.Host, host, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

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
