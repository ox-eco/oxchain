using OX.Compiler.MSIL;
using System;
using System.Collections.Generic;
using System.Text;

namespace OX.Compiler
{
    public class OXModule
    {
        public string Title;
        public string Description;
        public string Version;
        public string Author;
        public string Email;
        public bool HasStorage;
        public bool HasDynamicInvoke;
        public bool IsPayable;

        public OXModule(ILogger logger)
        {
        }

        //小蚁没类型，只有方法
        public SortedDictionary<int, OXCode> total_Codes = new SortedDictionary<int, OXCode>();
        public byte[] Build()
        {
            List<byte> bytes = new List<byte>();
            foreach (var c in this.total_Codes.Values)
            {
                bytes.Add((byte)c.code);
                if (c.bytes != null)
                    for (var i = 0; i < c.bytes.Length; i++)
                    {
                        bytes.Add(c.bytes[i]);
                    }
            }
            return bytes.ToArray();
            //将body链接，生成this.code       byte[]
            //并计算 this.codehash            byte[]
        }
        public string mainMethod;
        public ConvOption option;
        public Dictionary<string, OXMethod> mapMethods = new Dictionary<string, OXMethod>();
        public Dictionary<string, OXEvent> mapEvents = new Dictionary<string, OXEvent>();
        //public Dictionary<string, byte[]> codes = new Dictionary<string, byte[]>();
        //public byte[] GetScript(byte[] script_hash)
        //{
        //    string strhash = "";
        //    foreach (var b in script_hash)
        //    {
        //        strhash += b.ToString("X02");
        //    }
        //    return codes[strhash];
        //}
        public string GenJson()
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            json["__name__"] = new MyJson.JsonNode_ValueString("oxmodule.");

            //code
            var jsoncode = new MyJson.JsonNode_Array();
            json["code"] = jsoncode;
            foreach (var c in this.total_Codes.Values)
            {
                jsoncode.Add(c.GenJson());
            }
            //code bytes
            var code = this.Build();
            var codestr = "";
            foreach (var c in code)
            {
                codestr += c.ToString("X02");
            }
            json.SetDictValue("codebin", codestr);

            //calls
            MyJson.JsonNode_Object methodinfo = new MyJson.JsonNode_Object();
            json["call"] = methodinfo;
            foreach (var m in this.mapMethods)
            {
                methodinfo[m.Key] = m.Value.GenJson();
            }


            StringBuilder sb = new StringBuilder();
            json.ConvertToStringWithFormat(sb, 4);
            return sb.ToString();
        }
        public void FromJson(string json)
        {

        }

        public Dictionary<string, object> staticfields = new Dictionary<string, object>();
    }

    public class OXMethod
    {
        public string _namespace;
        public string name;
        public string displayName;
        public List<OXParam> paramtypes = new List<OXParam>();
        public string returntype;
        public bool isPublic = true;
        public bool inSmartContract;
        //临时变量
        public List<OXParam> body_Variables = new List<OXParam>();

        //临时记录在此，会合并到一起
        public SortedDictionary<int, OXCode> body_Codes = new SortedDictionary<int, OXCode>();
        public int funcaddr;
        public MyJson.JsonNode_Object GenJson()
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            json.SetDictValue("name", this.name);
            json.SetDictValue("returntype", this.returntype);
            json.SetDictValue("paramcount", this.paramtypes.Count);
            MyJson.JsonNode_Array jsonparams = new MyJson.JsonNode_Array();
            json.SetDictValue("params", jsonparams);
            for (var i = 0; i < this.paramtypes.Count; i++)
            {
                MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
                item.SetDictValue("name", this.paramtypes[i].name);
                item.SetDictValue("type", this.paramtypes[i].type);
                jsonparams.Add(item);
            }
            return json;
        }

        public void FromJson(MyJson.JsonNode_Object json)
        {
        }

        //public byte[] Build()
        //{
        //    List<byte> bytes = new List<byte>();
        //    foreach (var c in this.body_Codes.Values)
        //    {
        //        bytes.Add((byte)c.code);
        //        if (c.bytes != null)
        //            for (var i = 0; i < c.bytes.Length; i++)
        //            {
        //                bytes.Add(c.bytes[i]);
        //            }
        //    }
        //    return bytes.ToArray();
        //    //将body链接，生成this.code       byte[]
        //    //并计算 this.codehash            byte[]
        //}
        public string lastsfieldname = null;//最后一个加载的静态成员的名字，仅event使用

        public int lastparam = -1;//最后一个加载的参数对应
        public int lastCast = -1;
    }
    public class OXEvent
    {
        public string _namespace;
        public string name;
        public string displayName;
        public List<OXParam> paramtypes = new List<OXParam>();

        public OXEvent(ILField value)
        {
            _namespace = value.field.DeclaringType.FullName;
            name = value.field.DeclaringType.FullName + "::" + value.field.Name;
            displayName = value.displayName;
            paramtypes = value.paramtypes;

            if (value.returntype != "System.Void")
            {
                throw new NotSupportedException($"NEP-3 does not support return types for events. Expected: `System.Void`, Detected: `{value.returntype}`");
            }
        }
    }

    public class OXCode
    {
        public OX.VM.OpCode code = OX.VM.OpCode.NOP;
        public int addr;
        public byte[] bytes;
        public string debugcode;
        public int debugline = -1;
        public int debugILAddr = -1;
        public string debugILCode;
        public bool needfix = false;//lateparse tag
        public bool needfixfunc = false;
        public int srcaddr;
        public int[] srcaddrswitch;
        public string srcfunc;
        public Mono.Cecil.Cil.SequencePoint sequencePoint;

        public override string ToString()
        {
            //string info = "AL_" + addr.ToString("X04") + " " + code.ToString();
            //if (bytes != null)
            //    info += " len=" + bytes.Length;
            //if (debugcode != null && debugline >= 0)
            //{
            //    info += "    " + debugcode + "(" + debugline + ")";
            //}

            string info = "" + addr.ToString("X04") + " " + code.ToString();
            for (var j = 0; j < 16 - code.ToString().Length; j++)
            {
                info += " ";
            }
            info += "[";
            if (bytes != null)
            {
                foreach (var c in bytes)
                {
                    info += c.ToString("X02");
                }
            }
            info += "]";

            if (debugcode != null && debugline >= 0)
            {
                info += "//" + debugcode + "(" + debugline + ")";
            }
            return info;
        }
        public MyJson.JsonNode_ValueString GenJson()
        {
            string info = "" + addr.ToString("X04") + " " + code.ToString();
            for (var j = 0; j < 16 - code.ToString().Length; j++)
            {
                info += " ";
            }
            info += "[";
            if (bytes != null)
            {
                foreach (var c in bytes)
                {
                    info += c.ToString("X02");
                }
            }
            info += "]";

            if (debugILCode != null && debugILAddr >= 0)
            {
                info += "<IL_" + debugILAddr.ToString("X04") + " " + debugILCode + ">";
            }
            if (debugcode != null && debugline >= 0)
            {
                info += "//" + debugcode + "(" + debugline + ")";
            }
            return new MyJson.JsonNode_ValueString(info);
        }

        public void FromJson(MyJson.JsonNode_Object json)
        {
        }
    }

    public class OXParam
    {
        public OXParam(string name, string type)
        {
            this.name = name;
            this.type = type;
        }
        public string name
        {
            get;
            private set;
        }
        public string type
        {
            get;
            private set;
        }
        public override string ToString()
        {
            return type + " " + name;
        }
    }
}
