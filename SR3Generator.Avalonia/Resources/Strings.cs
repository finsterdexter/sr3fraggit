using System.Globalization;
using System.Resources;

namespace SR3Generator.Avalonia.Resources;

/// <summary>
/// Strongly-typed accessor for <c>Strings.resx</c>. One property per key so XAML
/// can reference entries via <c>{x:Static res:Strings.XyzKey}</c>.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager _rm =
        new("SR3Generator.Avalonia.Resources.Strings", typeof(Strings).Assembly);

    private static string Get(string key) =>
        _rm.GetString(key, CultureInfo.CurrentUICulture) ?? $"[{key}]";

    // Attribute abbreviations
    public static string AbbrBody => Get(nameof(AbbrBody));
    public static string AbbrQuickness => Get(nameof(AbbrQuickness));
    public static string AbbrStrength => Get(nameof(AbbrStrength));
    public static string AbbrCharisma => Get(nameof(AbbrCharisma));
    public static string AbbrIntelligence => Get(nameof(AbbrIntelligence));
    public static string AbbrWillpower => Get(nameof(AbbrWillpower));
    public static string AbbrReaction => Get(nameof(AbbrReaction));
    public static string AbbrEssence => Get(nameof(AbbrEssence));
    public static string AbbrMagic => Get(nameof(AbbrMagic));

    // Attribute full names
    public static string AttrBody => Get(nameof(AttrBody));
    public static string AttrQuickness => Get(nameof(AttrQuickness));
    public static string AttrStrength => Get(nameof(AttrStrength));
    public static string AttrCharisma => Get(nameof(AttrCharisma));
    public static string AttrIntelligence => Get(nameof(AttrIntelligence));
    public static string AttrWillpower => Get(nameof(AttrWillpower));
    public static string AttrReaction => Get(nameof(AttrReaction));
    public static string AttrEssence => Get(nameof(AttrEssence));
    public static string AttrMagic => Get(nameof(AttrMagic));
    public static string AttrInitiative => Get(nameof(AttrInitiative));
    public static string AttrReactionFormula => Get(nameof(AttrReactionFormula));

    // Resource-bar labels
    public static string LabelRace => Get(nameof(LabelRace));
    public static string LabelAspect => Get(nameof(LabelAspect));
    public static string LabelAttributeShort => Get(nameof(LabelAttributeShort));
    public static string LabelSkillShort => Get(nameof(LabelSkillShort));
    public static string LabelSpellShort => Get(nameof(LabelSpellShort));
    public static string LabelNuyen => Get(nameof(LabelNuyen));
    public static string LabelEssence => Get(nameof(LabelEssence));
    public static string LabelBioIndex => Get(nameof(LabelBioIndex));
    public static string LabelMagic => Get(nameof(LabelMagic));
    public static string LabelSpellPoints => Get(nameof(LabelSpellPoints));
    public static string LabelSpellPointsShort => Get(nameof(LabelSpellPointsShort));
    public static string LabelStatus => Get(nameof(LabelStatus));
    public static string LabelCategory => Get(nameof(LabelCategory));
    public static string LabelCost => Get(nameof(LabelCost));
    public static string LabelActive => Get(nameof(LabelActive));
    public static string LabelKnowledge => Get(nameof(LabelKnowledge));
    public static string LabelForce => Get(nameof(LabelForce));
    public static string LabelLevel => Get(nameof(LabelLevel));
    public static string LabelGrade => Get(nameof(LabelGrade));
    public static string LabelRank => Get(nameof(LabelRank));
    public static string LabelBenefit => Get(nameof(LabelBenefit));
    public static string LabelPowerPtsRemaining => Get(nameof(LabelPowerPtsRemaining));
    public static string LabelSpent => Get(nameof(LabelSpent));
    public static string LabelFreeContacts => Get(nameof(LabelFreeContacts));
    public static string LabelSpentOnContacts => Get(nameof(LabelSpentOnContacts));
    public static string LabelAttributePoints => Get(nameof(LabelAttributePoints));

    // Section headers
    public static string SectionAvailable => Get(nameof(SectionAvailable));
    public static string SectionPurchased => Get(nameof(SectionPurchased));
    public static string SectionOwned => Get(nameof(SectionOwned));
    public static string SectionInstalled => Get(nameof(SectionInstalled));
    public static string SectionCategories => Get(nameof(SectionCategories));
    public static string SectionActions => Get(nameof(SectionActions));
    public static string SectionPhysical => Get(nameof(SectionPhysical));
    public static string SectionMental => Get(nameof(SectionMental));
    public static string SectionDerived => Get(nameof(SectionDerived));
    public static string SectionPriorityReference => Get(nameof(SectionPriorityReference));
    public static string SectionAttributeModifiers => Get(nameof(SectionAttributeModifiers));
    public static string SectionRacialAbilities => Get(nameof(SectionRacialAbilities));
    public static string SectionCapabilities => Get(nameof(SectionCapabilities));
    public static string SectionIdentity => Get(nameof(SectionIdentity));
    public static string SectionAttributes => Get(nameof(SectionAttributes));
    public static string SectionActiveSkills => Get(nameof(SectionActiveSkills));
    public static string SectionSpells => Get(nameof(SectionSpells));
    public static string SectionGear => Get(nameof(SectionGear));
    public static string SectionContacts => Get(nameof(SectionContacts));
    public static string SectionResources => Get(nameof(SectionResources));
    public static string SectionQuickView => Get(nameof(SectionQuickView));
    public static string SectionPurchase => Get(nameof(SectionPurchase));
    public static string SectionBind => Get(nameof(SectionBind));
    public static string SectionAddSpell => Get(nameof(SectionAddSpell));
    public static string SectionCyberware => Get(nameof(SectionCyberware));
    public static string SectionBioware => Get(nameof(SectionBioware));
    public static string SectionYourContacts => Get(nameof(SectionYourContacts));
    public static string SectionAddFreeContact => Get(nameof(SectionAddFreeContact));
    public static string SectionBuyContact => Get(nameof(SectionBuyContact));

    // Column headers
    public static string HeaderBase => Get(nameof(HeaderBase));
    public static string HeaderMod => Get(nameof(HeaderMod));
    public static string HeaderTotal => Get(nameof(HeaderTotal));
    public static string HeaderRace => Get(nameof(HeaderRace));
    public static string HeaderMagic => Get(nameof(HeaderMagic));
    public static string HeaderAttr => Get(nameof(HeaderAttr));
    public static string HeaderSkill => Get(nameof(HeaderSkill));
    public static string HeaderNuyen => Get(nameof(HeaderNuyen));
    public static string HeaderBenefit => Get(nameof(HeaderBenefit));
    public static string HeaderCategory => Get(nameof(HeaderCategory));

    // Buttons
    public static string ButtonAdd => Get(nameof(ButtonAdd));
    public static string ButtonRemove => Get(nameof(ButtonRemove));
    public static string ButtonRemoveSelected => Get(nameof(ButtonRemoveSelected));
    public static string ButtonBuy => Get(nameof(ButtonBuy));
    public static string ButtonSell => Get(nameof(ButtonSell));
    public static string ButtonInstallCyberware => Get(nameof(ButtonInstallCyberware));
    public static string ButtonInstallBioware => Get(nameof(ButtonInstallBioware));
    public static string ButtonUp => Get(nameof(ButtonUp));
    public static string ButtonClear => Get(nameof(ButtonClear));
    public static string ButtonBindKarma => Get(nameof(ButtonBindKarma));
    public static string ButtonBindSpellPoints => Get(nameof(ButtonBindSpellPoints));
    public static string ButtonAddFree => Get(nameof(ButtonAddFree));
    public static string ButtonBuildCharacter => Get(nameof(ButtonBuildCharacter));
    public static string ButtonAddPower => Get(nameof(ButtonAddPower));

    // Watermarks
    public static string WatermarkSearch => Get(nameof(WatermarkSearch));
    public static string WatermarkSearchSkills => Get(nameof(WatermarkSearchSkills));
    public static string WatermarkSearchPowers => Get(nameof(WatermarkSearchPowers));
    public static string WatermarkSearchFoci => Get(nameof(WatermarkSearchFoci));
    public static string WatermarkSearchSpells => Get(nameof(WatermarkSearchSpells));
    public static string WatermarkContactName => Get(nameof(WatermarkContactName));

    // Checkboxes
    public static string CheckStreetIndex => Get(nameof(CheckStreetIndex));
    public static string CheckExclusive => Get(nameof(CheckExclusive));
    public static string CheckFetishLimited => Get(nameof(CheckFetishLimited));

    // Tabs
    public static string TabPriorities => Get(nameof(TabPriorities));
    public static string TabRace => Get(nameof(TabRace));
    public static string TabMagic => Get(nameof(TabMagic));
    public static string TabAttributes => Get(nameof(TabAttributes));
    public static string TabSkills => Get(nameof(TabSkills));
    public static string TabSpells => Get(nameof(TabSpells));
    public static string TabAdept => Get(nameof(TabAdept));
    public static string TabFoci => Get(nameof(TabFoci));
    public static string TabGear => Get(nameof(TabGear));
    public static string TabAugments => Get(nameof(TabAugments));
    public static string TabContacts => Get(nameof(TabContacts));
    public static string TabSummary => Get(nameof(TabSummary));

    // Page titles + subtitles
    public static string PrioritiesTitle => Get(nameof(PrioritiesTitle));
    public static string PrioritiesSubtitle => Get(nameof(PrioritiesSubtitle));

    public static string RaceTitle => Get(nameof(RaceTitle));
    public static string RaceSubtitle => Get(nameof(RaceSubtitle));

    public static string MagicTitle => Get(nameof(MagicTitle));
    public static string MagicSubtitle => Get(nameof(MagicSubtitle));
    public static string MagicMundaneWarning => Get(nameof(MagicMundaneWarning));

    public static string AttributesTitle => Get(nameof(AttributesTitle));
    public static string AttributesSubtitle => Get(nameof(AttributesSubtitle));

    public static string SkillsTitle => Get(nameof(SkillsTitle));
    public static string SkillsSubtitle => Get(nameof(SkillsSubtitle));

    public static string SpellsTitle => Get(nameof(SpellsTitle));
    public static string SpellsSubtitle => Get(nameof(SpellsSubtitle));
    public static string SpellsNoSorceryWarning => Get(nameof(SpellsNoSorceryWarning));
    public static string SpellsBuyPointButton => Get(nameof(SpellsBuyPointButton));
    public static string SpellsAddButton => Get(nameof(SpellsAddButton));
    public static string SpellsTypeTooltip => Get(nameof(SpellsTypeTooltip));

    public static string AdeptPowersTitle => Get(nameof(AdeptPowersTitle));
    public static string AdeptPowersSubtitle => Get(nameof(AdeptPowersSubtitle));
    public static string AdeptPowersLevelHint => Get(nameof(AdeptPowersLevelHint));

    public static string FociTitle => Get(nameof(FociTitle));
    public static string FociSubtitle => Get(nameof(FociSubtitle));
    public static string FociTypesAll => Get(nameof(FociTypesAll));

    public static string GearTitle => Get(nameof(GearTitle));
    public static string GearSubtitle => Get(nameof(GearSubtitle));

    public static string AugmentationsTitle => Get(nameof(AugmentationsTitle));
    public static string AugmentationsSubtitle => Get(nameof(AugmentationsSubtitle));

    public static string ContactsTitle => Get(nameof(ContactsTitle));
    public static string ContactsSubtitle => Get(nameof(ContactsSubtitle));
    public static string ContactsFreeOnly => Get(nameof(ContactsFreeOnly));
    public static string ContactsTierLvl1 => Get(nameof(ContactsTierLvl1));
    public static string ContactsTierLvl2 => Get(nameof(ContactsTierLvl2));
    public static string ContactsTierLvl3 => Get(nameof(ContactsTierLvl3));
    public static string ContactsTierContact => Get(nameof(ContactsTierContact));
    public static string ContactsTierBuddy => Get(nameof(ContactsTierBuddy));
    public static string ContactsTierFriendForLife => Get(nameof(ContactsTierFriendForLife));
    public static string ContactsCostLvl1 => Get(nameof(ContactsCostLvl1));
    public static string ContactsCostLvl2 => Get(nameof(ContactsCostLvl2));
    public static string ContactsCostLvl3 => Get(nameof(ContactsCostLvl3));

    public static string SummaryTitle => Get(nameof(SummaryTitle));
    public static string SummarySubtitle => Get(nameof(SummarySubtitle));
    public static string SummaryRemainingNuyen => Get(nameof(SummaryRemainingNuyen));
    public static string SummaryReadyToFinalize => Get(nameof(SummaryReadyToFinalize));
    public static string SummaryEmptyValue => Get(nameof(SummaryEmptyValue));

    // Priority reference table
    public static string PriorityRaceA => Get(nameof(PriorityRaceA));
    public static string PriorityRaceB => Get(nameof(PriorityRaceB));
    public static string PriorityRaceC => Get(nameof(PriorityRaceC));
    public static string PriorityRaceD => Get(nameof(PriorityRaceD));
    public static string PriorityRaceE => Get(nameof(PriorityRaceE));
    public static string PriorityMagicA => Get(nameof(PriorityMagicA));
    public static string PriorityMagicB => Get(nameof(PriorityMagicB));
    public static string PriorityMagicMundane => Get(nameof(PriorityMagicMundane));

    // Tooltips
    public static string TooltipRaisePriority => Get(nameof(TooltipRaisePriority));
    public static string TooltipLowerPriority => Get(nameof(TooltipLowerPriority));
    public static string TooltipBindKarma => Get(nameof(TooltipBindKarma));
    public static string TooltipBindSpellPoints => Get(nameof(TooltipBindSpellPoints));

    // Skills tab badges
    public static string BadgeSpec => Get(nameof(BadgeSpec));
    public static string BadgeFree => Get(nameof(BadgeFree));

    // Magic subtabs
    public static string MagicSubtabOverview => Get(nameof(MagicSubtabOverview));
    public static string MagicSubtabSpells => Get(nameof(MagicSubtabSpells));
    public static string MagicSubtabAdept => Get(nameof(MagicSubtabAdept));
    public static string MagicSubtabSpirits => Get(nameof(MagicSubtabSpirits));
    public static string MagicSubtabFoci => Get(nameof(MagicSubtabFoci));

    // Magic overview pickers
    public static string SectionTradition => Get(nameof(SectionTradition));
    public static string SectionTotem => Get(nameof(SectionTotem));
    public static string SectionElement => Get(nameof(SectionElement));
    public static string LabelTradition => Get(nameof(LabelTradition));
    public static string LabelTotem => Get(nameof(LabelTotem));
    public static string LabelElement => Get(nameof(LabelElement));
    public static string LabelEnvironment => Get(nameof(LabelEnvironment));
    public static string LabelAdvantages => Get(nameof(LabelAdvantages));
    public static string LabelDisadvantages => Get(nameof(LabelDisadvantages));
    public static string TraditionHermetic => Get(nameof(TraditionHermetic));
    public static string TraditionShamanic => Get(nameof(TraditionShamanic));
    public static string ElementEarth => Get(nameof(ElementEarth));
    public static string ElementAir => Get(nameof(ElementAir));
    public static string ElementFire => Get(nameof(ElementFire));
    public static string ElementWater => Get(nameof(ElementWater));
    public static string HintPickTradition => Get(nameof(HintPickTradition));
    public static string HintPickTotem => Get(nameof(HintPickTotem));
    public static string HintPickElement => Get(nameof(HintPickElement));
    public static string WatermarkSearchTotems => Get(nameof(WatermarkSearchTotems));

    // Spirits tab
    public static string SpiritsTitle => Get(nameof(SpiritsTitle));
    public static string SpiritsSubtitle => Get(nameof(SpiritsSubtitle));
    public static string LabelSpirits => Get(nameof(LabelSpirits));
    public static string LabelSpiritType => Get(nameof(LabelSpiritType));
    public static string LabelServices => Get(nameof(LabelServices));
    public static string SectionAddSpirit => Get(nameof(SectionAddSpirit));
    public static string SpiritsAddButton => Get(nameof(SpiritsAddButton));
    public static string WatermarkSpiritName => Get(nameof(WatermarkSpiritName));
    public static string SpiritsNoConjuringWarning => Get(nameof(SpiritsNoConjuringWarning));
}
