using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;
using BepInEx;
using System.Collections.Generic;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DangleFruitsUpdated;

[BepInPlugin(GUID, Name, Version)]
public partial class DangleFruitsUpdated : BaseUnityPlugin
{
    private readonly DangleFruitsUpdatedOptions Options;
    public const string GUID = "Peroconino.DangleFruitsUpdated";
    public const string Version = "1.0.0";
    public const string Name = "Fruits Updated";
    private bool IsInit;
    //NOTE: Necessário dois dicionarios para salvar e recuperar os dados das frutas em arquivo
    public readonly Dictionary<EntityID, int> customFruitIDs = [];
    public readonly Dictionary<IPlayerEdible, int> customFruitTypes = [];
    public DangleFruitsUpdated()
    {
        try
        {
            Options = new DangleFruitsUpdatedOptions(this, Logger);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (IsInit) return;

        try
        {
            IsInit = true;
            //Para cada fruta guardamos um valor customizado no dicionario
            On.DangleFruit.ctor += hook_DangleFruit_ctor;
            // Quando a fruta for usada
            On.Player.ObjectEaten += hook_ObjectEaten;
            // Muda a cor da fruta dependendo do valor de comida
            On.DangleFruit.ApplyPalette += hook_ApplyPalette;
            // Aplica efeitos customizados ao consumir a fruta
            On.Player.Update += hook_Player_Update;
            MachineConnector.SetRegisteredOI(GUID, Options);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
        }
    }
    public void hook_DangleFruit_ctor(On.DangleFruit.orig_ctor orig, DangleFruit self, AbstractPhysicalObject abstractPhysicalObject)
    {
        orig(self, abstractPhysicalObject);
        EntityID fruitID = self.AbstrConsumable.ID;
        // Verifica se a fruta já tem um valor no dicionario
        if (customFruitIDs.ContainsKey(fruitID))
        {
            customFruitTypes[self] = customFruitIDs[fruitID];
            return;
        }
        // Cada fruta pode ter um valor aleatório diferente
        float randomChance = UnityEngine.Random.Range(0f, 1f);
        int fruitType;
        if (randomChance <= DangleFruitCustomConstants.ExtremelyRare)
        {
            fruitType = (int)DangleFruitCustomConstants.FruitTypeColors.Golden;
        }
        else if (randomChance <= DangleFruitCustomConstants.VeryRare)
        {
            fruitType = (int)DangleFruitCustomConstants.FruitTypeColors.Red;
        }
        else if (randomChance <= DangleFruitCustomConstants.Rare)
        {
            fruitType = (int)DangleFruitCustomConstants.FruitTypeColors.Green;
        }
        else
        {
            fruitType = (int)DangleFruitCustomConstants.FruitTypeColors.Blue;
        }


        customFruitIDs[fruitID] = fruitType;
        customFruitTypes[self] = fruitType;

        if (ModManager.DevTools)
            Logger.LogInfo($"Fruit type: {fruitType} created with chance: {randomChance}");
    }
    public void hook_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible fruit)
    {
        // Verifica se existe um valor customizado para esta fruta específica
        if (customFruitTypes.ContainsKey(fruit))
        {
            int foodType = customFruitTypes[fruit];
            if (self.graphicsModule is not null)
            {
                (self.graphicsModule as PlayerGraphics)!.LookAtNothing();
            }

            if (ModManager.MSC && SlugcatStats.NourishmentOfObjectEaten(self.SlugCatClass, fruit) == -1)
            {
                self.Stun(60);
                return;
            }

            self.AI?.AteFood(fruit as PhysicalObject);
            if (self.room.game.IsStorySession)
            {
                int i;
                for (i = SlugcatStats.NourishmentOfObjectEaten(self.SlugCatClass, fruit); i >= 4; i -= 4)
                {
                    self.AddFood(0);
                }
                while (i > 0)
                {
                    self.AddQuarterFood();
                    i--;
                }
            }
            else
            {
                self.AddFood(0);
            }

            if ((ModManager.MSC || ModManager.CoopAvailable) && self.slugOnBack != null)
            {
                self.slugOnBack.interactionLocked = true;
            }

            if (self.spearOnBack != null)
            {
                self.spearOnBack.interactionLocked = true;
            }

            // NOTE: Limpar o dicionário quando a fruta for consumida
            if (ModManager.DevTools)
                Logger.LogInfo($"Fruit {fruit} of type {foodType} being removed from dictionaries");

            // Aplica efeitos baseados no tipo da fruta
            ApplyFruitEffect(self, fruit, customFruitTypes);
            DeleteFruitID(FindFruitID(foodType));
            customFruitTypes.Remove(fruit);


            return;
        }

        orig(self, fruit);
    }
    public void hook_ApplyPalette(On.DangleFruit.orig_ApplyPalette orig, DangleFruit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
    {
        if (customFruitTypes.ContainsKey(self))
        {
            int foodPoints = customFruitTypes[self];
            sLeaser.sprites[0].color = palette.blackColor;
            if (ModManager.MSC && rCam.room.game.session is StoryGameSession && rCam.room.world.name == "HR")
            {
                self.color = Color.Lerp(RainWorld.SaturatedGold, palette.blackColor, self.darkness);
                return;
            }

            //Dependendo do valor de comida, a cor da fruta muda
            self.color = foodPoints switch
            {
                4 => Color.Lerp(RainWorld.SaturatedGold, palette.blackColor, self.darkness),//dourado
                3 => Color.Lerp(new Color(1f, 0f, 0f), palette.blackColor, self.darkness),//vermelho
                2 => Color.Lerp(new Color(0f, 1f, 0f), palette.blackColor, self.darkness),//verde
                _ => Color.Lerp(new Color(0f, 0f, 1f), palette.blackColor, self.darkness),//azul
            };

            return;
        }
        orig(self, sLeaser, rCam, palette);
    }
    private EntityID? FindFruitID(int type)
    {
        foreach (var pair in customFruitIDs)
        {
            if (pair.Value == type)
            {
                return pair.Key;
            }
        }
        return null;
    }
    private bool DeleteFruitID(EntityID? fruitID)
    {

        if (fruitID is not null && customFruitIDs.ContainsKey((EntityID)fruitID))
        {
            customFruitIDs.Remove((EntityID)fruitID);
            return true;
        }
        return false;
    }
}
