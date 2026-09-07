using System.Text.Json;
using System.Text.Json.Serialization;
using KingdomDuel.Domain.Enums;
using KingdomDuel.Domain.Rounds;

namespace KingdomDuel.Persistence;

internal sealed class OrderPlanJsonConverter : JsonConverter<OrderPlan>
{
    public override OrderPlan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = JsonSerializer.Deserialize<Dictionary<string, int>>(ref reader, options);

        if (raw is null)
        {
            return OrderPlan.Empty;
        }

        var troops = new Dictionary<ActionType, int>();

        foreach (var (name, count) in raw)
        {
            if (Enum.TryParse<ActionType>(name, ignoreCase: true, out var direction))
            {
                troops[direction] = count;
            }
        }

        return new OrderPlan(troops);
    }

    public override void Write(Utf8JsonWriter writer, OrderPlan value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (direction, count) in value.Troops)
        {
            writer.WriteNumber(direction.ToString(), count);
        }

        writer.WriteEndObject();
    }
}
