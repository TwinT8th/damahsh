using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatLogUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;     // ScrollView의 Content
    [SerializeField] private GameObject messagePrefab;    // TMP_Text가 들어있는 프리팹
    [SerializeField] private ScrollRect scrollRect;       // Scroll View의 ScrollRect

    [Header("Auto Scroll")]
    [SerializeField] private bool autoScrollToBottom = true;

    private Coroutine _scrollCo;

    public void AddPlayerMessage(string text)
    {
        if (contentParent == null || messagePrefab == null)
        {
            Debug.LogError("[ChatLogUI] contentParent 또는 messagePrefab이 비었습니다.");
            return;
        }

        var go = Instantiate(messagePrefab, contentParent);
        var tmp = go.GetComponentInChildren<TMP_Text>();

        if (tmp == null)
        {
            Debug.LogError("[ChatLogUI] messagePrefab에 TMP_Text가 없습니다.");
            Destroy(go);
            return;
        }

        tmp.text = text;

        if (autoScrollToBottom)
            ScrollToBottom();
    }

    public void AddNpcMessage(string text)
    {
        AddPlayerMessage(text);
    }

    private void ScrollToBottom()
    {
        if (scrollRect == null) return;

        // 여러 번 연속 추가될 때 코루틴 중복 방지
        if (_scrollCo != null) StopCoroutine(_scrollCo);
        _scrollCo = StartCoroutine(CoScrollToBottomNextFrame());
    }

    private IEnumerator CoScrollToBottomNextFrame()
    {
        // 레이아웃/컨텐츠 높이 갱신 강제
        Canvas.ForceUpdateCanvases();

        // 그래도 1프레임 뒤에 레이아웃이 확정되는 경우가 많아서 한 프레임 기다림
        yield return null;

        Canvas.ForceUpdateCanvases();

        // 맨 아래 (0 = bottom, 1 = top)
        scrollRect.verticalNormalizedPosition = 0f;

        _scrollCo = null;
    }
}
