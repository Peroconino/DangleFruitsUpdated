using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace DangleFruitsUpdated;
public partial class DangleFruitsUpdated
{
  private const string FoodTypeTag = "FOODTYPE<svB>";
  private const string EndingTag = "<svA>";
  public readonly Dictionary<EntityID, FruitFileData> fruitsSaveData = [];
  private void hook_SaveState_LoadGame(On.SaveState.orig_LoadGame orig, SaveState self, string saveString, RainWorldGame game)
  {
    // Carrega os dados do jogo
    orig(self, saveString, game);

    // Procura por todas as ocorrências de FOODTYPE no arquivo
    int currentIndex = 0;
    while (true)
    {
      int startIndex = saveString.IndexOf(FoodTypeTag, currentIndex);
      if (startIndex == -1) break;

      int endIndex = saveString.IndexOf(EndingTag, startIndex);
      if (endIndex == -1) break;

      string foodTypeSection = saveString.Substring(
          startIndex + FoodTypeTag.Length,
          endIndex - (startIndex + FoodTypeTag.Length)
      );

      string[] fruitsData = foodTypeSection.Split(["<svC>"], StringSplitOptions.RemoveEmptyEntries);

      foreach (string fruitData in fruitsData)
      {
        if (string.IsNullOrEmpty(fruitData)) continue;

        string[] data = fruitData.Split(',');
        if (data.Length != 3) continue;

        EntityID fruitID = EntityID.FromString(data[0]);
        if (
            bool.TryParse(data[1], out bool isShelter) &&
            int.TryParse(data[2], out int fruitType) &&
            !fruitsSaveData.ContainsKey(fruitID)
        )
        {
          fruitsSaveData.Add(fruitID, new FruitFileData(fruitID, isShelter, fruitType));
          Logger.LogInfo($"Loaded fruit:: ID:{fruitID} - isShelter:{isShelter} - type:{fruitType}");
        }
      }

      currentIndex = endIndex + EndingTag.Length;
    }
  }
  private string hook_SaveState_SaveToString(On.SaveState.orig_SaveToString orig, SaveState self)
  {
    string text = orig(self);
    if (customFruitTypes.Count > 0)
    {
      text += FoodTypeTag;
      var sb = new StringBuilder(text);

      foreach (var playerEdible in customFruitTypes)
      {
        DangleFruit? fruit = playerEdible.Key as DangleFruit;
        if (fruit?.AbstrConsumable.ID is EntityID fruitID && FindFruitSaveData(fruitID) is null)
        {// apenas salva se não existir no dicionário, evita duplicatas
          bool isShelter = fruit.AbstrConsumable.Room.shelter;
          int foodType = customFruitTypes[fruit];
          if (isShelter)
          {
            // Salva no formato: ID,isShelter,foodType
            _ = sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "{0},{1},{2}<svC>",
                fruitID,
                isShelter,
                foodType
            );
            Logger.LogInfo($"Saved fruit:: ID:{fruitID} - isShelter:{isShelter} - type:{foodType}");
          }
        }
      }

      sb.Append(EndingTag);
      text = sb.ToString();
    }

    return text;
  }
  private void CleanData()
  {
    string saveDir = Application.persistentDataPath;

    try
    {
      // Procura por todos os arquivos de save possíveis
      string[] savePatterns = ["sav*", "exp*"];

      foreach (string pattern in savePatterns)
      {
        string[] files = Directory.GetFiles(saveDir, pattern, SearchOption.TopDirectoryOnly);

        foreach (string filePath in files)
        {
          try
          {
            string content = File.ReadAllText(filePath);

            // Se encontrar nossos dados no arquivo
            if (content.Contains(FoodTypeTag))
            {
              Logger.LogInfo($"Cleaning mod data from file: {Path.GetFileName(filePath)}");

              // Limpa os dados e salva
              string cleanedContent = CleanModData(content);
              File.WriteAllText(filePath, cleanedContent);
            }
          }
          catch (Exception ex)
          {
            Logger.LogError($"Error processing file {filePath}: {ex.Message}");
            // Continua para o próximo arquivo mesmo se este falhar
          }
        }
      }
    }
    catch (Exception ex)
    {
      Logger.LogError($"Critical error during save cleanup: {ex.Message}");
    }
  }
  // Método para limpar dados do mod do arquivo de save
  private static string CleanModData(string saveData)
  {
    while (true)
    {
      int startIndex = saveData.IndexOf(FoodTypeTag);
      if (startIndex == -1) break;

      int endIndex = saveData.IndexOf(EndingTag, startIndex);
      if (endIndex == -1) break;

      // Remove a seção completa incluindo os marcadores
      saveData = saveData.Remove(startIndex, endIndex + EndingTag.Length - startIndex);
    }
    return saveData;
  }
  // Método auxiliar para verificar se o arquivo é um save válido
  private static bool IsValidSaveFile(string fileName)
  {
    // Verifica se o nome do arquivo segue os padrões do jogo
    return fileName.Equals("sav", StringComparison.OrdinalIgnoreCase) ||
           (fileName.StartsWith("sav", StringComparison.OrdinalIgnoreCase) &&
            fileName.Length > 3 && char.IsDigit(fileName[3])) ||
           fileName.StartsWith("exp", StringComparison.OrdinalIgnoreCase) &&
           fileName.Length > 3 && char.IsDigit(fileName[3]);
  }
  private FruitFileData? FindFruitSaveData(EntityID fruitID)
  {
    if (fruitsSaveData.TryGetValue(fruitID, out FruitFileData? fruitData))
    {
      return fruitData;
    }

    return null;
  }
  sealed public class FruitFileData(EntityID fruitID, bool isShelter, int foodType)
  {
    public EntityID fruitID = fruitID;
    public bool isShelter = isShelter;
    public int foodType = foodType;
  }
}