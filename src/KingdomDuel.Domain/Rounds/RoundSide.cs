namespace KingdomDuel.Domain.Rounds;

public sealed record RoundSide(OrderPlan Order, KingdomState After);
