using UnityEngine;

namespace ColoringBoot.Game
{
    // PlayerPrefs 저장소. WebGL은 PlayerPrefs.Save()를 불러야 브라우저 저장소(IndexedDB)에 남으므로 쓸 때마다 부른다
    public sealed class PlayerPrefsStore : IKeyValueStore
    {
        public string Load(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;

        public void Save(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
