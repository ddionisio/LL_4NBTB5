using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using TMPro;

public class AreaOperationCellFactorDragWidget : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler {
    public enum FactorSideType {
        Left,
        Right
    }

    [Header("Factor Data")]
    public FactorSideType factorSide;
    public Transform factorAnchor;

    [Header("Hover")]
    public GameObject hoverHighlightGO;

    [Header("Drag/Drop")]
    public RectTransform dragRoot;
    public Vector2 dragOfs;
    public TMP_Text dragLabel;
    public GameObject dropHighlightGO;
    public float dropMoveSpeed = 100;
    public DG.Tweening.Ease dropMoveEase = DG.Tweening.Ease.OutSine;

    public AreaOperationCellWidget areaOpWidget { get { return mAreaOpWidget; } }

    public bool isDragging { get; private set; }

    public int number {
        get {
            switch(factorSide) {
                case FactorSideType.Left:
                    return areaOpWidget.cellData.op.operand1;
                case FactorSideType.Right:
                    return areaOpWidget.cellData.op.operand2;
            }

            return 0;
        }

        set {
            var cell = areaOpWidget.cellData;

            switch(factorSide) {
                case FactorSideType.Left:
                    cell.op.operand1 = value;
                    break;
                case FactorSideType.Right:
                    cell.op.operand2 = value;
                    break;
            }

            areaOpWidget.ApplyCell(cell, false);
        }
    }

    public bool isMoving { get { return mMoveRout != null; } }

    public bool dropHighlightActive { 
        get {
            if(dropHighlightGO)
                return dropHighlightGO.activeSelf;
            return false;
        }

        set {
            if(dropHighlightGO)
                dropHighlightGO.SetActive(value);
        }
    }

    private AreaOperationCellWidget mAreaOpWidget;

    private DG.Tweening.EaseFunction mDropMoveEaseFunc;

    private Coroutine mMoveRout;

    private AreaOperationCellFactorDragWidget mHoverCellDragWidget;

    private bool mIsPointerEnter;

    public void SetFactorNumberVisible(bool visible) {
        switch(factorSide) {
            case FactorSideType.Left:
                if(areaOpWidget)
                    areaOpWidget.factorLeftVisible = visible;
                break;
            case FactorSideType.Right:
                if(areaOpWidget)
                    areaOpWidget.factorRightVisible = visible;
                break;
        }
    }

    void Awake() {
        dragRoot.gameObject.SetActive(false);

        dropHighlightActive = false;

        mAreaOpWidget = GetComponentInParent<AreaOperationCellWidget>(true);

        mDropMoveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(dropMoveEase);
    }

    void OnApplicationFocus(bool isActive) {
        if(!isActive) {
            mIsPointerEnter = false;

            if(isDragging)
                DragEnd();
            else
                RefreshHoverHighlight();
        }
    }

    void OnEnable() {
        mIsPointerEnter = false;
        RefreshHoverHighlight();
    }

    void OnDisable() {
        StopMove();
        DragEnd();
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData) {
        DragStart();

        DragUpdate(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {
        if(!isDragging)
            return;

        DragUpdate(eventData);
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData) {
        if(!isDragging)
            return;

        DragUpdate(eventData);

        //check drop
        if(mHoverCellDragWidget) {
            //apply factor swap
            if(mHoverCellDragWidget != this) {
                var otherCellNumber = mHoverCellDragWidget.number;

                mHoverCellDragWidget.number = number;

                number = otherCellNumber;

                dragLabel.text = otherCellNumber.ToString();

                dragRoot.position = mHoverCellDragWidget.factorAnchor.position;
            }

            mHoverCellDragWidget.dropHighlightActive = false;
            mHoverCellDragWidget = null;
        }

        StartMoveBackToOrigin();
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData) {
        mIsPointerEnter = true;
        RefreshHoverHighlight();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData) {
        mIsPointerEnter = false;
        RefreshHoverHighlight();
    }

    IEnumerator DoMoveBackToOrigin() {
        Vector2 startPos = dragRoot.position;
        Vector2 endPos = factorAnchor.position;

        var dist = (endPos - startPos).magnitude;

        var delay = dropMoveSpeed > 0f ? dist / dropMoveSpeed : 0f;

        var curTime = 0f;

        while(curTime < delay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mDropMoveEaseFunc(curTime, delay, 0f, 0f);

            dragRoot.position = Vector2.Lerp(startPos, endPos, t);
        }

        mMoveRout = null;

        DragEnd();
    }

    private void StartMoveBackToOrigin() {
        if(mMoveRout != null)
            StopCoroutine(mMoveRout);

        mMoveRout = StartCoroutine(DoMoveBackToOrigin());
    }

    private void StopMove() {
        if(mMoveRout != null) {
            StopCoroutine(mMoveRout);
            mMoveRout = null;
        }
    }

    private void DragStart() {
        if(!isDragging) {
            StopMove();

            dragLabel.text = number.ToString();

            var dragParent = DragHolder.isInstantiated ? DragHolder.instance.dragRoot : null;
            if(dragParent)
                dragRoot.SetParent(dragParent, false);

            dragRoot.position = factorAnchor.position;

            dragRoot.gameObject.SetActive(true);

            SetFactorNumberVisible(false);

            isDragging = true;

            RefreshHoverHighlight();
        }
    }

    private void DragUpdate(PointerEventData eventData) {
        Vector2 cursorPos = eventData.position;

        cursorPos += dragOfs;

        dragRoot.position = cursorPos;

        //update highlight
        AreaOperationCellFactorDragWidget pointerDragWidget = null;

        var pointerGO = eventData.pointerCurrentRaycast.gameObject;

        if(pointerGO) {
            if(pointerGO == gameObject)
                pointerDragWidget = this;
            else if(mHoverCellDragWidget && mHoverCellDragWidget.gameObject == pointerGO)
                pointerDragWidget = mHoverCellDragWidget;
            else
                pointerDragWidget = pointerGO.GetComponent<AreaOperationCellFactorDragWidget>();
        }

        if(mHoverCellDragWidget != pointerDragWidget) {
            if(mHoverCellDragWidget)
                mHoverCellDragWidget.dropHighlightActive = false;

            mHoverCellDragWidget = pointerDragWidget;
            if(mHoverCellDragWidget)
                mHoverCellDragWidget.dropHighlightActive = true;
        }
    }

    private void DragEnd() {
        if(isDragging) {
            if(mHoverCellDragWidget) {
                mHoverCellDragWidget.dropHighlightActive = false;
                mHoverCellDragWidget = null;
            }

            if(dragRoot)
                dragRoot.gameObject.SetActive(false);

            SetFactorNumberVisible(true);

            isDragging = false;

            RefreshHoverHighlight();
        }
    }

    private void RefreshHoverHighlight() {
        if(hoverHighlightGO)
            hoverHighlightGO.SetActive(mIsPointerEnter && !isDragging);
    }
}