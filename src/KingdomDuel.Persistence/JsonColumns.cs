using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KingdomDuel.Persistence;

internal static class JsonColumns
{
    public static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter(), new OrderPlanJsonConverter() },
    };

    public static ValueConverter<T, string> Converter<T>() => new(
        value => JsonSerializer.Serialize(value, Options),
        json => JsonSerializer.Deserialize<T>(json, Options)!);

    public static ValueComparer<T> Comparer<T>() => new(
        (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
        value => JsonSerializer.Serialize(value, Options).GetHashCode(),
        value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!);

    public static ValueConverter<T?, string> NullableConverter<T>() where T : class => new(
        value => JsonSerializer.Serialize(value, Options),
        json => JsonSerializer.Deserialize<T>(json, Options));

    public static ValueComparer<T?> NullableComparer<T>() where T : class => new(
        (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
        value => JsonSerializer.Serialize(value, Options).GetHashCode(),
        value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options));
}
