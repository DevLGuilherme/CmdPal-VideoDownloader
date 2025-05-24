using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YtDlpExtension.Converters
{
    public class FlexibleIntConverter : JsonConverter<int?>
    {
        public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                // Tenta ler como int
                if (reader.TryGetInt32(out int i))
                    return i;
                // Se não for int, tenta como double e converte para int
                if (reader.TryGetDouble(out double d))
                    return (int)d; // Trunca, se quiser arredondar use (int)Math.Round(d)
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    return null;
                if (int.TryParse(s, out int result))
                    return result;
                if (double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
                    return (int)d;
                return null;
            }

            if (reader.TokenType == JsonTokenType.Null)
                return null;

            reader.Skip();
            return null;
        }

        public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteNumberValue(value.Value);
            else
                writer.WriteNullValue();
        }
    }
}
