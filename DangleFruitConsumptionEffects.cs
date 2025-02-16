using UnityEngine;
using System.Collections.Generic;

namespace DangleFruitsUpdated;
public partial class DangleFruitsUpdated
{
  public readonly Dictionary<Player, List<ActiveEffect>> activeEffects = [];
  private void hook_Player_Update(On.Player.orig_Update orig, Player self, bool eu)
  {
    orig(self, eu);

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
          self.Stun(60); // Aplica stun
          break;

        case EffectType.FoodLoss:
          if (!effect.HasBeenApplied)
          {
            if (Random.value >= 0.5f)
            {
              self.SubtractFood((int)effect.Value);
            }
            else
            {
              self.AddFood((int)effect.Value);
            }
            effect.HasBeenApplied = true; // Garante que só acontece uma vez
          }
          break;
        case EffectType.Ascend:
          if (!effect.HasBeenApplied)
          {
            self.ActivateAscension();
            effect.HasBeenApplied = true; // Garante que só acontece uma vez
          }
          self.ClassMechanicsSaint();
          break;
        case EffectType.Blind:
          self.Blind(60);
          break;
        case EffectType.AddFood:
          if (!effect.HasBeenApplied)
          {
            self.AddFood((int)effect.Value);
            effect.HasBeenApplied = true; // Garante que só acontece uma vez
          }
          break;
        case EffectType.PyroDeath:
          self.PyroDeath();
          break;
        case EffectType.Deafen:
          self.Deafen((int)effect.Value);
          break;
        case EffectType.Die:
          self.Die();
          break;
        case EffectType.ClassMechanicsArtificer:
          self.ClassMechanicsArtificer();
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
      activeEffects[player].Add(new ActiveEffect(EffectType.Deafen, 1, 60f)); //60 segundos de surdo
    }
    else if (chance < 0.7f) // 30% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.PyroDeath, 0.1f, 2f)); // Morre explodido
    }
    else
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.ClassMechanicsArtificer, 30f, 0)); // Ganha habilidades do pyro por 30s
    }
    activeEffects[player].Add(new ActiveEffect(EffectType.AddFood, 0.1f, 2f)); // Ganha 2 de comida
  }
  private void ApplyMagicalFruitEffect(Player player, float chance)
  {
    if (chance < 0.5f) // 50% chance
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Ascend, 3f, 0)); // 3s de ascenção?
    }
    else if (chance < 0.75f) // 25% chance de ganhar comida
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.AddFood, 0.1f, 4f)); // Ganha 4 de comida
    }
    else
    { // 25% chance de morrer
      activeEffects[player].Add(new ActiveEffect(EffectType.Die, 0.1f, 0)); // Ganha 1 de comida
    }
  }
  private void ApplyPoisonousFruitEffect(Player player, float chance)
  {
    if (chance < 0.3f) // 30% chance de stun
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Stun, 0.06f, 0f)); // 60ms de stun
    }
    else if (chance < 0.5f) // 20% chance de perder comida
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.FoodLoss, 0.1f, 1f)); // Perde 1 de comida
    }
    else if (chance < 0.7f) // 20% chance de ambos
    {
      activeEffects[player].Add(new ActiveEffect(EffectType.Stun, 0.06f, 0f));
      activeEffects[player].Add(new ActiveEffect(EffectType.FoodLoss, 0.1f, 1f));
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
  }
  // Classe para rastrear efeitos ativos
  sealed public class ActiveEffect(EffectType type, float duration, float value)
  {
    public EffectType Type { get; set; } = type;
    public float Duration { get; set; } = duration;
    public float StartTime { get; set; } = Time.time;
    public float Value { get; set; } = value;
    public bool HasBeenReverted { get; set; }
    public bool HasBeenApplied { get; set; } = false;
    public bool IsExpired => (Time.time - StartTime) >= Duration;
    public bool ShouldRevert => IsExpired && !HasBeenReverted;
  }
}