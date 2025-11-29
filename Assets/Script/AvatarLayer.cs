using UnityEngine;

public class AvatarLayer : MonoBehaviour
{
    [Header("Sprite Renderers by Layer")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer hairRenderer;
    public SpriteRenderer outfitRenderer;
    public SpriteRenderer accessoryRenderer;

    [Header("Animation State Name")]
    public string currentAnimState = "Idle"; // Idle / Walk µî

    public void ChangeOutfit(string outfitName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Character/Outfit/{currentAnimState}/{outfitName}");
        outfitRenderer.sprite = sprites.Length > 0 ? sprites[0] : null;
    }

    public void ChangeHair(string hairName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Character/Hair/{currentAnimState}/{hairName}");
        hairRenderer.sprite = sprites.Length > 0 ? sprites[0] : null;
    }

    public void ChangeAccessory(string accName)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>($"Character/Accessories/{currentAnimState}/{accName}");
        accessoryRenderer.sprite = sprites.Length > 0 ? sprites[0] : null;
    }

    public void SetAnimState(string animState)
    {
        currentAnimState = animState;
    }



}