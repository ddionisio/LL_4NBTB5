using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

using TMPro;

public class AreaOperationCellFactorDragWidget : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler {
    public enum FactorSideType {
        Left,
        Right
    }

    [Header("Factor Data")]
    public FactorSideType factorSide;
    public Transform factorAnchorLeft;
    public Transform factorAnchorRight;

    [Header("Drag/Drop")]
    public RectTransform dragRoot;
    public Vector2 dragOfs;
    public TMP_Text dragLabel;
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

    public Transform factorAnchor {
        get {
            switch(factorSide) {
                case FactorSideType.Left:
                    return factorAnchorLeft;
                case FactorSideType.Right:
                    return factorAnchorRight;
            }

            return null;
        }
    }

    public bool isMoving { get { return mMoveRout != null; } }

    private AreaOperationCellWidget mAreaOpWidget;

    private DG.Tweening.EaseFunction mDropMoveEaseFunc;

    private Coroutine mMoveRout;

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

        mAreaOpWidget = GetComponentInParent<AreaOperationCellWidget>(true);

        mDropMoveEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(dropMoveEase);
    }

    void OnApplicationFocus(bool isActive) {
        if(!isActive) {
            if(isDragging)
                DragEnd();
        }
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

        //check drop
        AreaOperationCellFactorDragWidget otherDragWidget = null;

        if(eventData.pointerDrag && eventData.pointerDrag != gameObject)
            otherDragWidget = eventData.pointerDrag.GetComponent<AreaOperationCellFactorDragWidget>();

        if(otherDragWidget) {
            //switch factors
            var otherCellNumber = otherDragWidget.number;

            otherDragWidget.number = number;

            number = otherCellNumber;

            dragRoot.position = otherDragWidget.factorAnchor.position;
        }

        StartMoveBackToOrigin();
    }

    IEnumerator DoMoveBackToOrigin() {
        Vector2 startPos = dragRoot.position;
        Vector2 endPos = factorAnchor.position;

        var dist = (endPos - startPos).magnitude;

        var delay = dropMoveSpeed > 0f ? dist / dropMoveSpeed : 0f;

        var curTime = 0f;

        while(curTime < delay) {
            yield return null;

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

            dragRoot.position = factorAnchor.position;

            dragRoot.gameObject.SetActive(true);

            SetFactorNumberVisible(false);

            isDragging = true;
        }
    }

    private void DragUpdate(PointerEventData eventData) {
        Vector2 cursorPos = eventData.position;

        cursorPos += dragOfs;

        dragRoot.position = cursorPos;
    }

    private void DragEnd() {
        if(isDragging) {
            if(dragRoot)
                dragRoot.gameObject.SetActive(false);

            SetFactorNumberVisible(true);

            isDragging = false;
        }
    }
}