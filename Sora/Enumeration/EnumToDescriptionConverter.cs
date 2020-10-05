using System;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;

namespace Sora.Enumeration
{
    internal class EnumToDescriptionConverter : JsonConverter
    {
        //反序列化时不执行
        public override bool CanRead => false;
        //序列化时执行
        public override bool CanWrite => true;

        //控制执行条件（当属性的值为枚举类型时才使用转换器）
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Enum);
        }

        /// <summary>
        /// 序列化时执行的转换
        /// 获取枚举的描述值
        /// </summary>
        /// <param name="writer">可以用来重写值</param>
        /// <param name="value">属性的原值</param>
        /// <param name="serializer">serializer对象</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (string.IsNullOrEmpty(value.ToString()))
            {
                writer.WriteValue("");
                return;
            }
            FieldInfo fieldInfo = value.GetType().GetField(value.ToString()!);
            if (fieldInfo == null)
            {
                writer.WriteValue("");
                return;
            }
            DescriptionAttribute[] attributes =
                (DescriptionAttribute[]) fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
            writer.WriteValue(attributes.Length > 0 ? attributes[0].Description : "");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
