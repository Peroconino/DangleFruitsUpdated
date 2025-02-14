using System;
using System.Globalization;
using System.IO;
using RWCustom;

namespace DangleFruitsUpdated;
public partial class DangleFruitsUpdated
{
  private void hook_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string saveString, RainWorldGame game)
  {
    // Carrega os dados do jogo
    orig(self, saveString, game);

    LoadSavedFruitData(saveString);
  }
  private string hook_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self)
  {
    string text = orig(self);

    return SaveToString(text);
  }
  private void LoadSavedFruitData(string saveString)
  {
    if (saveString.Contains("FOODTYPE<svB>"))
    {
      string[] parts = saveString.Split(["FOODTYPE<svB>"], StringSplitOptions.None);
      string[] fruitsData = parts[1].Split(["<svA>"], StringSplitOptions.None)[0].Split(["<svC>"], StringSplitOptions.None);

      customFruitIDs.Clear();
      customFruitTypes.Clear();
      foreach (string fruitData in fruitsData)
      {
        if (string.IsNullOrEmpty(fruitData)) continue;

        string[] data = fruitData.Split(',');

        EntityID fruitID = EntityID.FromString(data[0]);
        int fruitType = int.Parse(data[1]);

        customFruitIDs[fruitID] = fruitType;
      }
    }
  }
  private string SaveToString(string saveString)
  {
    // Adiciona uma seção própria para seus dados
    if (customFruitTypes.Count > 0)
    {
      saveString += "FOODTYPE<svB>";
      var sb = new System.Text.StringBuilder(saveString);
      foreach (var pair in customFruitIDs)
      {
        // Salva no formato: ID,foodType
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0},{1}<svC>", pair.Key, pair.Value);
      }
      sb.Append("<svA>");
      saveString = sb.ToString();
    }

    return saveString;
  }
  private void SaveToFile()
  {
    try
    {
      string savePath = Path.Combine(Custom.RootFolderDirectory(), "DangleFruitsUpdated", "CustomFruitSave.txt");
      string directory = Path.GetDirectoryName(savePath);

      if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);

      File.WriteAllText(savePath, SerializeFruitData());
    }
    catch (System.Exception e)
    {
      Debug.LogError($"Erro ao salvar arquivo: {e.Message}");
    }
  }
}