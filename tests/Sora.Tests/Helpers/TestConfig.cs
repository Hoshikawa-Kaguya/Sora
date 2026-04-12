namespace Sora.Tests.Helpers;

/// <summary>
///     Reads test configuration from environment variables.
///     Supports dual-bot architecture: Primary Bot (main test executor) and Secondary Bot (interaction simulator).
///     All values except ports have no hardcoded defaults — functional tests skip when required ENV vars are absent.
/// </summary>
public static class TestConfig
{
    // ---- Milky Primary ----

    /// <summary>Milky primary host. Read from SORA_TEST_MILKY_PRIMARY_HOST.</summary>
    public static string MilkyPrimaryHost => Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PRIMARY_HOST") ?? "";

    /// <summary>Milky secondary host. Read from SORA_TEST_MILKY_SECONDARY_HOST.</summary>
    public static string MilkySecondaryHost => Environment.GetEnvironmentVariable("SORA_TEST_MILKY_SECONDARY_HOST") ?? "";

    /// <summary>Milky port (shared by both bots). Read from SORA_TEST_MILKY_PORT (default 3010).</summary>
    public static int MilkyPort => int.TryParse(Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PORT"), out int p) ? p : 3010;

    /// <summary>Milky API prefix (shared by both bots). Read from SORA_TEST_MILKY_PREFIX.</summary>
    public static string MilkyPrefix => Environment.GetEnvironmentVariable("SORA_TEST_MILKY_PREFIX") ?? "";

    /// <summary>Milky access token (shared by both bots). Read from SORA_TEST_MILKY_TOKEN.</summary>
    public static string MilkyToken => Environment.GetEnvironmentVariable("SORA_TEST_MILKY_TOKEN") ?? "";

    // ---- OneBot11 Primary ----

    /// <summary>OneBot11 primary host. Read from SORA_TEST_OB11_PRIMARY_HOST.</summary>
    public static string Ob11PrimaryHost => Environment.GetEnvironmentVariable("SORA_TEST_OB11_PRIMARY_HOST") ?? "";

    /// <summary>OneBot11 secondary host. Read from SORA_TEST_OB11_SECONDARY_HOST.</summary>
    public static string Ob11SecondaryHost => Environment.GetEnvironmentVariable("SORA_TEST_OB11_SECONDARY_HOST") ?? "";

    /// <summary>OneBot11 port (shared by both bots). Read from SORA_TEST_OB11_PORT (default 3001).</summary>
    public static int Ob11Port => int.TryParse(Environment.GetEnvironmentVariable("SORA_TEST_OB11_PORT"), out int p) ? p : 3001;

    /// <summary>OneBot11 access token (shared by both bots). Read from SORA_TEST_OB11_TOKEN.</summary>
    public static string Ob11Token => Environment.GetEnvironmentVariable("SORA_TEST_OB11_TOKEN") ?? "";

    // ---- Avatars & Media ----

    /// <summary>Path to primary bot avatar image. Read from SORA_TEST_PRIMARY_BOT_AVATAR.</summary>
    public static string PrimaryBotAvatar => Environment.GetEnvironmentVariable("SORA_TEST_PRIMARY_BOT_AVATAR") ?? "";

    /// <summary>Path to secondary bot avatar image. Read from SORA_TEST_SECONDARY_BOT_AVATAR.</summary>
    public static string SecondaryBotAvatar => Environment.GetEnvironmentVariable("SORA_TEST_SECONDARY_BOT_AVATAR") ?? "";

    /// <summary>Path to group avatar image for SetGroupAvatar test. Read from SORA_TEST_GROUP_AVATAR.</summary>
    public static string GroupAvatarPath => Environment.GetEnvironmentVariable("SORA_TEST_GROUP_AVATAR") ?? "";

    /// <summary>Path to audio file for SendReceive_Audio test. Read from SORA_TEST_AUDIO_FILE.</summary>
    public static string AudioFilePath => Environment.GetEnvironmentVariable("SORA_TEST_AUDIO_FILE") ?? "";

    /// <summary>Path to video file for SendReceive_Video test. Read from SORA_TEST_VIDEO_FILE.</summary>
    public static string VideoFilePath => Environment.GetEnvironmentVariable("SORA_TEST_VIDEO_FILE") ?? "";

    // ---- Configuration checks ----

    /// <summary>Whether Milky primary bot is configured.</summary>
    public static bool IsMilkyConfigured => !string.IsNullOrEmpty(MilkyPrimaryHost);

    /// <summary>Whether Milky dual-bot (primary + secondary) is configured.</summary>
    public static bool IsMilkyDualBotConfigured => IsMilkyConfigured && !string.IsNullOrEmpty(MilkySecondaryHost);

    /// <summary>Whether OB11 primary bot is configured.</summary>
    public static bool IsOb11Configured => !string.IsNullOrEmpty(Ob11PrimaryHost);

    /// <summary>Whether OB11 dual-bot (primary + secondary) is configured.</summary>
    public static bool IsOb11DualBotConfigured => IsOb11Configured && !string.IsNullOrEmpty(Ob11SecondaryHost);

    // ---- Shared test targets ----

    /// <summary>
    ///     Group ID for functional tests. Read from SORA_TEST_GROUP_ID.
    ///     Returns 0 if not set — functional tests skip when this is 0.
    /// </summary>
    public static long TestGroupId => long.TryParse(Environment.GetEnvironmentVariable("SORA_TEST_GROUP_ID"), out long g) ? g : 0L;

    /// <summary>
    ///     Test results directory path. Read from SORA_TEST_RESULTS_DIR.
    ///     Set by Run-Tests.ps1 to the actual TestResults directory.
    /// </summary>
    public static string ResultsDirectory => Environment.GetEnvironmentVariable("SORA_TEST_RESULTS_DIR") ?? "";

    // ---- Skip reasons ----

    /// <summary>
    ///     Returns a skip reason if functional tests should be skipped, or null if they can run.
    ///     Skips when: SORA_TEST_FUNCTIONAL != "true", or SORA_TEST_GROUP_ID is not set.
    /// </summary>
    public static string? SkipFunctionalReason
    {
        get
        {
            if (Environment.GetEnvironmentVariable("SORA_TEST_FUNCTIONAL") != "true")
                return "Functional tests disabled. Set SORA_TEST_FUNCTIONAL=true to enable.";
            if (TestGroupId == 0)
                return "SORA_TEST_GROUP_ID not set. Functional tests require a test group.";
            return null;
        }
    }

    /// <summary>Skip reason for Milky functional tests (primary bot).</summary>
    public static string? SkipMilkyReason
    {
        get
        {
            string? baseReason = SkipFunctionalReason;
            if (baseReason is not null) return baseReason;
            if (!IsMilkyConfigured) return "SORA_TEST_MILKY_PRIMARY_HOST not set.";
            return null;
        }
    }

    /// <summary>Skip reason for Milky dual-bot tests (requires both primary and secondary).</summary>
    public static string? SkipMilkyDualBotReason
    {
        get
        {
            string? baseReason = SkipMilkyReason;
            if (baseReason is not null) return baseReason;
            if (!IsMilkyDualBotConfigured) return "SORA_TEST_MILKY_SECONDARY_HOST not set. Dual-bot tests require both bots.";
            return null;
        }
    }

    /// <summary>Skip reason for OB11 functional tests (primary bot).</summary>
    public static string? SkipOb11Reason
    {
        get
        {
            string? baseReason = SkipFunctionalReason;
            if (baseReason is not null) return baseReason;
            if (!IsOb11Configured) return "SORA_TEST_OB11_PRIMARY_HOST not set.";
            return null;
        }
    }

    /// <summary>Skip reason for OB11 dual-bot tests (requires both primary and secondary).</summary>
    public static string? SkipOb11DualBotReason
    {
        get
        {
            string? baseReason = SkipOb11Reason;
            if (baseReason is not null) return baseReason;
            if (!IsOb11DualBotConfigured) return "SORA_TEST_OB11_SECONDARY_HOST not set. Dual-bot tests require both bots.";
            return null;
        }
    }
}