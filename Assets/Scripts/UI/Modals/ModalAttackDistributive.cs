using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ModalAttackDistributive : M8.ModalController, M8.IModalPush, M8.IModalPop {
    [Header("Templates")]
    public AreaOperationCellWidget areaCellTemplate;
    public int areaCellCapacity = 4;

    public DigitWidget digitFixedTemplate;
    public int digitFixedCapacity = 3;

    public OpWidget opTemplate;
    public int opCapacity = 3;

    public Transform cacheRoot;

    [Header("Digit Group")]
    public DigitGroupWidget digitGroupTop;
    public DigitGroupWidget digitGroupLeft;

    [Header("Area Info")]
    public AreaGridControl[] areaGrids;

    [Header("Area Cell Anchors")]
    public string areaCellAnchorTop = "top";
    public string areaCellAnchorLeft = "left";
    public string areaCellAnchorOpTop = "opTop";
    public string areaCellAnchorOpLeft = "opLeft";

    [Header("Buttons")]
    public Button proceedButton;

    private AreaOperation mAreaOp;
    private int mMistakeCount;

    private int mAreaGridInd = -1;

    private M8.CacheList<AreaOperationCellWidget> mAreaCellActives;
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private List<DigitWidget> mDigitFixedHorizontalActives;
    private List<DigitWidget> mDigitFixedVerticalActives;
    private M8.CacheList<DigitWidget> mDigitFixedCache;

    private M8.CacheList<OpWidget> mOpActives;
    private M8.CacheList<OpWidget> mOpCache;

    private bool mIsInit;

    public void Back() {

    }

    public void Proceed() {

    }
    
    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            ///////////////////////////////////
            //generate caches from templates

            //area cells
            mAreaCellActives = new M8.CacheList<AreaOperationCellWidget>(areaCellCapacity);
            mAreaCellCache = new M8.CacheList<AreaOperationCellWidget>(areaCellCapacity);

            for(int i = 0; i < areaCellCapacity; i++) {
                var newAreaCell = Instantiate(areaCellTemplate);
                newAreaCell.Init();
                newAreaCell.clickCallback += OnAreaOpCellClick;

                newAreaCell.rectTransform.SetParent(cacheRoot, false);

                mAreaCellCache.Add(newAreaCell);
            }

            //digits
            mDigitFixedHorizontalActives = new List<DigitWidget>(digitFixedCapacity);
            mDigitFixedVerticalActives = new List<DigitWidget>(digitFixedCapacity);
            mDigitFixedCache = new M8.CacheList<DigitWidget>(digitFixedCapacity);

            for(int i = 0; i < digitFixedCapacity; i++) {
                var newDigit = Instantiate(digitFixedTemplate);
                newDigit.Init(-1);

                newDigit.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(newDigit);
            }

            //ops
            mOpActives = new M8.CacheList<OpWidget>(opCapacity);
            mOpCache = new M8.CacheList<OpWidget>(opCapacity);

            for(int i = 0; i < opCapacity; i++) {
                var newOp = Instantiate(opTemplate);

                newOp.rectTransform.SetParent(cacheRoot, false);

                mOpCache.Add(newOp);
            }

            //////////////////////////////////
            //setup digit groups

            digitGroupTop.Init();
            digitGroupTop.clickCallback += OnDigitGroupHorizontalClick;

            digitGroupLeft.Init();
            digitGroupLeft.clickCallback += OnDigitGroupVerticalClick;

            //////////////////////////////////
            //setup area grids

            for(int i = 0; i < areaGrids.Length; i++) {
                var areaGrid = areaGrids[i];

                areaGrid.Init();
                areaGrid.gameObject.SetActive(false);
            }

            mIsInit = true;
        }

        //setup shared data across attack phases
        mAreaOp = ModalAttackParm.GetAreaOperation(parms);
        mMistakeCount = ModalAttackParm.GetMistakeCount(parms);

        if(mAreaOp != null) {
            //choose the area grid to work with
            AreaGridControl curAreaGrid = null;
            mAreaGridInd = -1;

            for(int i = 0; i < areaGrids.Length; i++) {
                var areaGrid = areaGrids[i];
                if(areaGrid && areaGrid.numRow == mAreaOp.areaRowCount && areaGrid.numCol == mAreaOp.areaColCount) {
                    curAreaGrid = areaGrid;
                    mAreaGridInd = i;
                    break;
                }
            }

            if(curAreaGrid) {
                curAreaGrid.Init();

                //setup initial grid telemetry
                for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                    for(int col = 0; col < mAreaOp.areaColCount; col++) {
                        var cell = mAreaOp.GetAreaOperation(row, col);
                        if(cell.isValid)
                            curAreaGrid.SetAreaScale(row, col, Vector2.one);
                        else
                            curAreaGrid.SetAreaScale(row, col, Vector2.zero);
                    }
                }

                curAreaGrid.RefreshAreas();

                //insert area widgets
                var gridRoot = curAreaGrid.rectTransform;

                for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                    for(int col = 0; col < mAreaOp.areaColCount; col++) {
                        var cell = mAreaOp.GetAreaOperation(row, col);

                        var areaCellWidget = GenerateAreaCell(row, col);

                        areaCellWidget.interactable = false;
                        areaCellWidget.solvedVisible = false;

                        //TODO: setup panel color

                        areaCellWidget.rectTransform.SetParent(gridRoot, false);

                        if(cell.isValid) {
                            areaCellWidget.ApplyCell(cell, false);

                            curAreaGrid.ApplyArea(row, col, areaCellWidget.rectTransform);

                            areaCellWidget.operationVisible = true;
                            areaCellWidget.gameObject.SetActive(true);
                        }
                        else {
                            areaCellWidget.operationVisible = false;
                            areaCellWidget.gameObject.SetActive(false);
                        }
                    }
                }

                //setup digit groups

                //insert digits
            }
            else
                Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");
        }
    }

    void M8.IModalPop.Pop() {
        ClearAreaCells();
        ClearDigits();
        ClearOps();

        if(mAreaGridInd != -1) {
            if(areaGrids[mAreaGridInd])
                areaGrids[mAreaGridInd].gameObject.SetActive(false);

            mAreaGridInd = -1;
        }
    }

    void OnDigitGroupHorizontalClick(int digitIndex) {

    }

    void OnDigitGroupVerticalClick(int digitIndex) {

    }

    void OnAreaOpCellClick(AreaOperationCellWidget areaCellWidget) {

    }

    private AreaOperationCellWidget GenerateAreaCell(int row, int col) {
        if(mAreaCellCache.Count == 0)
            return null;

        var newAreaCellWidget = mAreaCellCache.RemoveLast();

        newAreaCellWidget.SetCellIndex(row, col);

        mAreaCellActives.Add(newAreaCellWidget);

        return newAreaCellWidget;
    }

    private void ClearAreaCells() {
        for(int i = 0; i < mAreaCellActives.Count; i++) {
            var areaCellWidget = mAreaCellActives[i];

            areaCellWidget.rectTransform.SetParent(cacheRoot, false);

            mAreaCellCache.Add(areaCellWidget);
        }

        mAreaCellActives.Clear();
    }

    private void ClearDigits() {
        for(int i = 0; i < mDigitFixedHorizontalActives.Count; i++) {
            var digitWidget = mDigitFixedHorizontalActives[i];

            digitWidget.rectTransform.SetParent(cacheRoot, false);

            mDigitFixedCache.Add(digitWidget);
        }

        mDigitFixedHorizontalActives.Clear();

        for(int i = 0; i < mDigitFixedVerticalActives.Count; i++) {
            var digitWidget = mDigitFixedVerticalActives[i];

            digitWidget.rectTransform.SetParent(cacheRoot, false);

            mDigitFixedCache.Add(digitWidget);
        }

        mDigitFixedVerticalActives.Clear();
    }

    private void ClearOps() {
        for(int i = 0; i < mOpActives.Count; i++) {
            var opWidget = mOpActives[i];

            opWidget.rectTransform.SetParent(cacheRoot, false);

            mOpCache.Add(opWidget);
        }

        mOpActives.Clear();
    }
}
