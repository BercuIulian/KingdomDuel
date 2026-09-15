using System.Text.Json;
using System.Text.Json.Serialization;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Persistence;

internal sealed class OrderPlanJsonConverter : JsonConverter<OrderPlan>
{
    private const string TroopsProperty = "Troops";
    private const string FeastProperty = "Feast";
    private const string PurchasesProperty = "Purchases";

    public override OrderPlan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var troops = new Dictionary<ActionType, int>();

        if (Find(root, TroopsProperty) is { ValueKind: JsonValueKind.Object } allocation)
        {
            foreach (var entry in allocation.EnumerateObject())
            {
                if (Enum.TryParse<ActionType>(entry.Name, ignoreCase: true, out var direction))
                {
                    troops[direction] = entry.Value.GetInt32();
                }
            }
        }

        FeastTier? feast = null;

        if (Find(root, FeastProperty) is { ValueKind: JsonValueKind.String } tier
            && Enum.TryParse<FeastTier>(tier.GetString(), ignoreCase: true, out var parsed))
        {
            feast = parsed;
        }

        var purchases = new List<PurchaseType>();

        if (Find(root, PurchasesProperty) is { ValueKind: JsonValueKind.Array } bought)
        {
            foreach (var entry in bought.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String
                    && Enum.TryParse<PurchaseType>(entry.GetString(), ignoreCase: true, out var purchase))
                {
                    purchases.Add(purchase);
                }
            }
        }

        return new OrderPlan(troops, feast, purchases);
    }

    public override void Write(Utf8JsonWriter writer, OrderPlan value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteStartObject(TroopsProperty);

        foreach (var (direction, count) in value.Troops)
        {
            writer.WriteNumber(direction.ToString(), count);
        }

        writer.WriteEndObject();

        if (value.Feast is { } tier)
        {
            writer.WriteString(FeastProperty, tier.ToString());
        }

        if (value.Purchases.Count > 0)
        {
            writer.WriteStartArray(PurchasesProperty);

            foreach (var purchase in value.Purchases)
            {
                writer.WriteStringValue(purchase.ToString());
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    private static JsonElement? Find(JsonElement root, string name)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }
}
