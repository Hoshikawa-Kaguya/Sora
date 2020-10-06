using System;
using Newtonsoft.Json;

namespace Sora.Converter
{
    internal class StringConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override bool CanWrite => true;

        public override bool CanConvert(Type objectType) => true;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
