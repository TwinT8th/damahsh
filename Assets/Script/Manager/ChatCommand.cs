using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Events;

public class ChatCommand : MonoBehaviour
{

    [Header("UI")]
    [SerializeField] private TMP_InputField inputField;//텍스트 입력창

    [Header("Commands")]
    [SerializeField] private List<ChatCommandEntry> commands = new(); //인스펙터에서 등록하는 키워드 목록

    private readonly Dictionary<string, ChatCommandEntry> _map = new();


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

    /// <summary>
    /// 입력값을 검사하고,
    /// 키워드와 일치하면 명령을 실행한다.
    /// </summary>
    public void Submit()
    {
        // 입력값 저장
        string raw = inputField.text;

        // 입력창 비우기
        inputField.text = "";

        // 다시 입력 가능 상태로 활성화
        inputField.ActivateInputField();

        // 공백/대소문자 등을 정리한 값
        var key = Normalize(raw);

        // 빈 입력은 무시
        if (string.IsNullOrEmpty(key)) return;

        // 딕셔너리에서 해당 키 검색
        if (_map.TryGetValue(key, out var cmd))
        {
            // 한 번만 실행되는 명령이고,
            // 이미 사용했다면 재실행 방지
            if (cmd.oneShot && cmd.used)
            {
                Debug.Log($"[ChatCommandConsole] 이미 사용한 명령: {cmd.keyword}");
                return;
            }

            // 사용 처리
            cmd.used = true;

            // 인스펙터에서 연결한 이벤트 실행
            cmd.onMatched?.Invoke();
        }
        else
        {
            // 일치하는 키워드가 없을 때
            Debug.Log($"[ChatCommandConsole] 매칭 실패: '{raw}'");
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

        // 공백 여러 개 → 하나로
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");

        // 대소문자 구분 제거
        s = s.ToLowerInvariant();

        return s;
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
