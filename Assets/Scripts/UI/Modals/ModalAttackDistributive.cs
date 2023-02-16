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

    public AreaOperationCellWidget mainAreaCellWidget {
        get {
            if(mAreaOp == null)
                return null;

            return mAreaCellActives[mAreaOp.areaRowCount - 1, mAreaOp.areaColCount - 1];
        }
    }

    private AreaOperation mAreaOp;
    private int mMistakeCount;

    private int mAreaGridInd = -1;

    private AreaOperationCellWidget[,] mAreaCellActives; //[row, col], count is based on row and col count in mAreaOp
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private DigitWidget[] mDigitFixedHorizontalActives; //count is based on mAreaOp's col count
    private DigitWidget[] mDigitFixedVerticalActives; //count is based on mAreaOp's row count
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
            mAreaCellActives = new AreaOperationCellWidget[areaCellCapacity, areaCellCapacity];
            mAreaCellCache = new M8.CacheList<AreaOperationCellWidget>(areaCellCapacity);

            for(int i = 0; i < areaCellCapacity; i++) {
                var newAreaCell = Instantiate(areaCellTemplate);
                newAreaCell.Init();
                newAreaCell.clickCallback += OnAreaOpCellClick;

                newAreaCell.rectTransform.SetParent(cacheRoot, false);

                mAreaCellCache.Add(newAreaCell);
            }

            //digits
            mDigitFixedHorizontalActives = new DigitWidget[digitFixedCapacity];
            mDigitFixedVerticalActives = new DigitWidget[digitFixedCapacity];
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
            if(digitGroupTop) {
                digitGroupTop.Init();
                digitGroupTop.clickCallback += OnDigitGroupHorizontalClick;
            }

            if(digitGroupLeft) {
                digitGroupLeft.Init();
                digitGroupLeft.clickCallback += OnDigitGroupVerticalClick;
            }

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
                var mainCell = mAreaOp.mainCell;
                var mainCellWidget = mainAreaCellWidget;

                if(mainCell.isValid && mainCellWidget) {
                    //factor left
                    if(digitGroupTop) {
                        var anchorTrans = mainCellWidget.GetAnchor(areaCellAnchorTop);
                        if(anchorTrans) {
                            digitGroupTop.number = mainCell.op.operand1;
                            RefreshDigitGroupInteractive(digitGroupTop);

                            var rTrans = digitGroupTop.rectTransform;

                            rTrans.SetParent(anchorTrans, false);
                            rTrans.anchoredPosition = Vector2.zero;

                            digitGroupTop.gameObject.SetActive(true);
                        }
                    }

                    //factor right
                    if(digitGroupLeft) {
                        var anchorTrans = mainCellWidget.GetAnchor(areaCellAnchorLeft);
                        if(anchorTrans) {
                            digitGroupLeft.number = mainCell.op.operand2;
                            RefreshDigitGroupInteractive(digitGroupLeft);

                            var rTrans = digitGroupLeft.rectTransform;

                            rTrans.SetParent(anchorTrans, false);
                            rTrans.anchoredPosition = Vector2.zero;

                            digitGroupLeft.gameObject.SetActive(true);
                        }
                    }
                }

                //insert digits

                //top
                for(int col = 0; col < mAreaOp.areaColCount - 1; col++) {
                    var cell = mAreaOp.GetAreaOperation(0, col);
                    if(cell.isValid)
                        SetDigitFixedColumn(col, cell.op.operand1);
                }

                //left
                for(int row = 0; row < mAreaOp.areaRowCount - 1; row++) {
                    var cell = mAreaOp.GetAreaOperation(row, 0);
                    if(cell.isValid)
                        SetDigitFixedRow(row, cell.op.operand2);
                }
            }
            else
                Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");
        }
    }

    void M8.IModalPop.Pop() {
        if(digitGroupTop) {
            digitGroupTop.transform.SetParent(transform, false);
            digitGroupTop.gameObject.SetActive(false);
        }

        if(digitGroupLeft) {
            digitGroupLeft.transform.SetParent(transform, false);
            digitGroupLeft.gameObject.SetActive(false);
        }

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

    private void RefreshDigitGroupInteractive(DigitGroupWidget digitGroup) { //set interactivity based on number > 0, no interaction for last digit
        int digitCount = digitGroup.digitCount;
        if(digitCount > 0) {
            digitGroup.SetDigitInteractive(digitCount - 1, false);

            for(int i = digitCount - 2; i >= 0; i--)
                digitGroup.SetDigitInteractive(i, digitGroup.GetDigitNumber(i) > 0);
        }
    }

    private AreaOperationCellWidget GenerateAreaCell(int row, int col) {
        if(mAreaCellActives[row, col]) //fail-safe
            return mAreaCellActives[row, col];

        if(mAreaCellCache.Count == 0)
            return null;

        var newAreaCellWidget = mAreaCellCache.RemoveLast();

        newAreaCellWidget.SetCellIndex(row, col);

        mAreaCellActives[row, col] = newAreaCellWidget;

        return newAreaCellWidget;
    }

    private void SetDigitFixedColumn(int colIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedHorizontalActives, colIndex);
        if(!digitWidget)
            return;

        digitWidget.number = number;

        //setup position
        var areaCellWidget = mAreaCellActives[0, colIndex];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorTop);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.anchoredPosition = Vector2.zero;
        }
    }

    private void SetDigitFixedRow(int rowIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedVerticalActives, rowIndex);
        if(!digitWidget)
            return;

        digitWidget.number = number;

        //setup position
        var areaCellWidget = mAreaCellActives[rowIndex, 0];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorLeft);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.anchoredPosition = Vector2.zero;
        }
    }

    private DigitWidget GetOrGenerateDigitFixed(DigitWidget[] digitWidgets, int index) {
        if(index < 0 || index >= digitWidgets.Length)
            return null;

        var retDigitWidget = digitWidgets[index];
        if(!retDigitWidget) {
            if(mDigitFixedCache.Count != 0) {
                retDigitWidget = mDigitFixedCache.RemoveLast();

                retDigitWidget.Init(index);

                digitWidgets[index] = retDigitWidget;
            }
        }

        return retDigitWidget;
    }

    private void ClearAreaCells() {
        for(int r = 0; r < mAreaCellActives.GetLength(0); r++) {
            for(int c = 0; c < mAreaCellActives.GetLength(1); c++) {
                var areaCellWidget = mAreaCellActives[r, c];
                if(areaCellWidget) {
                    areaCellWidget.rectTransform.SetParent(cacheRoot, false);

                    mAreaCellCache.Add(areaCellWidget);

                    mAreaCellActives[r, c] = null;
                }
            }
        }
    }

    private void ClearDigits() {
        for(int i = 0; i < mDigitFixedHorizontalActives.Length; i++) {
            var digitWidget = mDigitFixedHorizontalActives[i];

            digitWidget.rectTransform.SetParent(cacheRoot, false);

            mDigitFixedCache.Add(digitWidget);

            mDigitFixedHorizontalActives[i] = null;
        }

        for(int i = 0; i < mDigitFixedVerticalActives.Length; i++) {
            var digitWidget = mDigitFixedVerticalActives[i];

            digitWidget.rectTransform.SetParent(cacheRoot, false);

            mDigitFixedCache.Add(digitWidget);

            mDigitFixedVerticalActives[i] = null;
        }
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
