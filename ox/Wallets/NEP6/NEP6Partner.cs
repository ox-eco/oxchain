using OX.IO.Json;
using System;

namespace OX.Wallets.NEP6
{
    public class NEP6Partner
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Remark { get; set; }
        public NEP6Partner(string address, string name, string mobile, string remark)
        {
            this.Address = address;
            this.Name = name;
            this.Mobile = mobile;
            this.Remark = remark;
        }
        public static NEP6Partner FromJson(JObject json)
        {
            return new NEP6Partner(json["address"].AsString(), json["name"]?.AsString(), json["mobile"]?.AsString(), json["remark"]?.AsString());
        }
        public JObject ToJson()
        {
            JObject account = new JObject();
            account["address"] = this.Address;
            account["name"] = this.Name;
            account["mobile"] = this.Mobile;
            account["remark"] = this.Remark;
            return account;
        }
    }
}
