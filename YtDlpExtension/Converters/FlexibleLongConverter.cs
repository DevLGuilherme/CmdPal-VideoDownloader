using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YtDlpExtension.Converters
{
    public class FlexibleLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // Tenta ler como int
                if (reader.TryGetInt64(out long i))
                    return i;
                // Se não for int, tenta como double e converte para int
                if (reader.TryGetDouble(out double d))
                    return (long)d; // Trunca, se quiser arredondar use (int)Math.Round(d)
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                if (long.TryParse(s, out long result))
                    return result;
                if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                    return (long)d;
                return null;
            }

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            reader.Skip();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
