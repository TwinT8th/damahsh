using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI.Table;

/// <summary>
/// "sing" 입력 시 연출 시퀀스 실행:
/// 1) 이미지1 표시 -> 4초
/// 2) 이미지2로 교체 -> 2초
/// 3) 마이크 스프라이트가 2초 동안 내려옴(부드럽게/중력처럼) -> 목표 위치
/// 4) 캐릭터 Animator를 sing 상태로 전환(루프)
/// "stop" 입력 시 언제든 연출/루프 정지 + 초기화
/// </summary>
public class SingSequenceController : MonoBehaviour
{

    public void StartSingSequence()
    {
        Debug.Log("SingSingSingmybaby");
    }



}
