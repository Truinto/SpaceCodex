using Kingmaker.Localization;
using Kingmaker.Localization.Enums;
using Kingmaker.RuleSystem;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SpaceCodex
{
    public static class Localization
    {
        private static Dictionary<string, string> dictionary = new();
        private static SHA1 _SHA = SHA1.Create();
        private static StringBuilder _sb1 = new();

        public static void Init()
        {
            LocalizationManager.Instance.LocaleChanged += Instance_LocaleChanged;
        }

        public static LocalizedString CreateString(this string value, string key = null)
        {
            if (value is null or "")
                return new LocalizedString() { Key = "" };

            if (key is null)
            {
                var sha = _SHA.ComputeHash(Encoding.UTF8.GetBytes(value));
                for (int i = 0; i < sha.Length; i++)
                    _sb1.Append(sha[i].ToString("x2"));
                key = _sb1.ToString();
                _sb1.Clear();
            }

            dictionary[key] = value;
            LocalizationManager.Instance.CurrentPack.PutString(key, value);

            return new LocalizedString() { Key = key };
        }

        private static void Instance_LocaleChanged(Locale locale)
        {
            var pack = LocalizationManager.Instance.CurrentPack;
            foreach (var pair in dictionary)
                pack.PutString(pair.Key, pair.Value);
        }
    }
}