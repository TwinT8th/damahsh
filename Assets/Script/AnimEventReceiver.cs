using UnityEngine;

public class AnimEventReceiver : MonoBehaviour
{
    private CharacterMove characterMove;

    void Awake()
    {
        characterMove = GetComponentInParent<CharacterMove>();
    }

    // AnimationEvent로 실행될 함수
    public void ForceMoveToBed()
    {
        if (characterMove != null)
            characterMove.ForceMoveToBed();
    }

    public void WakeUpPlace()
    {
        if (characterMove != null)
            characterMove.WakeUpPlace();
    }
}
