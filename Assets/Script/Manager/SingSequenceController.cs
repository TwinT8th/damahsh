using System.Collections;
using UnityEngine;

//using static UnityEngine.Rendering.DebugUI.Table;

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

    ScrollManager scrollManager;

    [Header("Room Overlays")]
    [SerializeField] private GameObject darkOverlay;       // 깜깜한 방 덮개
    [SerializeField] private GameObject spotlightOverlay;  // 스포트라이트 덮개

    [Header("Timing")]
    [SerializeField] private float delayBeforeDark = 2f;       // 2초 후 깜깜
    [SerializeField] private float darkDuration = 4f;          // 2초 유지 후 스포트라이트

    [Header("Mic Drop")]
    [SerializeField] private Transform mic;           // 마이크 스프라이트 오브젝트
    [SerializeField] private Transform micStart;      // 화면 위(숨겨진 위치) 빈 오브젝트
    [SerializeField] private Transform micTarget;     // 멈출 위치(아바타 앞) 빈 오브젝트
    [SerializeField] private float micDropDuration = 2f;
    // 빠르게 -> 느리게(감속) 용 커브
    // 기본값: EaseOut (처음 빠르고 끝에서 느려짐)
    [SerializeField]
    private AnimationCurve micDropCurve =
        new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Header("Mic Bounce")]
    [SerializeField] private int bounceCount = 3;          // 통통 몇 번? (2 이상)
    [SerializeField] private float bounceHeight = 0.8f;    // 첫 튀어오름 높이(월드 단위)
    [SerializeField] private float bounceDamping = 0.55f;  // 다음 높이는 몇 배로 줄일지(0~1)
    [SerializeField] private float bounceTime = 0.18f;     // 첫 바운스(위/아래 한 번)의


 

    private Coroutine _routine;


    void Start()
    {
        scrollManager = FindObjectOfType<ScrollManager>();

        if (darkOverlay != null)
            darkOverlay.SetActive(false);

        if (spotlightOverlay != null)
            spotlightOverlay.SetActive(false);
        // 시작 시 마이크는 화면 밖에 숨겨두기
        if (mic != null && micStart != null)
            mic.position = micStart.position;


    }
    public void StartSingSequence()
    {
   
        if (scrollManager == null)
        {
            Debug.Log("[SingSequenceController] ScrollManager 참조가 비었습니다.");
            return;
        }
        _routine = StartCoroutine(CoSingSequence());
    }

    private IEnumerator CoSingSequence()
    {
        // 0) 방 중앙 고정 (너가 이미 완성한 단계)
        scrollManager.MoveForAction();

        // 1) n초 기다렸다가 깜깜
        yield return new WaitForSeconds(delayBeforeDark);
        SetOverlay(darkOn: true, spotOn: false);

        // 2) n초 뒤 스포트라이트
        yield return new WaitForSeconds(darkDuration);
        SetOverlay(darkOn: false, spotOn: true);

        // 여기서부터 다음 단계(이미지 교체, 마이크 드랍, 애니 sing) 이어붙이면 됨

        //마이크 드랍
        yield return StartCoroutine(DropMic());

        yield return new WaitForSeconds(1f);



        _routine = null;
    }
    private void SetOverlay(bool darkOn, bool spotOn)
    {
        if (darkOverlay != null)
            darkOverlay.SetActive(darkOn);

        if (spotlightOverlay != null)
            spotlightOverlay.SetActive(spotOn);
    }

    // 나중에 stop에서 호출할 용도(연출 초기화)
    public void ResetOverlays()
    {
        SetOverlay(false, false);
    }


    // 중력 느낌(점점 빨라짐) + 마지막에 살짝 바운스(오버슈트 1회) 후 정지
    // 중력 느낌(점점 빨라짐) + 고무공처럼 통통(여러 번, 점점 낮아짐)
    private IEnumerator DropMic()
    {
        if (mic == null || micStart == null || micTarget == null)
        {
            Debug.LogError("[SingSequenceController] mic/micStart/micTarget 참조가 비었습니다.");
            yield break;
        }

        // 시작 위치(화면 밖)로 스냅
        mic.position = micStart.position;

        Vector3 from = micStart.position;
        Vector3 to = micTarget.position;

        float dur = Mathf.Max(0.01f, micDropDuration);

        // ----- 1) 가속 낙하(중력 느낌) -----
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            // EaseIn(초반 느림 -> 후반 빠름) : 커브 없으면 t^2
            float eased = (micDropCurve != null)
                ? micDropCurve.Evaluate(Mathf.Clamp01(t))
                : Mathf.Clamp01(t) * Mathf.Clamp01(t);

            mic.position = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        mic.position = to;

        // ----- 2) 통통 바운스(점점 줄어듦) -----
        // 바운스 방향: "위로" (만화적 고무공 느낌)
        Vector3 up = Vector3.up;

        // 첫 바운스 높이/시간
        float height = Mathf.Max(0f, bounceHeight);
        float halfTime = Mathf.Max(0.01f, bounceTime); // 위로 가는 시간, 아래로 가는 시간(각각)

        int count = Mathf.Max(0, bounceCount);

        for (int i = 0; i < count; i++)
        {
            if (height <= 0.0001f) break;

            Vector3 peak = to + up * height;

            // 바닥(to) -> 꼭대기(peak) : 빠르게 튀어오르고
            yield return StartCoroutine(LerpPosition(to, peak, halfTime, easeOut: true));

            // 꼭대기(peak) -> 바닥(to) : 좀 더 빠르게 떨어지는 느낌(가속) 주기
            // (만화처럼 통!통!은 떨어질 때도 빨라야 맛이 남)
            yield return StartCoroutine(LerpPosition(peak, to, halfTime, easeOut: false));

            // 다음 바운스는 더 낮게 + 약간 더 짧게(템포 업)
            height *= Mathf.Clamp01(bounceDamping);

        }
    }

    // 보조: 두 지점 사이를 부드럽게 이동
    // 보조: 두 지점 사이를 부드럽게 이동 (easeOut = true면 끝에서 감속, false면 가속)
    private IEnumerator LerpPosition(Vector3 a, Vector3 b, float duration, bool easeOut)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float x = Mathf.Clamp01(t);

            // easeOut: 처음 빠르고 끝에서 느려짐 (튀어오를 때 "통!" 느낌)
            // easeIn : 처음 느리고 끝에서 빨라짐 (떨어질 때 "중력" 느낌)
            float eased = easeOut
                ? 1f - Mathf.Pow(1f - x, 2f)   // easeOutQuad
                : x * x;                       // easeInQuad

            mic.position = Vector3.LerpUnclamped(a, b, eased);
            yield return null;
        }

        mic.position = b;
    }



}
