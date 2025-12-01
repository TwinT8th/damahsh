using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FeedingManager : MonoBehaviour
{
    //StatusManager 연결
    public StatusManager statusManager;


    /// <summary>
    /// 음식 UI 버튼에서 호출할 함수
    /// </summary>

    public void Feed(int amount)
    {
        statusManager.AddStatus(StatusManager.StatusType.Hunger, amount);
        Debug.Log($"Feed! Hunger +{amount}");
    }
}
