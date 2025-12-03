using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollHandleDrag : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public ScrollManager scrollManager; // Inspector에서 연결

    public void OnBeginDrag(PointerEventData eventData)
    {
        scrollManager.StartManualDrag();
    }

    public void OnDrag(PointerEventData eventData)
    {
        scrollManager.OnManualDrag(eventData.delta.x);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        scrollManager.EndManualDrag();
    }
}
