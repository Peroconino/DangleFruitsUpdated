using UnityEngine;

namespace DangleFruitsUpdated;

partial class DangleFruitsUpdated
{
  public void hook_DangleFruit_ApplyPalette(On.DangleFruit.orig_ApplyPalette orig, DangleFruit self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
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