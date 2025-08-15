
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Unsheathed.Interfaces;
internal interface IDataManager
{
    void SaveAll();
    void LoadAll();
    void Save(ulong steamId);
    void Load(ulong steamId);
}
internal abstract class DataManager<T> : IDataManager where T : class, new()
{
    protected static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        IncludeFields = true,
        Converters = { new JsonStringEnumConverter() }
    };
    protected abstract string FolderName { get; }
    protected abstract Dictionary<ulong, T> DataMap { get; }
    protected virtual string FileName => typeof(T).Name + ".json";
    protected string GetPlayerPath(ulong steamId)
    {
        string folder = Path.Combine(BasePath, FolderName, steamId.ToString());
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, FileName);
    }
    public virtual void Save(ulong steamId)
    {
        if (!DataMap.TryGetValue(steamId, out T data)) return;
        string path = GetPlayerPath(steamId);
        string json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText(path, json);
    }
    public virtual void Load(ulong steamId)
    {
        string path = GetPlayerPath(steamId);
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        DataMap[steamId] = JsonSerializer.Deserialize<T>(json, _options) ?? new T();
    }
    public void SaveAll()
    {
        foreach (var steamId in DataMap.Keys)
        {
            Save(steamId);
        }
    }
    public void LoadAll()
    {
        // Optional: Directory scanning logic if you want to bulk-load all existing files
    }
    protected static string BasePath => Path.Combine(BepInEx.Paths.ConfigPath, MyPluginInfo.PLUGIN_NAME);
}

    
