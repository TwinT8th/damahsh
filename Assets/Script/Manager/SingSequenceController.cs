using System.Collections;
using UnityEngine;

public class SingSequenceController : MonoBehaviour
{
    ScrollManager scrollManager;

    [Header("Room Overlays")]
    [SerializeField] private GameObject darkOverlay;       // 깜깜한 방 덮개
    [SerializeField] private GameObject spotlightOverlay;  // 스포트라이트 덮개

    [Header("Timing")]
    [SerializeField] private float delayBeforeDark = 2f;   // 2초 후 깜깜
    [SerializeField] private float darkDuration = 3f;      // 3초 유지 후 스포트라이트

    [Header("Mic Drop")]
    [SerializeField] private Transform mic;
    [SerializeField] private Transform micStart;
    [SerializeField] private Transform micTarget;
    [SerializeField] private float micDropDuration = 2f;
    [SerializeField]
    private AnimationCurve micDropCurve =
        new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

    [Header("Mic Bounce")]
    [SerializeField] private int bounceCount = 3;
    [SerializeField] private float bounceHeight = 0.8f;
    [SerializeField] private float bounceDamping = 0.55f;
    [SerializeField] private float bounceTime = 0.18f;

    [Header("Test Sequence")]
    [SerializeField] private ChatCommand chatCommand;   // 보이스+입 담당
    [SerializeField] private AudioClip testVoice;       // 테스트 보이스
    [SerializeField] private float afterMicDelay = 2f;  // 마이크 후 딜레이
    [SerializeField] private float afterVoiceDelay = 2f;// 보이스 후 딜레이
    [SerializeField] private bool showTextInChat = false;
    [SerializeField] private string testText = "> (테스트 보이스)";

    private Coroutine _routine;

    void Start()
    {
        scrollManager = FindObjectOfType<ScrollManager>();

        if (darkOverlay != null) darkOverlay.SetActive(false);
        if (spotlightOverlay != null) spotlightOverlay.SetActive(false);

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

        // 이미 연출 중이면 중복 시작 방지
        if (_routine != null) return;

        _routine = StartCoroutine(CoSingSequence());
    }

    public void StartTestSequence()
    {
        StopCurrentRoutine();
        _routine = StartCoroutine(CoTestSequence());
    }

    // bye 입력 시 호출할 함수
    public void StartByeSequence()
    {
        // 진행 중이던 sing(또는 다른 연출) 즉시 중단
        StopCurrentRoutine();

        // 역재생(원복) 시작
        _routine = StartCoroutine(CoByeSequence());
    }

    private void StopCurrentRoutine()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
    }

    private IEnumerator CoSingSequence()
    {
        scrollManager.MoveForAction();

        yield return new WaitForSeconds(delayBeforeDark);
        SetOverlay(darkOn: true, spotOn: false);

        yield return new WaitForSeconds(darkDuration);
        SetOverlay(darkOn: false, spotOn: true);

        yield return StartCoroutine(DropMic());

        yield return new WaitForSeconds(1f);

        _routine = null;
    }

    //  역재생(원복) 시퀀스
    private IEnumerator CoByeSequence()
    {
        // 1) 마이크가 내려와 있으면 → 위로 올려서 숨김 위치로
        if (mic != null && micStart != null)
        {
            // micStart와 충분히 떨어져 있으면 복귀 애니메이션
            if (Vector3.Distance(mic.position, micStart.position) > 0.001f)
                yield return StartCoroutine(ReturnMic());
            else
                mic.position = micStart.position;
        }

        // 2) 스포트라이트가 켜져 있었다면 → 어둠으로(역방향 느낌)
        bool spotWasOn = spotlightOverlay != null && spotlightOverlay.activeSelf;
        bool darkWasOn = darkOverlay != null && darkOverlay.activeSelf;

        if (spotWasOn)
        {
            SetOverlay(darkOn: true, spotOn: false);

            if (darkDuration > 0f)
                yield return new WaitForSeconds(darkDuration);
        }

        // 바로 원래 상태
        SetOverlay(false, false);
       

        if (scrollManager != null)
            scrollManager.ReleaseAfterAction();

        _routine = null;
    }

    private IEnumerator CoTestSequence()
    {
        if (scrollManager == null)
        {
            Debug.Log("[SingSequenceController] ScrollManager 참조가 비었습니다.");
            _routine = null;
            yield break;
        }

        // 1) sing과 동일
        scrollManager.MoveForAction();

        yield return new WaitForSeconds(delayBeforeDark);
        SetOverlay(darkOn: true, spotOn: false);

        yield return new WaitForSeconds(darkDuration);
        SetOverlay(darkOn: false, spotOn: true);

        yield return StartCoroutine(DropMic());

        // 2) 마이크 내려온 뒤 2초
        if (afterMicDelay > 0f)
            yield return new WaitForSeconds(afterMicDelay);

        // 3) 보이스 + 입뻐끔
        if (chatCommand != null && testVoice != null)
        {
            yield return StartCoroutine(chatCommand.CoPlayVoiceOnly(
                testVoice,
                testText,
                showTextInChat
            ));
        }
        else
        {
            Debug.LogWarning("[SingSequenceController] chatCommand 또는 testVoice가 비어있어서 보이스를 재생하지 못했습니다.");
        }

        // 4) 끝나고 2초
        if (afterVoiceDelay > 0f)
            yield return new WaitForSeconds(afterVoiceDelay);

        // 5) bye(역재생)
        yield return StartCoroutine(CoByeSequence());

        _routine = null;
    }

    private void SetOverlay(bool darkOn, bool spotOn)
    {
        if (darkOverlay != null) darkOverlay.SetActive(darkOn);
        if (spotlightOverlay != null) spotlightOverlay.SetActive(spotOn);
    }

    public void ResetOverlays()
    {
        SetOverlay(false, false);
    }

    // 내려온 마이크를 위로 되돌리는 코루틴 (역방향)
    private IEnumerator ReturnMic()
    {
        if (mic == null || micStart == null) yield break;

        Vector3 from = mic.position;
        Vector3 to = micStart.position;

        float dur = Mathf.Max(0.01f, micDropDuration);
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            // 내려올 때 썼던 커브를 역으로 쓰면 느낌이 깔끔함
            float x = Mathf.Clamp01(t);
            float eased = (micDropCurve != null)
                ? micDropCurve.Evaluate(1f - x) // 역커브
                : 1f - (x * x);

            // eased는 1->0으로 가는 값이니, 보간을 반대로
            mic.position = Vector3.LerpUnclamped(to, from, eased);

            yield return null;
        }

        mic.position = to;
    }

    private IEnumerator DropMic()
    {
        if (mic == null || micStart == null || micTarget == null)
        {
            Debug.LogError("[SingSequenceController] mic/micStart/micTarget 참조가 비었습니다.");
            yield break;
        }

        mic.position = micStart.position;

        Vector3 from = micStart.position;
        Vector3 to = micTarget.position;

        float dur = Mathf.Max(0.01f, micDropDuration);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;

            float eased = (micDropCurve != null)
                ? micDropCurve.Evaluate(Mathf.Clamp01(t))
                : Mathf.Clamp01(t) * Mathf.Clamp01(t);

            mic.position = Vector3.LerpUnclamped(from, to, eased);
            yield return null;
        }
        mic.position = to;

        Vector3 up = Vector3.up;

        float height = Mathf.Max(0f, bounceHeight);
        float halfTime = Mathf.Max(0.01f, bounceTime);
        int count = Mathf.Max(0, bounceCount);

        for (int i = 0; i < count; i++)
        {
            if (height <= 0.0001f) break;

            Vector3 peak = to + up * height;

            yield return StartCoroutine(LerpPosition(to, peak, halfTime, easeOut: true));
            yield return StartCoroutine(LerpPosition(peak, to, halfTime, easeOut: false));

            height *= Mathf.Clamp01(bounceDamping);
        }
    }

    private IEnumerator LerpPosition(Vector3 a, Vector3 b, float duration, bool easeOut)
    {
        float t = 0f;
        float dur = Mathf.Max(0.01f, duration);

        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float x = Mathf.Clamp01(t);

            float eased = easeOut
                ? 1f - Mathf.Pow(1f - x, 2f)
                : x * x;

            mic.position = Vector3.LerpUnclamped(a, b, eased);
            yield return null;
        }

        mic.position = b;
    }
}