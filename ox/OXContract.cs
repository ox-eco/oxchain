using OX.IO.Json;
using OX.SmartContract;
using System.Linq;

namespace OX
{
    public class OXContract : Contract
    {
        public string[] ParameterNames;
        public bool Deployed;

        public static OXContract FromJson(JObject json)
        {
            if (json == null) return null;
            return new OXContract
            {
                Script = json["script"].AsString().HexToBytes(),
                ParameterList = ((JArray)json["parameters"]).Select(p => p["type"].AsEnum<ContractParameterType>()).ToArray(),
                ParameterNames = ((JArray)json["parameters"]).Select(p => p["name"].AsString()).ToArray(),
                Deployed = json["deployed"].AsBoolean()
            };
        }

        public JObject ToJson()
        {
            JObject contract = new JObject();
            contract["script"] = Script.ToHexString();
            contract["parameters"] = new JArray(ParameterList.Zip(ParameterNames, (type, name) =>
            {
                JObject parameter = new JObject();
                parameter["name"] = name;
                parameter["type"] = type;
                return parameter;
            }));
            contract["deployed"] = Deployed;
            return contract;
        }
    }
}
