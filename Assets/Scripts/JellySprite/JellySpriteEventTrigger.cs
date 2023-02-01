using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Added per reference point
/// </summary>
[AddComponentMenu("")]
public class JellySpriteEventTrigger : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerClickHandler,
    IInitializePotentialDragHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IScrollHandler,
    IUpdateSelectedHandler,
    ISelectHandler,
    IDeselectHandler,
    IMoveHandler,
    ISubmitHandler,
    ICancelHandler {

    public JellySprite jellySprite { get; set; }
    public int index { get; set; }

    public event System.Action<JellySprite, int, PointerEventData> pointerEnterCallback;
    public event System.Action<JellySprite, int, PointerEventData> pointerExitCallback;
    public event System.Action<JellySprite, int, PointerEventData> pointerDownCallback;
    public event System.Action<JellySprite, int, PointerEventData> pointerUpCallback;
    public event System.Action<JellySprite, int, PointerEventData> pointerClickCallback;
    public event System.Action<JellySprite, int, PointerEventData> initializePotentialDragCallback;
    public event System.Action<JellySprite, int, PointerEventData> beginDragCallback;
    public event System.Action<JellySprite, int, PointerEventData> dragCallback;
    public event System.Action<JellySprite, int, PointerEventData> endDragCallback;
    public event System.Action<JellySprite, int, PointerEventData> dropCallback;
    public event System.Action<JellySprite, int, PointerEventData> scrollCallback;
    public event System.Action<JellySprite, int, BaseEventData> updateSelectedCallback;
    public event System.Action<JellySprite, int, BaseEventData> selectCallback;
    public event System.Action<JellySprite, int, BaseEventData> deselectCallback;
    public event System.Action<JellySprite, int, AxisEventData> moveCallback;
    public event System.Action<JellySprite, int, BaseEventData> submitCallback;
    public event System.Action<JellySprite, int, BaseEventData> cancelCallback;

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        pointerEnterCallback?.Invoke(jellySprite, index, eventData);
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        pointerExitCallback?.Invoke(jellySprite, index, eventData);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
        pointerDownCallback?.Invoke(jellySprite, index, eventData);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
        pointerUpCallback?.Invoke(jellySprite, index, eventData);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        pointerClickCallback?.Invoke(jellySprite, index, eventData);
    }

    void IInitializePotentialDragHandler.OnInitializePotentialDrag(PointerEventData eventData) {
        initializePotentialDragCallback?.Invoke(jellySprite, index, eventData);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
        beginDragCallback?.Invoke(jellySprite, index, eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {
        dragCallback?.Invoke(jellySprite, index, eventData);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
        endDragCallback?.Invoke(jellySprite, index, eventData);
    }

    void IDropHandler.OnDrop(PointerEventData eventData) {
        dropCallback?.Invoke(jellySprite, index, eventData);
    }

    void IScrollHandler.OnScroll(PointerEventData eventData) {
        scrollCallback?.Invoke(jellySprite, index, eventData);
    }

    void IUpdateSelectedHandler.OnUpdateSelected(BaseEventData eventData) {
        updateSelectedCallback?.Invoke(jellySprite, index, eventData);
    }

    void ISelectHandler.OnSelect(BaseEventData eventData) {
        selectCallback?.Invoke(jellySprite, index, eventData);
    }

    void IDeselectHandler.OnDeselect(BaseEventData eventData) {
        deselectCallback?.Invoke(jellySprite, index, eventData);
    }

    void IMoveHandler.OnMove(AxisEventData eventData) {
        moveCallback?.Invoke(jellySprite, index, eventData);
    }

    void ISubmitHandler.OnSubmit(BaseEventData eventData) {
        submitCallback?.Invoke(jellySprite, index, eventData);
    }

    void ICancelHandler.OnCancel(BaseEventData eventData) {
        cancelCallback?.Invoke(jellySprite, index, eventData);
    }
}
