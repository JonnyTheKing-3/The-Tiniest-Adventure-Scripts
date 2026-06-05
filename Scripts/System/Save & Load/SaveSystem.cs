using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(GameSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"Game saved to: {SavePath}");
    }

    public static GameSaveData Load()
    {
        Debug.Log($"Loading game!");
        if (!File.Exists(SavePath))
        {
            Debug.Log("No save file found.");
            return null;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        return data;
    }

    public static bool SaveExists()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (SaveExists())
            File.Delete(SavePath);
        else
            Debug.Log("No save exists. Can't delete save.");
    }
}
