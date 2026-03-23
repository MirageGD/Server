namespace GDMirage.Server.Features.Game.Stats;

public static class ExperienceCalculator
{
    public static int CalculateExperienceForLevel(int level)
    {
        return (int)(level * ((1 + (level - 1) * 0.5) * 100));
    }
}
