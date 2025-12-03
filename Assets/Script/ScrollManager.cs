using UnityEngine;
using System.Collections;

public class ScrollManager : MonoBehaviour
{
    [Header("필수 레퍼런스")]
    [SerializeField] private Transform background;
    [SerializeField] private RectTransform scrollHandle;

    [Header("직접 지정하는 핸들 이동 범위 (anchoredPosition.x)")]
    [SerializeField] private float handleMinX = -238f;
    [SerializeField] private float handleMaxX = 238f;

    [Header("방 설정")]
    [SerializeField] private int totalRooms = 3;
    [SerializeField] private float roomWidth = 4f;

    [Header("스냅 속도")]
    [SerializeField] private float snapSpeed = 7f;

    [Header("스와이프 감도")]
    [SerializeField] private float dragSensitivity = 0.003f;

    private int currentRoomIndex = 0;
    private float minBgX;
    private float maxBgX;
    private Coroutine snapRoutine = null;

    private bool isDragging = false;

    private void Start()
    {
        maxBgX = 0f;
        minBgX = -(totalRooms - 1) * roomWidth;

        SetPositionImmediate(currentRoomIndex);
        StartCoroutine(InitScrollHandle());
    }

    private IEnumerator InitScrollHandle()
    {
        yield return null;
        UpdateScrollHandleImmediate();
    }

    // ===== 스크롤 핸들에서 호출되는 이벤트 =====

    public void StartManualDrag()
    {
        if (snapRoutine != null)
            StopCoroutine(snapRoutine);

        isDragging = true;
    }

    public void OnManualDrag(float deltaX)
    {
        if (!isDragging) return;

        float worldDeltaX = -deltaX * dragSensitivity;

        Vector3 pos = background.localPosition;
        pos.x += worldDeltaX;
        pos.x = Mathf.Clamp(pos.x, minBgX, maxBgX);
        background.localPosition = pos;

        UpdateScrollHandle();
    }

    public void EndManualDrag()
    {
        isDragging = false;

        FindNearestRoomAndSnap();
    }

    // ===== Snap & 이동 로직 =====

    private void FindNearestRoomAndSnap()
    {
        float bgX = background.localPosition.x;
        float t = Mathf.InverseLerp(maxBgX, minBgX, bgX);
        currentRoomIndex = Mathf.RoundToInt(t * (totalRooms - 1));
        currentRoomIndex = Mathf.Clamp(currentRoomIndex, 0, totalRooms - 1);

        if (snapRoutine != null)
            StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapToRoom(currentRoomIndex));
    }

    IEnumerator SnapToRoom(int idx)
    {
        float targetX = -idx * roomWidth;
        Vector3 start = background.localPosition;
        Vector3 end = new Vector3(targetX, start.y, start.z);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * snapSpeed;
            background.localPosition = Vector3.Lerp(start, end, t);
            UpdateScrollHandle();
            yield return null;
        }

        snapRoutine = null;
    }

    private void UpdateScrollHandle()
    {
        float bgX = background.localPosition.x;
        float t = Mathf.InverseLerp(maxBgX, minBgX, bgX);
        float newX = Mathf.Lerp(handleMinX, handleMaxX, t);

        Vector2 pos = scrollHandle.anchoredPosition;
        pos.x = newX;
        scrollHandle.anchoredPosition = pos;
    }

    private void UpdateScrollHandleImmediate()
    {
        float step = (handleMaxX - handleMinX) / (totalRooms - 1);
        float newX = handleMinX + step * currentRoomIndex;

        Vector2 pos = scrollHandle.anchoredPosition;
        pos.x = newX;
        scrollHandle.anchoredPosition = pos;
    }

    private void SetPositionImmediate(int roomIdx)
    {
        float x = -roomIdx * roomWidth;
        x = Mathf.Clamp(x, minBgX, maxBgX);

        Vector3 p = background.localPosition;
        p.x = x;
        background.localPosition = p;
    }
}
