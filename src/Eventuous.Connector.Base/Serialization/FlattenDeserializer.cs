using System.Text.Json;

namespace Eventuous.Connector.Base.Serialization;

public static class FlattenDeserializer {
    public static IDictionary<string, object> Deserialize(string json)
        => ParseJsonElement("", JsonDocument.Parse(json).RootElement) ?? new Dictionary<string, object>();

    static Dictionary<string, object>? ParseJsonElement(string key, JsonElement jsonElement) {
        return jsonElement.ValueKind switch {
            JsonValueKind.Undefined => null,
            JsonValueKind.Object    => Object(),
            JsonValueKind.Array     => Array(),
            JsonValueKind.String    => Single(jsonElement.GetString()!),
            JsonValueKind.Number    => Single(jsonElement.GetDouble()),
            JsonValueKind.True      => Single(jsonElement.GetBoolean()),
            JsonValueKind.False     => Single(jsonElement.GetBoolean()),
            JsonValueKind.Null      => null,
            _                       => throw new ArgumentOutOfRangeException(nameof(jsonElement))
        };

        Dictionary<string, object> Single(object val) => new() { { key, val } };

        Dictionary<string, object> Object() {
            var enumerator = jsonElement.EnumerateObject();
            var dictionary = new Dictionary<string, object>();

            foreach (var v in enumerator.Select(prop => ParseJsonElement(prop.Name, prop.Value))) {
                ProcessDict(dictionary, v);
            }

            return dictionary;
        }

        Dictionary<string, object> Array() {
            var dictionary = new Dictionary<string, object>();
            var enumerator = jsonElement.EnumerateArray();
            var index      = 0;

            foreach (var v in enumerator.Select(element => ParseJsonElement($"{index++}", element))) {
                ProcessDict(dictionary, v);
            }

            return dictionary;
        }

        void ProcessDict(IDictionary<string, object> target, Dictionary<string, object>? source) {
            if (source == null) return;

            foreach (var vKey in source.Keys) {
                var k = string.IsNullOrEmpty(key) ? vKey : $"{key}.{vKey}";
                target[k] = source[vKey];
            }
        }
    }
}
