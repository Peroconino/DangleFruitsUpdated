using System.Linq;
using Expedition;
using HarmonyLib;

namespace DangleFruitsUpdated;

[HarmonyPatch]
partial class DangleFruitsUpdated
{
  [HarmonyPatch(typeof(ExpeditionGame), "get_explosivejump")]
  [HarmonyPostfix]
  static void ExplosiveJumpPatch(ref bool __result)
  {
    // Verifica se o player existe no dicionário e tem o efeito ativo
    if (player is not null && activeEffects.TryGetValue(player, out var effects))
    {
      __result = effects.Any(effect => effect.Type == EffectType.ClassMechanicsArtificer);
    }
  }

  [HarmonyPatch(typeof(Player), "GraspsCanBeCrafted")]
  [HarmonyPostfix]
  static void GrapsCanBeCraftedPatch(ref bool __result, Player __instance)
  {
    // Verifica se o player existe no dicionário e tem o efeito ativo
    if (__instance is not null && activeEffects.TryGetValue(__instance, out var effects))
    {
      __result = effects.Any(effect => effect.Type == EffectType.Crafting) && __instance.CraftingResults != null;
    }
  }
}