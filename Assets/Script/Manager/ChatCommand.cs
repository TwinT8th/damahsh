using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class ChatCommand : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;//텍스트 입력창

    [Header("Commands")]
    [SerializeField] private List<ChatCommandEntry> commands = new(); //인스펙터에서 등록하는 키워드 목록
    [SerializeField] private ChatLogUI chatLog;

    [Header("NPC Reply Queue")]
    [SerializeField] private float npcFirstDelay = 1f;     // 첫 답장까지 대기
    [SerializeField] private float npcLineInterval = 0.8f; // 줄과 줄 사이 간격
    [SerializeField] private AudioSource voiceSource;

    [Header("Voice Clips")]
    [SerializeField] private AudioClip helloClip;
    [SerializeField] private AudioClip introClip1;
    [SerializeField] private AudioClip introClip2;

    [Header("SingingMouth")]
    [SerializeField] private SpriteRenderer mouthSprite;


    private readonly Dictionary<string, ChatCommandEntry> _map = new();

    [Serializable]
    private class NpcLine
    {
        public string text;
        public AudioClip voice;

        public NpcLine(string text, AudioClip voice)
        {
            this.text = text;
            this.voice = voice;
        }
    }

    private readonly Queue<NpcLine> _npcQueue = new();
    private Coroutine _npcReplyRoutine;
    private Coroutine _mouthRoutine;


    // Start is called before the first frame update
    void Awake()
    {
        if(inputField == null)
        {
            Debug.LogError("[ChatCommandConsole] inputField가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        //인스펙터에 등록한 리스트를 딕셔너리로 변환
        BuildMap();

        inputField.onSubmit.AddListener(_ => Submit()); //Enter누르면 Submit()실행, PC전용

        if (mouthSprite != null)
        {
            mouthSprite.enabled = false;
        }

    }

    void OnDestroy()
    {
        // 이벤트 리스너 제거 (메모리 누수 방지)
        if (inputField != null)
            inputField.onSubmit.RemoveAllListeners();
    }



    /// <summary>
    /// 인스펙터에서 설정한 명령 리스트를
    /// 딕셔너리로 변환해 빠르게 검색할 수 있게 만든다.
    /// </summary>
    private void BuildMap()
    {
        _map.Clear();

        foreach (var entry in commands)
        {
            if (entry == null) continue;

            var key = Normalize(entry.keyword);

            // 비어 있는 키워드는 무시
            if (string.IsNullOrEmpty(key))
                continue;

            // 같은 키워드가 두 번 등록되었는지 체크
            if (_map.ContainsKey(key))
            {
                Debug.LogWarning($"[ChatCommandConsole] 중복 키워드: '{key}' (리스트에서 하나만 남기세요)");
                continue;
            }

            _map.Add(key, entry);
        }
    }


    private static bool ContainsWord(string text, string word)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(word)) return false;

        // Normalize된 상태를 가정하고, 단어 경계 기준으로 검사
        // "hi there" OK, "say hi" OK, "hike"는 X
        return System.Text.RegularExpressions.Regex.IsMatch(
            text,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(word)}\b"
        );
    }

    /// <summary>
    /// 입력값을 검사하고,
    /// 키워드와 일치하면 명령을 실행한다.
    /// </summary>
    public void Submit()
    {
        if (inputField == null) return;

        // 1) 입력값 저장 (원문)
        string raw = inputField.text;

        // 2) 입력창 비우기
        inputField.text = "";

        // 3) (선택) 다시 입력 포커스
        inputField.ActivateInputField();

        // 4) 공백만 입력은 무시
        if (string.IsNullOrWhiteSpace(raw)) return;

        // 5) 플레이어가 보낸 메시지는 무조건 로그에 찍기 (원문 그대로)
        if (chatLog != null)
            chatLog.AddPlayerMessage(raw);

        // 6) 비교용 키 정규화
        var key = Normalize(raw);

        // 7) 특정 키워드면 NPC 자동 답장(원하는 규칙대로)

        //------------------------------------------------
        if (chatLog != null && (ContainsWord(key, "hi") || ContainsWord(key, "hello")))
        {
            EnqueueNpc(new NpcLine("> 안녕.", helloClip));
        }

        if (chatLog != null && ContainsWord(key, "name"))
        {
            EnqueueNpc(new NpcLine("> 나는 희소한이라고 해", introClip1));
            EnqueueNpc(new NpcLine("> 반가워", introClip2));

        }

        // 8) 기존 명령 실행 로직 유지
        if (_map.TryGetValue(key, out var cmd))
        {
            if (cmd.oneShot && cmd.used)
            {
                Debug.Log($"[ChatCommand] 이미 사용한 명령: {cmd.keyword}");
                return;
            }

            cmd.used = true;
            cmd.onMatched?.Invoke();
        }
        else
        {
            Debug.Log($"[ChatCommand] 매칭 실패: '{raw}'");
        }
    }


    public void MuteHSH()
    {
        if (mouthSprite != null)
        {
            mouthSprite.enabled = false;
        }
    }


    /// <summary>
    /// 문자열을 비교하기 쉽게 정리한다.
    /// - 앞뒤 공백 제거
    /// - 여러 공백을 하나로
    /// - 영문 대소문자 무시
    /// </summary>
    private static string Normalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";

        s = s.Trim();

        // 소문자
        s = s.ToLowerInvariant();

        // 문장부호/특수문자 제거하고(문자/숫자/공백만 남김)
        // 예: "hi." -> "hi", "hello!!" -> "hello"
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^\p{L}\p{N}\s]+", "");

        // 공백 여러 개 → 하나로
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");

        return s;
    }






    private void EnqueueNpc(params NpcLine[] lines)
    {
        if (lines == null || lines.Length == 0) return;

        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line.text))
                _npcQueue.Enqueue(line);
        }

        if (_npcReplyRoutine == null)
            _npcReplyRoutine = StartCoroutine(CoNpcReplyQueue());
    }
    private IEnumerator CoNpcReplyQueue()
    {
        // 첫 답장 딜레이 (사람이 생각하다 답장하는 느낌)
        if (npcFirstDelay > 0f)
            yield return new WaitForSeconds(npcFirstDelay);

        while (_npcQueue.Count > 0)
        {
            var line = _npcQueue.Dequeue();

            if (chatLog != null)
                chatLog.AddNpcMessage(line.text);

            if (voiceSource != null && line.voice != null)
            {
                voiceSource.PlayOneShot(line.voice);

                PlayMouth(line.voice.length);

                while (voiceSource.isPlaying)
                    yield return null;
            }
            else
            {
                yield return new WaitForSeconds(npcLineInterval);
            }

            if (_npcQueue.Count > 0 && npcLineInterval > 0f)
                yield return new WaitForSeconds(npcLineInterval);
        }

        _npcReplyRoutine = null;
    }


    private void PlayMouth(float sec)
    {
        if (mouthSprite == null) return;

        if (_mouthRoutine != null) StopCoroutine(_mouthRoutine);
        _mouthRoutine = StartCoroutine(CoMouth(sec));
    }

    private IEnumerator CoMouth(float sec)
    {
        mouthSprite.enabled = true;
        yield return new WaitForSeconds(sec);
        mouthSprite.enabled = false;
        _mouthRoutine = null;
    }



}

/// <summary>
/// 하나의 채팅 명령 데이터를 담는 클래스.
/// 인스펙터에서 설정한다.
/// </summary>
[Serializable]
public class ChatCommandEntry
{
    public string keyword;
    // 입력해야 할 정확한 문자열 (예: "open sesame")

    public bool oneShot = true;
    // true이면 한 번 실행 후 재사용 불가

    [NonSerialized]
    public bool used;
    // 런타임에서만 사용되는 상태값 (저장되지 않음)

    public UnityEvent onMatched;
    // 키워드가 일치했을 때 실행할 이벤트

}

