using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusManager : MonoBehaviour
{
    // 어떤 상태인지 구분하는 용도 (열거형 - Enum)
    public enum StatusType {  Hunger, Fun, Lone }

    // -------- 각 Status 별 개별 세팅 구조 --------
    [System.Serializable]
    public class StatusData
    {
        [Header("상태 종류 (배고픔, 즐거움, 외로움)")]
        public StatusType type;

        [Header("UI에 표시될 하트 이미지들 (4개)")]
        public Image[] hearts;

        [Header("현재 상태 수치 (0~100)")]
        [Range(0, 100)] public float value = 100f;

        [Header("시간으로 감소되는 설정")]
        public float decreaseAmount = 5f;      // 한 번 감소되는 양 (%)
        public float decreaseInterval = 10f;   // 감소 주기 (초)

        [HideInInspector]
        public float timer = 0f;               // 내부에서 시간 측정용
    }


    [Header("각 Status 데이터")]
    public  StatusData[] statusList;

    private const int maxHeartCount = 4;
    private const float valuePerHeart = 25f;

    

    // Start is called before the first frame update
    void Start()
    {
        // 게임 시작할 때 UI 즉시 갱신
        foreach (var status in statusList)
            UpdateStatusUI(status);
    }

    void Update()
    {
        // 각 상태마다 개별적으로 시간 기반 감소 처리
        foreach (var status in statusList)
        {
            status.timer += Time.deltaTime;

            // 설정한 초가 지나면 자동 감소
            if (status.timer >= status.decreaseInterval)
            {
                status.timer = 0f;
                AddStatus(status.type, -status.decreaseAmount);
            }
        }
    }


    /// <summary>
    /// 특정 상태 값 증가/감소 + UI 업데이트
    /// 예) 음식 → Hunger +20, 놀기 → Fun +10 등
    /// </summary>
    public void AddStatus(StatusType type, float amount)
    {
        var st = GetStatus(type);

        st.value = Mathf.Clamp(st.value + amount, 0f, 100f);
        UpdateStatusUI(st);
    }

    /// <summary>
    /// 배열에서 필요한 StatusData 찾아 반환
    /// </summary>
    private StatusData GetStatus(StatusType type)
    {
        foreach (var st in statusList)
            if (st.type == type)
                return st;

        Debug.LogError($"Status {type} 설정되지 않음!");
        return null;
    }

    /// <summary>
    /// UI 하트 개별 FillAmount 계산 및 반영
    /// </summary>
    private void UpdateStatusUI(StatusData st)
    {
        // 현재 수치를 0~1로 환산
        float normalized = st.value / 100f;

        for (int i = 0; i < st.hearts.Length; i++)
        {
            // i번째 하트가 표현해야 할 값의 시작 구간
            float threshold = (i * valuePerHeart) / 100f;

            // threshold 이상이면 fill, 아니면 줄어듦
            float heartFill = Mathf.Clamp01((normalized - threshold) * (100f / valuePerHeart));

            st.hearts[i].fillAmount = heartFill;
        }
    }

}
