namespace QuietPls.Storage;

public interface ISettingsStore
{
    UserSettings Load();
    void Save(UserSettings settings);
}
