using System.Linq;
using UnityEngine;

namespace DangleFruitsUpdated;
public static class DangleFruitCustomConstants
{
  public const float Normal = 0.50f;//.8
  public const float Rare = 0.25f;//.15
  public const float VeryRare = 0.15f;//.04
  public const float ExtremelyRare = 0.1f;//.01
  public enum FruitTypeColors
  {
    Blue = 1,
    Green = 2,
    Red = 3,
    Golden = 4
  }
  public static void ApplyPoisonEffect(RoomCamera.SpriteLeaser sLeaser, float timeStacker)
  {
    // Efeito verde pulsante
    float pulse = Mathf.Sin(Time.time * 2f) * 0.3f + 0.7f;
    sLeaser.sprites[2].scale = 1.5f * pulse;
    sLeaser.sprites[2].color = new Color(0.2f, 0.8f, 0.1f, 0.3f);

    // Partículas verdes
    sLeaser.sprites[3].scale = 0.3f;
    sLeaser.sprites[3].color = new Color(0.1f, 0.7f, 0.1f, Mathf.Sin(Time.time * 4f) * 0.3f);
  }
  public static void ApplySmokeEffect(RoomCamera.SpriteLeaser sLeaser, float timeStacker)
  {
    // Efeito de fumaça vermelha
    sLeaser.sprites[2].scale = 1.2f;
    sLeaser.sprites[2].color = new Color(0.8f, 0.2f, 0.1f, 0.4f);

    // Partículas de fumaça
    float smokeWave = Mathf.Sin(Time.time * 3f);
    sLeaser.sprites[3].scale = 0.4f + smokeWave * 0.2f;
    sLeaser.sprites[3].color = new Color(0.7f, 0.3f, 0.1f, 0.3f - smokeWave * 0.1f);
  }
  public static void ApplyGlowEffect(RoomCamera.SpriteLeaser sLeaser, float timeStacker)
  {
    // Aura dourada
    sLeaser.sprites[2].scale = 1.3f;
    sLeaser.sprites[2].color = new Color(1f, 0.9f, 0.2f, 0.4f);

    // Brilhos aleatórios
    if (Random.value < 0.05f)
    {
      sLeaser.sprites[3].scale = Random.Range(0.2f, 0.5f);
      sLeaser.sprites[3].color = new Color(1f, 1f, 0.5f, Random.Range(0.4f, 0.8f));
    }
  }
}
