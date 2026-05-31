namespace SR3Generator.Creation
{
    /// <summary>
    /// Derived, situational cyberzombie modifiers (Man &amp; Machine pp. 50–58). Informational —
    /// only the Willpower penalty and Magic=0 actually mutate attributes (applied in
    /// <see cref="CharacterBuilder"/>); everything here is display-only TN/dice guidance shown on
    /// the Summary tab. Produced by <see cref="CharacterBuilder.GetCybermancyStats"/>.
    /// </summary>
    /// <param name="Essence">Signed (negative) current Essence.</param>
    /// <param name="MagicResistanceTnMod">+ceil(|Ess|) to the TN of magic targeting the character.</param>
    /// <param name="SocialCharismaPenalty">+3 plus the abs Essence Charisma-linked modifier, portion beyond 2 doubled.</param>
    /// <param name="IntimidationInterrogationBonus">Same magnitude as the social penalty, usable as a bonus.</param>
    /// <param name="SurpriseReactionBonus">+1 per point or fraction of |Ess| when surprised.</param>
    /// <param name="PerceptionBonus">+1 per 2 points of negative Essence "or part thereof" (rounds up).</param>
    /// <param name="WillpowerPenalty">Net Willpower drop actually applied to the attribute.</param>
    /// <param name="CybermancySurvivalTn">Cybersurgery/Cybermancy Survival Table TN for the total Essence cost.</param>
    /// <param name="AutoInjectorUpkeepYen">3,000¥ × |Ess| per 10-day supply.</param>
    public sealed record CybermancyStats(
        decimal Essence,
        int MagicResistanceTnMod,
        int SocialCharismaPenalty,
        int IntimidationInterrogationBonus,
        int SurpriseReactionBonus,
        int PerceptionBonus,
        int WillpowerPenalty,
        int CybermancySurvivalTn,
        int AutoInjectorUpkeepYen);
}
