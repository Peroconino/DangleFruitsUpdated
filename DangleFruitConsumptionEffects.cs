using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MoreSlugcats;
using RWCustom;

namespace DangleFruitsUpdated;
public partial class DangleFruitsUpdated
{
  public static readonly Dictionary<Player, List<ActiveEffect>> activeEffects = [];
  private static Player? player;
  public static void ConsumeMagicalFruit(Player self, Room checkRoom)
  {
    if (!(checkRoom.game.session as StoryGameSession)!.saveState.deathPersistentSaveData.reinforcedKarma && activeEffects[self].Any(effect => effect.Type == EffectType.KarmaProtection))
    {
      (checkRoom.game.session as StoryGameSession)!.saveState.deathPersistentSaveData.reinforcedKarma = true;
      for (int i = 0; i < self.room.game.cameras.Length; i++)
      {
        if (self.room.game.cameras[i].followAbstractCreature == self.abstractCreature)
        {
          self.room.game.cameras[i].hud.karmaMeter.reinforceAnimation = 0;
          break;
        }
        if (ModManager.CoopAvailable)
        {
          self.room.game.cameras[i].hud.karmaMeter.reinforceAnimation = 0;
        }
      }
    }
  }
  private static void hook_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
  {
    orig(self, eu);
    player = self;
    // caso ele ainda não exista no dicionário, adicionar ele com array vazio de efeitos
    if (!activeEffects.ContainsKey(self))
      return;

    // Processa os efeitos ativos
    for (int i = activeEffects[self].Count - 1; i >= 0; i--)
    {
      var effect = activeEffects[self][i];

      if (effect.IsExpired)
      {
        if (effect.ShouldRevert)
        {
          RevertEffect(self, effect);
        }
        activeEffects[self].RemoveAt(i);
        continue;
      }

      // Aplica os efeitos
      switch (effect.Type)
      {
        case EffectType.Stun:
          self.Stun(effect.Value); // Aplica stun
          break;

        case EffectType.FoodLoss:
          if (!effect.HasBeenApplied)
          {
            if (Random.value >= 0.5f)
            {
              self.SubtractFood(effect.Value);
            }
            else
            {
              self.AddFood(effect.Value);
            }
            effect.HasBeenApplied = true; // Garante que só acontece uma vez
          }
          break;

        case EffectType.Ascend:
          //Feito usando patcher
          break;

        case EffectType.Blind:
          self.Blind(60);
          break;

        case EffectType.AddFood:
          if (!effect.HasBeenApplied)
          {
            self.AddFood(effect.Value);
            effect.HasBeenApplied = true;
          }
          break;

        case EffectType.PyroDeath:
          if (!effect.HasBeenApplied)
          {
            self.PyroDeath();
            effect.HasBeenApplied = true;
          }
          break;

        case EffectType.Deafen:
          self.Deafen(effect.Value);
          break;

        case EffectType.Die:
          self.Die();
          break;

        case EffectType.ClassMechanicsArtificer:
          //Nada aqui por enquanto a função é feita pelo patcher
          break;

        case EffectType.Crafting:
          //Nada aqui por enquanto a função é feita pelo patcher
          break;
        case EffectType.KarmaProtection:
          ConsumeMagicalFruit(self, self.room);
          break;
      }
    }
  }
  private void ApplyFruitEffect(Player player, IPlayerEdible fruit, Dictionary<IPlayerEdible, int> customFruitTypes)
  {
    if (!customFruitTypes.ContainsKey(fruit))
      return;

    if (!activeEffects.ContainsKey(player))
    {
      activeEffects.Add(player, []);
    }

    float randomChance = Random.value;

    switch ((DangleFruitCustomConstants.FruitTypeColors)customFruitTypes[fruit])
    {
      case DangleFruitCustomConstants.FruitTypeColors.Green:
        ApplyPoisonousFruitEffect(player, randomChance);
        break;

      case DangleFruitCustomConstants.FruitTypeColors.Red:
        ApplyHotFruitEffect(player, randomChance);
        break;

      case DangleFruitCustomConstants.FruitTypeColors.Golden:
        ApplyMagicalFruitEffect(player, randomChance);
        break;
    }
  }
  private void ApplyHotFruitEffect(Player player, float chance)
  {
    if (chance < 0.4f) // 40% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Deafen, 60)); //60 milisegundos de surdo
    }
    else if (chance < 0.7f) // 30% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.PyroDeath, 0.1f, 2)); // Morre explodido
    }
    else
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.ClassMechanicsArtificer, 30f)); // 30% de chance de ganhar habilidades do pyro por 30s
    }
    activeEffects[player].Add(new ActiveEffect(EffectType.AddFood, 2)); // Ganha 2 de comida
  }
  private void ApplyMagicalFruitEffect(Player player, float chance)
  {
    if (chance < 0.25f) // 25% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.AddFood, 4)); // Ganha 4 de comida
    }
    else if (chance < .5f)      // 25% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.AddFood, 4)); // Ganha 4 de comida
    }
    else if (chance < 0.75f) // 25% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.KarmaProtection));// Karma reinforce
    }
    else
    { // 25% chance
      activeEffects[player].Add(new ActiveEffect(EffectType.Die)); // Die
    }
  }
  private void ApplyPoisonousFruitEffect(Player player, float chance)
  {
    if (chance < 0.3f) // 30% chance de stun
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Crafting, 30f));// Crafting 
    }
    else if (chance < 0.5f) // 20% chance de perder comida
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.FoodLoss, 1)); // Perde 1 de comida
    }
    else
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Stun, 60));
      activeEffects[player].Add(new ActiveEffect(EffectType.FoodLoss, 1));
    }

    // 30% chance de nada acontecer
  }
  private static void RevertEffect(Player player, ActiveEffect effect)
  {
    switch (effect.Type)
    {
      case EffectType.Ascend:
        player.DeactivateAscension();
        break;
    }

    effect.HasBeenReverted = true;
  }
  public enum EffectType
  {
    Stun,
    FoodLoss,
    Poison,
    Ascend,
    Blind,
    AddFood,
    PyroDeath,
    Deafen,
    Die,
    ClassMechanicsArtificer,
    Crafting,
    KarmaProtection,
  }
  // Classe para rastrear efeitos ativos
  sealed public class ActiveEffect(EffectType type, float duration = .1f, int value = 0)
  {
    public EffectType Type { get; set; } = type;
    public float Duration { get; set; } = duration;
    public float StartTime { get; set; } = Time.time;
    public int Value { get; set; } = value;
    public bool HasBeenReverted { get; set; }
    public bool HasBeenApplied { get; set; } = false;
    public bool IsExpired => (Time.time - StartTime) >= Duration;
    public bool ShouldRevert => IsExpired && !HasBeenReverted;
  }
}