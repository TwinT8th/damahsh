using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScrollManager : MonoBehaviour
{
    [Header("카메라 이동 레퍼런스")]
    [SerializeField] private Camera cam;
    [SerializeField] private RectTransform scrollHandle;

    [Header("직접 지정하는 핸들 이동 범위")]
    [SerializeField] private float handleMinX = -238f;
    [SerializeField] private float handleMaxX = 238f;

    [Header("방(배경) 설정")]
    [SerializeField] private List<Transform> backgroundRooms;
    // 각 방 배경 그룹 Transform (왼쪽→오른쪽 순)

    private float[] roomCenterX;   // 카메라가 바라볼 X
    private float[] roomWidth;     // 각 방의 실제 월드 폭 (Sprite 기준)

    [Header("스냅 속도")]
    [SerializeField] private float snapSpeed = 7f;

    [Header("스와이프 감도")]
    [SerializeField] private float dragSensitivity = 0.004f;

    private int currentRoomIndex = 0;
    private Coroutine snapRoutine = null;
    private bool isDragging = false;

    public CharacterMove character;

    private void Start()
    {
        AutoGenerateRoomPositions();
        SetPositionImmediate(currentRoomIndex);
        StartCoroutine(InitScrollHandle());
    }

    private void AutoGenerateRoomPositions()
    {
        int count = backgroundRooms.Count;
        roomCenterX = new float[count];
        roomWidth = new float[count];

        for (int i = 0; i < count; i++)
        {
            Transform room = backgroundRooms[i];
            SpriteRenderer sr = room.GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                // ★ 실제 스프라이트 기준으로 방 중앙/폭 계산
                roomCenterX[i] = sr.bounds.center.x;
                roomWidth[i] = sr.bounds.size.x;
            }
            else
            {
                // Sprite가 없으면 Transform 위치만 사용, 폭은 임의 값
                roomCenterX[i] = room.position.x;
                roomWidth[i] = 6f; // 기본 이동 가능 폭 (원하면 Inspector에서 조절 가능하게 바꿔도 됨)
                Debug.LogWarning($"{room.name}에 SpriteRenderer가 없습니다!");
            }
        }
    }

    private IEnumerator InitScrollHandle()
    {
        yield return null;
        UpdateScrollHandleImmediate();
    }


    // ===== ScrollHandleDrag 이벤트 =====
    public void StartManualDrag()
    {
        if (snapRoutine != null)
            StopCoroutine(snapRoutine);

        isDragging = true;

        if (character != null && !character.IsSleeping()) // 조건 추가
        {
            character.Freeze();
            character.transform.SetParent(cam.transform, true);
        }

        else if(character.IsSleeping())
        {
            character.Freeze();
        }

    }

    public void OnManualDrag(float deltaX)
    {
        if (!isDragging) return;

        float move = deltaX * dragSensitivity;

        Vector3 pos = cam.transform.position;
        pos.x += move;
        cam.transform.position = pos;

        UpdateScrollHandle();
    }

    public void EndManualDrag()
    {
        isDragging = false;
        FindNearestRoomAndSnap();
    }


    // ===== Snap 이동 =====

    private void FindNearestRoomAndSnap()
    {
        float camX = cam.transform.position.x;

        float nearest = float.MaxValue;
        int newIdx = currentRoomIndex;

        for (int i = 0; i < roomCenterX.Length; i++)
        {
            float d = Mathf.Abs(camX - roomCenterX[i]);
            if (d < nearest)
            {
                nearest = d;
                newIdx = i;
            }
        }

        currentRoomIndex = newIdx;

        if (snapRoutine != null)
            StopCoroutine(snapRoutine);
        snapRoutine = StartCoroutine(SnapToRoom(currentRoomIndex));
    }

    IEnumerator SnapToRoom(int idx)
    {
        float targetX = roomCenterX[idx];

        Vector3 start = cam.transform.position;
        Vector3 end = new Vector3(targetX, start.y, start.z);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * snapSpeed;
            cam.transform.position = Vector3.Lerp(start, end, t);
            UpdateScrollHandle();
            yield return null;
        }

        snapRoutine = null;

        if (character != null)
        {
            // 카메라 자식에서 떼고, 해당 방 기준으로 이동가능 범위/위치 설정
            character.transform.SetParent(null, true);

            if (character.IsSleeping())
            {

                character.Unfreeze(); // 애니메이션 속도만 복구 필요
            }
            else
            {
                // 깨어있을 때만 SafePoint 처리
                character.SetRoomLimits(roomCenterX[idx], roomWidth[idx]);
                character.TeleportToRoom(idx);
                character.Unfreeze();
            }
        }
    }


    // ===== ScrollHandle 동기화 =====
    private void UpdateScrollHandle()
    {
        if (roomCenterX == null || roomCenterX.Length == 0) return;

        float camX = cam.transform.position.x;

        // roomCenterX[0] ~ roomCenterX[last]를 전체 구간으로 보고 보간
        float t = Mathf.InverseLerp(roomCenterX[0], roomCenterX[roomCenterX.Length - 1], camX);
        float newX = Mathf.Lerp(handleMinX, handleMaxX, t);

        Vector2 pos = scrollHandle.anchoredPosition;
        pos.x = newX;
        scrollHandle.anchoredPosition = pos;
    }

    private void UpdateScrollHandleImmediate()
    {
        if (roomCenterX == null || roomCenterX.Length <= 1) return;

        float step = (handleMaxX - handleMinX) / (roomCenterX.Length - 1);
        float newX = handleMinX + step * currentRoomIndex;

        Vector2 pos = scrollHandle.anchoredPosition;
        pos.x = newX;
        scrollHandle.anchoredPosition = pos;
    }

    private void SetPositionImmediate(int idx)
    {
        if (roomCenterX == null || roomCenterX.Length == 0) return;

        Vector3 p = cam.transform.position;
        p.x = roomCenterX[idx];
        cam.transform.position = p;
    }
}
