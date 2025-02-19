using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using System.Collections.Generic;
using HarmonyLib;

#pragma warning disable CS0618

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace DangleFruitsUpdated;

[BepInPlugin(GUID, Name, Version)]
public partial class DangleFruitsUpdated : BaseUnityPlugin
{
    public const string GUID = "Peroconino.DangleFruitsUpdated";
    public const string Version = "1.0.0";
    public const string Name = "Fruits Updated";
    private bool IsInit;
    private readonly DangleFruitsUpdatedOptions Options;
    private CustomLogger CustomLogger;
    public readonly Dictionary<IPlayerEdible, int> customFruitTypes = [];
    public DangleFruitsUpdated()
    {
        try
        {
            Options = new DangleFruitsUpdatedOptions(this, CustomLogger!);
        }
        catch (Exception ex)
        {
            CustomLogger!.LogError(ex);
        }
    }
    private void OnEnable()
    {
        On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
    }
    private void OnDisable()
    {
        CleanData();
    }
    private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);
        if (IsInit) return;

        try
        {
            CustomLogger = new CustomLogger(Logger);
            var harmony = new Harmony(GUID);
            //Para cada fruta guardamos um valor customizado no dicionario
            On.DangleFruit.ctor += hook_DangleFruit_ctor;
            // Quando a fruta for usada
            On.Player.ObjectEaten += hook_Player_ObjectEaten;
            On.Player.ctor += hook_Player_ctor;
            // Muda a cor da fruta dependendo do tipo
            On.DangleFruit.ApplyPalette += hook_DangleFruit_ApplyPalette;
            // Aplica efeitos customizados ao consumir a fruta
            On.Player.Update += hook_Player_Update;
            // Salvar e carregar dados das frutas
            On.SaveState.LoadGame += hook_SaveState_LoadGame;
            On.SaveState.SaveToString += hook_SaveState_SaveToString;
            // pegar rwgame para salvar e carregar
            MachineConnector.SetRegisteredOI(GUID, Options);
            harmony.PatchAll();
            IsInit = true;
        }
        catch (Exception ex)
        {
            CustomLogger.LogError(ex);
        }
    }
    public void hook_DangleFruit_ctor(On.DangleFruit.orig_ctor orig, DangleFruit self, AbstractPhysicalObject abstractPhysicalObject)
    {
        orig(self, abstractPhysicalObject);


        EntityID fruitID = self.AbstrConsumable.ID;
        // Verifica se a fruta já tem um valor no dicionario
        if (fruitsSaveData.ContainsKey(fruitID))
        {
            customFruitTypes[self] = fruitsSaveData[fruitID].foodType;
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
            //fruta normal não precisa ser inserida no dicionário
            return;
        }

        customFruitTypes[self] = fruitType;

        CustomLogger.LogInfo($"Fruit type: {fruitType} created with chance: {randomChance}");
    }
    public void hook_Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible fruit)
    {
        // Verifica se existe um valor customizado para esta fruta específica
        if (customFruitTypes.ContainsKey(fruit) && !self.isNPC)
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

            // Limpar o dicionário quando a fruta for consumida
            CustomLogger.LogInfo($"Fruit {fruit} of type {foodType} being removed from dictionaries");

            // Aplica efeitos baseados no tipo da fruta
            ApplyFruitEffect(self, fruit, customFruitTypes);
            customFruitTypes.Remove(fruit);

            return;
        }

        orig(self, fruit);
    }
}
