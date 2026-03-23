using System.Diagnostics.Contracts;

namespace GDMirage.Server.Features.Game.Stats;

public static class VitalCalculator
{
    [Pure]
    public static int CalculateMaxHealth(int stamina, int level)
    {
        return 50 + stamina * (2 + level / 3) + level * level / 4;
    }

    [Pure]
    public static int CalculateMaxMana(int intelligence, int level)
    {
        return 50 + intelligence * (2 + level / 3) + level * level / 4;
    }
}
