using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ScrollManager : MonoBehaviour
{
    [Header("카메라 이동 레퍼런스")]
    [SerializeField] private Camera cam;
    [SerializeField] private RectTransform scrollHandle;

    [Header("직접 지정하는 핸들 이동 범위")]
    [SerializeField] private float handleMinX = -620f;
    [SerializeField] private float handleMaxX = -138f;

    [Header("방(배경) 설정")]
    [SerializeField] private List<Transform> backgroundRooms;
    // 각 방 배경 그룹 Transform (왼쪽→오른쪽 순)

    [Header("카메라 기준 오프셋(카메라 중앙에서 얼마나 좌/우로 기준을 둘지)")]
    [SerializeField] private float cameraRoomOffsetX = 3.8f;

    private float[] roomCenterX;   // 카메라가 바라볼 X
    private float[] roomWidth;     // 각 방의 실제 월드 폭 (Sprite 기준)

    [Header("스냅 속도")]
    [SerializeField] private float snapSpeed = 7f;

    [Header("스와이프 감도")]
    [SerializeField] private float dragSensitivity = 0.004f;

    private int currentRoomIndex = 0;
    private Coroutine snapRoutine = null;
    private bool isDragging = false;

    private float minCamX, maxCamX;


    public CharacterMove character;
    private bool holdCharacterAfterSnap = false; // sing 같은 이벤트로 고정할 때 사용


    private void Start()
    {
        AutoGenerateRoomPositions();

        minCamX = roomCenterX[0] + cameraRoomOffsetX;
        maxCamX = roomCenterX[roomCenterX.Length - 1] + cameraRoomOffsetX;

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
        pos.x = Mathf.Clamp(pos.x + move, minCamX, maxCamX);
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
            float targetX = roomCenterX[i] + cameraRoomOffsetX; // ★ 비교 기준도 오프셋 포함
            float d = Mathf.Abs(camX - targetX);

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
        float targetX = roomCenterX[idx] + cameraRoomOffsetX;

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
            // 카메라 자식에서 떼기
            character.transform.SetParent(null, true);

            if (character.IsSleeping())
            {
                // 자는 상태면 기존 정책 유지(원하면 여기서도 고정/이동 정책 바꿀 수 있음)
                character.Unfreeze();
            }
            else
            {
                // 깨어있을 때만 SafePoint 처리
                character.SetRoomLimits(roomCenterX[idx], roomWidth[idx]);
                character.TeleportToRoom(idx);

                if (holdCharacterAfterSnap)
                {
                    //  sing 등으로 호출된 경우: 자동 이동만 멈추고 고정
                    character.HoldPosition();
                    holdCharacterAfterSnap = false;
                }
                else
                {
                    // 기존 스크롤 스냅 로직: 원래대로 풀어줌
                    character.Unfreeze();
                }
            }
        }

    }


    // ===== ScrollHandle 동기화 =====
    private void UpdateScrollHandle()
    {
        if (roomCenterX == null || roomCenterX.Length == 0) return;

        float camX = cam.transform.position.x;

        // roomCenterX[0] ~ roomCenterX[last]를 전체 구간으로 보고 보간
        float t = Mathf.InverseLerp(
    roomCenterX[0] + cameraRoomOffsetX,
    roomCenterX[roomCenterX.Length - 1] + cameraRoomOffsetX,
    camX
);
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
        p.x = roomCenterX[idx] + cameraRoomOffsetX;
        cam.transform.position = p;
    }


    // 캐릭터를 그 방 SafePoint에 고정(자동 이동 멈춤)
    public void MoveForAction()
    {
        holdCharacterAfterSnap = true;   // 스냅 끝나면 Unfreeze 대신 HoldPosition 하도록
        isDragging = false;              // 드래그 상태 강제 해제

        if (snapRoutine != null)
            StopCoroutine(snapRoutine);

        // 캐릭터가 있으면 일단 자동 이동만 멈춤
        if (character != null && !character.IsSleeping())
        {
            character.HoldPosition();
            // 스크롤 드래그 때처럼 카메라에 붙여둘지 여부:
            // sing 연출에서 "카메라 이동 중 캐릭터가 화면에서 안정적으로 보이게" 하려면 붙이는 게 안전함
            character.transform.SetParent(cam.transform, true);
        }

        // 지금 카메라 X 위치에서 가장 가까운 방 찾고 스냅
        FindNearestRoomAndSnap();
    }

}
