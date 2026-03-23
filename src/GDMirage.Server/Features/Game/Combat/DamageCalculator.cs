namespace GDMirage.Server.Features.Game.Combat;

public static class DamageCalculator
{
    private static readonly Random Random = new();

    public static int CalculateMeleeDamage(int strength)
    {
        var variance = Random.NextDouble() * 0.3 - 0.15;
        var damage = (int)(strength * (1 + variance));

        return Math.Max(1, damage);
    }
}
