namespace Gotchi.Core
{
    // Modes that exist in the code but are not part of the current release. A switch here is the only thing that
    // decides whether the player can reach them; turning one on needs no other change.
    public static class GameFeatures
    {
        // The Wild (campaign, Systems/CampaignSystem.cs + UI/CampaignPanelView.cs). Parked on 2026-09-21: it is to
        // become a whole world to explore, which is not first-release work. Not a const, so the code behind it
        // keeps compiling without "unreachable code" warnings.
        public static readonly bool Wild = false;
    }
}
