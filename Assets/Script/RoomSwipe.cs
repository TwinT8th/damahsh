using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class RoomSwipe : MonoBehaviour, IDragHandler, IEndDragHandler
{

    [Header("배경 그룹")]
    public Transform background;   // BackgroundGroup 참조

    [Header("패럴럭스 레이어 (선택)")]
    public Transform parallaxLayer;
    public float parallaxRatio = 0.4f;

    [Header("방 개수& 간격")]
    public int totalRooms = 3;
    public float roomWidth = 4f; // 방 x 간격

    [Header("스와이프 감도")]
    public float dragSpeed = 0.0025f;

    private float currentX = 0f;
    private int currentRoomIndex = 0;


    public void OnDrag(PointerEventData eventData)
    {
        float delta = eventData.delta.x * dragSpeed;
        currentX += delta;

        background.localPosition += new Vector3(delta, 0, 0);

        if (parallaxLayer != null)
        {
            parallaxLayer.localPosition += new Vector3(delta * parallaxRatio, 0, 0);
        }
    }


    public void OnEndDrag(PointerEventData data)
    {
        float targetX = currentRoomIndex * -roomWidth;

        float diff = background.localPosition.x - targetX;

        if (Mathf.Abs(diff) > roomWidth * 0.3f)
        {
            if (diff > 0) currentRoomIndex--;
            else currentRoomIndex++;
        }

        currentRoomIndex = Mathf.Clamp(currentRoomIndex, 0, totalRooms - 1);

        StartCoroutine(SnapToRoom());
    }

    IEnumerator SnapToRoom()
    {
        Vector3 targetPos = new Vector3(-currentRoomIndex * roomWidth, background.localPosition.y, 0);
        Vector3 startPos = background.localPosition;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            background.localPosition = Vector3.Lerp(startPos, targetPos, t);

            if (parallaxLayer != null)
            {
                Vector3 pTarget = new Vector3(targetPos.x * parallaxRatio, parallaxLayer.localPosition.y, 0);
                parallaxLayer.localPosition = Vector3.Lerp(parallaxLayer.localPosition, pTarget, t);
            }

            yield return null;
        }
    }
}