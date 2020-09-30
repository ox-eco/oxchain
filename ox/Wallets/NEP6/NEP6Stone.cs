using OX.IO.Json;

namespace OX.Wallets.NEP6
{
    public class NEP6Stone
    {
        public string Key { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public NEP6Stone(string key, string type, string value)
        {
            this.Key = key;
            this.Type = type;
            this.Value = value;
        }
        public static NEP6Stone FromJson(JObject json)
        {
            return new NEP6Stone(json["key"].AsString(), json["type"]?.AsString(), json["value"]?.AsString());
        }
        public JObject ToJson()
        {
            JObject account = new JObject();
            account["key"] = this.Key;
            account["type"] = this.Type;
            account["value"] = this.Value;
            return account;
        }
    }
}
