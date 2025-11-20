using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteSorter : MonoBehaviour
{
    [Header("씬 전체 기준값")]
    public int sortingOrderBase = 0;

    [Header("오브젝트별 보정값")]
    public int offset = 0;

    [Header("한 번만 계산 (가구 등 고정 오브젝트)")]
    public bool runOnlyOnce = false;

    private SpriteRenderer sr;
    private Collider2D col;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();   // 있으면 사용, 없으면 transform.position 사용
    }

    private void LateUpdate()
    {
        UpdateSortingOrder();

        if (runOnlyOnce)
            enabled = false;
    }

    private void UpdateSortingOrder()
    {
        float y;

        if (col != null)
            y = col.bounds.min.y;     // ★ 발바닥/바닥 기준
        else
            y = transform.position.y; // 콜라이더 없으면 위치 기준

        sr.sortingOrder = sortingOrderBase - Mathf.RoundToInt(y * 100) + offset;
    }
}