using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMove : MonoBehaviour
{

    [Header("이동 속도 (픽셀/초 단위)")]
    public float moveSpeed = 30f;
    public int pixelsPerUnit = 16;

    [Header("상태 지속 시간 범위")]
    public Vector2 idleTimeRange = new Vector2(2f, 3f);
    public Vector2 walkTimeRange = new Vector2(3f, 5f);

    private float timer = 0f;
    private float decisionTime = 2f;
    private int direction = 0; // -1: 왼쪽, 0: 멈춤, 1: 오른쪽
    private Vector2 moveDir = Vector2.zero;

    [Header("애니메이션")]
    public Animator anim;
    public SpriteRenderer hshSprite;
    public float idleAnimSpeed = 0.2f;
    public float walkAnimSpeed = 0.2f;

    [Header("벽 / 바닥 / 천장 위치")]
    public float leftLimit = -3f;
    public float rightLimit = 3f;
    public float bottomLimit = -1.5f;    // 캐릭터가 내려갈 수 있는 최소 Y
    public float topLimit = 1.5f;        // 캐릭터가 올라갈 수 있는 최대 Y

    [Header("방별 안전 위치")]
    [SerializeField] private Transform[] roomSafePoints;

    [Header("수면 위치")]
    [SerializeField] public Transform bedPosition;

    [Header("Collider 참조")]
    public BoxCollider2D clickCollider;   // 부모 캐릭터에 큰 collider
    public BoxCollider2D footCollider;    // 자식 sprite 발 collider

    private bool isSleeping = false;
    private bool isMoving = true;

    [Header("입력 체크")]
    private float lastInputTime = 0f;
    public float sleepAfterSeconds = 5f; // 입력 없으면 N초 뒤에 잠듦


    // Start is called before the first frame update
    void Start()
    {
        //anim = GetComponent<Animator>();
        // hshSprite = GetComponent<SpriteRenderer>();
        DecideDirection();
        SetStandingCollider(); //초기 상태는 서 있기 (나중에 자고 있는 거로 바꿀때 여기 바꾸기)
        isMoving = true;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        // 자동 수면 체크
        if (!isSleeping && Time.time - lastInputTime >= sleepAfterSeconds)
        {
            GoToSleepAuto();
        }


        // 걷기/Idle 로직
        if (timer >= decisionTime)
        {
            timer = 0f;
            DecideDirection(); //-1, 0, 1중 하나
        }

        //실제 이동
        Move();

    }


    void Move()
    {

        //잘 땐 안 움직임
        if (isSleeping || !isMoving) return;

        float step = (moveSpeed / (float)pixelsPerUnit) * Time.deltaTime;

        if (IsBlocked(moveDir))
        {
            // 충돌했으면 멈추고 방향 다시 골라
            moveDir = Vector2.zero;
            return;
        }

        Vector3 nextPos = transform.position + (Vector3)(moveDir.normalized * step);

        // 벽 범위 제한 (좌우)
        nextPos.x = Mathf.Clamp(nextPos.x, leftLimit, rightLimit);
        nextPos.y = Mathf.Clamp(nextPos.y, bottomLimit, topLimit);
        transform.position = nextPos;
    }


    void DecideDirection()
    {
        if (isSleeping) return;

        // 70% 확률로 기존 방향 유지
        if (moveDir != Vector2.zero && Random.value < 0.7f)
        {
            // 기존 방향 유지 + 약간의 랜덤(흔들림)
            Vector2 jitter = new Vector2(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.1f, 0.1f) // Y 방향 약하게
            );
            moveDir = (moveDir + jitter).normalized;
        }
        else
        {
            // 새 방향 고르기 : 좌/우 이동 비중 강화, 위아래 가중치↓
            Vector2[] dirs = {
            new Vector2(-1, 0),  // 왼
            new Vector2(1, 0),   // 오른
            new Vector2(-1, 0.3f),
            new Vector2(1, 0.3f),
            new Vector2(-1, -0.3f),
            new Vector2(1, -0.3f),
            Vector2.zero // Idle 하나만
        };

            moveDir = dirs[Random.Range(0, dirs.Length)];
        }

        // 이동/Idle 지속시간 조정
        if (moveDir == Vector2.zero)
            decisionTime = Random.Range(idleTimeRange.x, idleTimeRange.y);
        else
            decisionTime = Random.Range(walkTimeRange.x * 1.3f, walkTimeRange.y * 1.6f); // ← 걷기 더 오래
    }


    //스와이프 중 정지
    public void Freeze()
    {
        isMoving = false;
        moveDir = Vector2.zero; // ★ 이동 방향 초기화
        if (anim != null)
            anim.speed = 0f;

        if (clickCollider != null)
            clickCollider.enabled = false;
        if (footCollider != null)
            footCollider.enabled = false;
    }

    public void Unfreeze()
    {
        isMoving = true;
        if (anim != null)
        {
            anim.speed = 1f;
        }

        // collider는 상태에 따라 분기
        if (!isSleeping)
            SetStandingCollider();
        else
            SetSleepingCollider();

    }


    // 스냅 후 해당 방 안전 위치로 순간 이동
    public void TeleportToRoom(int roomIndex)
    {
        // 현재 카메라 기준으로 X는 중앙에 고정
        Camera cam = Camera.main;
        if (cam == null) return;

        float targetX = cam.transform.position.x;

        // 방마다 Y 위치만 safePoint에서 가져오고 싶다면:
        float targetY = transform.position.y;

        if (roomSafePoints != null &&
            roomIndex >= 0 &&
            roomIndex < roomSafePoints.Length &&
            roomSafePoints[roomIndex] != null)
        {
            targetY = roomSafePoints[roomIndex].position.y;
        }

        transform.position = new Vector3(targetX, targetY, transform.position.z);

        moveDir = Vector2.zero;
    }

    // ★★ 방이 바뀔 때마다, 그 방 기준으로 이동 가능 범위를 다시 설정하는 함수
    public void SetRoomLimits(float centerX, float roomWidth)
    {
        float half = roomWidth * 0.5f;
        leftLimit = centerX - half;
        rightLimit = centerX + half;
    }

    // =====================
    //    Sleep / Wake
    // =====================
    public void GoToSleep()
    {
        isSleeping = true;
        direction = 0;                  // 이동 멈추기
        SetSleepingCollider();
        anim.SetTrigger("Sleep");
    }

    private void GoToSleepAuto()
    {
        //안건드리면 자동으로 자러 감
        isSleeping = true;
        direction = 0;
        SetSleepingCollider();
        anim.SetTrigger("Sleep");

    }


    //Sleep 상태 유지 조건 (이건 나중에 일정 시간 지나면 알아서 깨게?)
    IEnumerator SleepRoutine()
    {
        GoToSleep();
        yield return new WaitForSeconds(10f);
        //StartCoroutine(WakeUpRoutine());
    }


    public void WakeUp()
    {
        if (!isSleeping) return;

        anim.SetTrigger("Wake");
        StartCoroutine(WakeUpRoutine());
    }

    IEnumerator WakeUpRoutine()
    {
        // isSleeping 유지
        AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0);

        // "WakeUp" 클립 재생될 때까지 대기
        while (!info.IsName("WakeUp"))
        {
            yield return null;
            info = anim.GetCurrentAnimatorStateInfo(0);
        }


        // WakeUp 클립이 끝날 때까지 대기
        float clipLength = info.length;

        yield return new WaitForSeconds(clipLength);

        // 여기서 자연스럽게 딱 끝나고 Idle로 넘어가기 직전
        transform.position = new Vector3(0, -0.72f, 0);
        SetStandingCollider();   // ← 깨어날 때 collider 원복
        isSleeping = false;
    }
    void OnMouseDown()
    {
        lastInputTime = Time.time; // 입력 발생 → 시간 초기화
        WakeUp();
    }


    // =====================
    //   Collider 설정
    // =====================

    private void SetStandingCollider()
    {
        if (footCollider != null)
            footCollider.enabled = true;     // 작은 hitbox 오프
        if (clickCollider != null)
            clickCollider.enabled = false;   // 큰 클릭 콜라이더 비활성
    }

    private void SetSleepingCollider()
    {
        if (footCollider != null)
            footCollider.enabled = false;    // 충돌 필요 없음
        if (clickCollider != null)
            clickCollider.enabled = true;    // 전신 클릭 가능
    }

    bool IsBlocked(Vector2 dir)
    {
        if (footCollider == null) return false;

        float distance = 0.2f; // 발 콜라이더 사이즈에 맞게 늘림

        RaycastHit2D hit = Physics2D.Raycast(
            footCollider.bounds.center,
            dir,
            distance,
            LayerMask.GetMask("Furniture")
        );

        return hit.collider != null;
    }

    //--- 아래는 애니메이션 이벤트 용 ----

    public void ForceMoveToBed()
    {
        if (bedPosition == null) return;

        transform.position = new Vector3(
            bedPosition.position.x,
            bedPosition.position.y + 1.38f,
            transform.position.z
        );
    }

    public void WakeUpPlace()
    {
        transform.position = new Vector3(0, -0.72f, 0);
    }

}
