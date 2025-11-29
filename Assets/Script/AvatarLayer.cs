using UnityEngine;
using System.Linq;

public class AvatarLayer : MonoBehaviour
{
    [Header("Sprite Renderers by Layer")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;
    public SpriteRenderer hairFrontRenderer;
    public SpriteRenderer hairBackRenderer;
    public SpriteRenderer upperRenderer;
    public SpriteRenderer underRenderer;

    [Header("Sprite Arrays (Assign in Inspector)")]
    public Sprite[] bodySprites;
    public Sprite[] headSprites;
    public Sprite[] hairFrontSprites;
    public Sprite[] hairBackSprites;
    public Sprite[] upperSprites;
    public Sprite[] underSprites;

    public void SetFrame(int index)
    {
        if (bodySprites.Length > index)
            bodyRenderer.sprite = bodySprites[index];

        if (headSprites.Length > index)
            headRenderer.sprite = headSprites[index];

        if (hairFrontSprites.Length > index)
            hairFrontRenderer.sprite = hairFrontSprites[index];

        if (hairBackSprites.Length > index)
            hairBackRenderer.sprite = hairBackSprites[index];

        if (upperSprites.Length > index)
            upperRenderer.sprite = upperSprites[index];

        if (underSprites.Length > index)
            underRenderer.sprite = underSprites[index];
    }
}