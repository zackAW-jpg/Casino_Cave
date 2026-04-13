

public static class GameSession
{
    public enum BootMode
    {
        NewGame,
        Continue
    }

    public static BootMode NextBoot = BootMode.NewGame;

    public const string MainMenuSceneName = "MainMenu";
    public const string GameplaySceneName = "DungeonGameplay";
}
