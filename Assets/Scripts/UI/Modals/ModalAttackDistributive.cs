using System.Collections;
using UnityEngine;

public class ModalAttackDistributive : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
    [Header("Templates")]
    public AreaOperationCellWidget areaCellTemplate;
    public int areaCellCapacity = 4;

    public DigitWidget digitFixedTemplate;
    public int digitFixedCapacity = 3;

    public OpWidget opTemplate;
    public int opCapacity = 3;

    public Transform cacheRoot;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeFinish;

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

    [Header("Distribute Info")]
    public float distributeNumberDelay = 0.3f;
    public float distributeNumberJumpHeight = 20f;
    public DG.Tweening.Ease distributeNumberEase = DG.Tweening.Ease.InOutSine;

    [Header("Finish Info")]
    public float finishEndDelay = 2f;

    [Header("Signal Invoke")]
    public SignalAttackState signalInvokeAttackStateChange;
    public M8.SignalBoolean signalInvokeActive;

    public AreaOperationCellWidget mainAreaCellWidget {
        get {
            if(mAreaOp == null)
                return null;

            return mAreaCellActives[mAreaOp.areaRowCount - 1, mAreaOp.areaColCount - 1];
        }
    }

    public bool isDigitSplitting { get { return mDigitSplitRout != null; } }

    private AreaOperation mAreaOp;

    private int mAreaGridInd = -1;

    private AreaOperationCellWidget[,] mAreaCellActives; //[row, col], count is based on row and col count in mAreaOp
    private M8.CacheList<AreaOperationCellWidget> mAreaCellCache;

    private DigitWidget[] mDigitFixedHorizontalActives; //count is based on mAreaOp's col count
    private DigitWidget[] mDigitFixedVerticalActives; //count is based on mAreaOp's row count
    private M8.CacheList<DigitWidget> mDigitFixedCache;

    private M8.CacheList<OpWidget> mOpActives;
    private M8.CacheList<OpWidget> mOpCache;

    private ModalAttackParams mAttackParms;

    private bool mIsInit;

    private Coroutine mDigitSplitRout;

    private DG.Tweening.EaseFunction mDistributeNumberEaseFunc;

    public void Back() {
        Close();

        signalInvokeAttackStateChange?.Invoke(AttackState.Cancel);
    }

    public void Proceed() {
        Close();

        M8.ModalManager.main.Open(GameData.instance.modalAttackAreaEvaluate, mAttackParms);
    }

    void M8.IModalActive.SetActive(bool aActive) {
        signalInvokeActive?.Invoke(aActive);
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

            mDistributeNumberEaseFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(distributeNumberEase);

            mIsInit = true;
        }

        //setup shared data across attack phases        
        mAttackParms = parms as ModalAttackParams;
        if(mAttackParms != null) {
            mAreaOp = mAttackParms.GetAreaOperation();
        }
        else {
            mAreaOp = null;
        }

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

                curAreaGrid.gameObject.SetActive(true);

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
                            areaCellWidget.ApplyCell(cell, true);

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
                    //top
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

                    //left
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

                //insert digits and operators

                //top
                for(int col = 0; col < mAreaOp.areaColCount - 1; col++) {
                    var cell = mAreaOp.GetAreaOperation(0, col);
                    if(cell.isValid) {
                        SetDigitFixedColumn(col, cell.op.operand1);
                        GenerateOperatorColumn(col);
                    }
                }

                //left
                for(int row = 0; row < mAreaOp.areaRowCount - 1; row++) {
                    var cell = mAreaOp.GetAreaOperation(row, 0);
                    if(cell.isValid) {
                        SetDigitFixedRow(row, cell.op.operand2);
                        GenerateOperatorRow(row);
                    }
                }
            }
            else
                Debug.LogError("Unable to find matching area grid: [row = " + mAreaOp.areaRowCount + ", col = " + mAreaOp.areaColCount + "]");
        }
    }

    void M8.IModalPop.Pop() {
        StopDigitSplit();

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
        if(isDigitSplitting)
            return;

        StartDigitSplitColumn(digitIndex);
    }

    void OnDigitGroupVerticalClick(int digitIndex) {
        if(isDigitSplitting)
            return;

        StartDigitSplitRow(digitIndex);
    }

    IEnumerator DoDigitSplitColumn(int digitIndex) {
        //apply to area op
        if(!mAreaOp.SplitAreaCol(mAreaOp.areaColCount - 1, digitIndex)) { //fail-safe
            mDigitSplitRout = null;
            yield break;
        }

        digitGroupTop.SetDigitInteractive(digitIndex, false);

        //unhide area cells
        for(int row = 0; row < mAreaOp.areaRowCount; row++) {
            var areaCellWidget = mAreaCellActives[row, digitIndex];
            if(areaCellWidget) {
                var areaCellInfo = mAreaOp.GetAreaOperation(row, digitIndex);
                if(areaCellInfo.isValid)
                    areaCellWidget.gameObject.SetActive(true);
            }
        }

        //animate grid
        var curAreaGrid = areaGrids[mAreaGridInd];

        curAreaGrid.ShowColumn(digitIndex);

        while(curAreaGrid.isAnimating) {
            yield return null;

            //refresh areas
            RefreshAreaCellWidgetTelemetries(0, digitIndex);
        }

        var mainCell = mAreaOp.mainCell;

        //apply new number of source
        digitGroupTop.number = mainCell.op.operand1;
        digitGroupTop.PlayPulse();

        //apply new op to main area cell widget
        mainAreaCellWidget.ApplyCell(mAreaOp.mainCell, true);

        //setup digit to dest, apply extracted number
        var digitWidget = SetDigitFixedColumn(digitIndex, mAreaOp.GetAreaOperation(mAreaOp.areaRowCount - 1, digitIndex).op.operand1);

        //do digit split animation (number float from source to dest)
        var digitStartTrans = digitGroupTop.GetDigitTransform(digitIndex);
        var digitWidgetTrans = digitWidget.rectTransform;

        Vector2 startPos = digitStartTrans.position;
        Vector2 endPos = digitWidgetTrans.position;

        digitWidgetTrans.position = startPos;

        var curTime = 0f;
        while(curTime < distributeNumberDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mDistributeNumberEaseFunc(curTime, distributeNumberDelay, 0f, 0f);

            var x = Mathf.Lerp(startPos.x, endPos.x, t);
            var y = Mathf.Lerp(startPos.y, endPos.y, t) + Mathf.Sin(Mathf.PI * t) * distributeNumberJumpHeight;

            digitWidgetTrans.position = new Vector3(x, y, 0f);
        }

        digitWidget.PlayPulse();

        //update and show new area cell operations
        RefreshAreaCellWidgetOperations(0, digitIndex);

        //add operator
        GenerateOperatorColumn(digitIndex);

        mDigitSplitRout = null;

        if(!(mAreaOp.canSplitFactorLeft || mAreaOp.canSplitFactorRight))
            StartCoroutine(DoFinish());
    }

    IEnumerator DoDigitSplitRow(int digitIndex) {
        //apply to area op
        if(!mAreaOp.SplitAreaRow(mAreaOp.areaRowCount - 1, digitIndex)) { //fail-safe
            mDigitSplitRout = null;
            yield break;
        }

        digitGroupLeft.SetDigitInteractive(digitIndex, false);

        //unhide area cells
        for(int col = 0; col < mAreaOp.areaColCount; col++) {
            var areaCellWidget = mAreaCellActives[digitIndex, col];
            if(areaCellWidget) {
                var areaCellInfo = mAreaOp.GetAreaOperation(digitIndex, col);
                if(areaCellInfo.isValid)
                    areaCellWidget.gameObject.SetActive(true);
            }
        }

        //animate grid
        var curAreaGrid = areaGrids[mAreaGridInd];

        curAreaGrid.ShowRow(digitIndex);

        while(curAreaGrid.isAnimating) {
            yield return null;

            //refresh areas
            RefreshAreaCellWidgetTelemetries(digitIndex, 0);
        }

        var mainCell = mAreaOp.mainCell;

        //apply new number of source
        digitGroupLeft.number = mainCell.op.operand2;
        digitGroupLeft.PlayPulse();

        //apply new op to main area cell widget
        mainAreaCellWidget.ApplyCell(mAreaOp.mainCell, true);

        //setup digit to dest, apply extracted number
        var digitWidget = SetDigitFixedRow(digitIndex, mAreaOp.GetAreaOperation(digitIndex, mAreaOp.areaColCount - 1).op.operand2);

        //do digit split animation (number float from source to dest)
        var digitStartTrans = digitGroupLeft.GetDigitTransform(digitIndex);
        var digitWidgetTrans = digitWidget.rectTransform;

        Vector2 startPos = digitStartTrans.position;
        Vector2 endPos = digitWidgetTrans.position;

        digitWidgetTrans.position = startPos;

        var curTime = 0f;
        while(curTime < distributeNumberDelay) {
            yield return null;

            curTime += Time.deltaTime;

            var t = mDistributeNumberEaseFunc(curTime, distributeNumberDelay, 0f, 0f);

            digitWidgetTrans.position = Vector2.Lerp(startPos, endPos, t);
        }

        digitWidget.PlayPulse();

        //update and show new area cell operations
        RefreshAreaCellWidgetOperations(digitIndex, 0);

        //add operator
        GenerateOperatorRow(digitIndex);

        mDigitSplitRout = null;

        if(!(mAreaOp.canSplitFactorLeft || mAreaOp.canSplitFactorRight))
            StartCoroutine(DoFinish());
    }

    IEnumerator DoFinish() {
        if(animator && !string.IsNullOrEmpty(takeFinish))
            yield return animator.PlayWait(takeFinish);

        yield return new WaitForSeconds(finishEndDelay);

        Proceed();
    }

    private void RefreshAreaCellWidgetTelemetries(int startRow, int startCol) {
        var curAreaGrid = areaGrids[mAreaGridInd];

        for(int row = startRow; row < mAreaOp.areaRowCount; row++) {
            for(int col = startCol; col < mAreaOp.areaColCount; col++) {
                var areaCellWidget = mAreaCellActives[row, col];
                if(areaCellWidget)
                    curAreaGrid.ApplyArea(row, col, areaCellWidget.rectTransform);
            }
        }
    }

    private void RefreshAreaCellWidgetOperations(int startRow, int startCol) {
        for(int row = startRow; row < mAreaOp.areaRowCount; row++) {
            for(int col = startCol; col < mAreaOp.areaColCount; col++) {
                var areaCellWidget = mAreaCellActives[row, col];
                if(areaCellWidget) {
                    var areaCellInfo = mAreaOp.GetAreaOperation(row, col);
                    if(areaCellInfo.isValid) {
                        areaCellWidget.ApplyCell(areaCellInfo, true);
                        areaCellWidget.operationVisible = true;
                    }
                }
            }
        }
    }

    private void StartDigitSplitColumn(int digitIndex) {
        if(mDigitSplitRout != null)
            StopCoroutine(mDigitSplitRout);

        mDigitSplitRout = StartCoroutine(DoDigitSplitColumn(digitIndex));
    }

    private void StartDigitSplitRow(int digitIndex) {
        if(mDigitSplitRout != null)
            StopCoroutine(mDigitSplitRout);

        mDigitSplitRout = StartCoroutine(DoDigitSplitRow(digitIndex));
    }

    private void StopDigitSplit() {
        if(mDigitSplitRout != null) {
            StopCoroutine(mDigitSplitRout);
            mDigitSplitRout = null;
        }
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

    private DigitWidget SetDigitFixedColumn(int colIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedHorizontalActives, colIndex);
        if(!digitWidget)
            return null;

        digitWidget.number = number;

        if(digitWidget.numberRoot) {
            digitWidget.numberRoot.anchorMin = digitWidget.numberRoot.anchorMax = digitWidget.numberRoot.pivot = new Vector2 { x = 0.5f, y = 0.5f };
        }

        //setup position
        var areaCellWidget = mAreaCellActives[mAreaOp.areaRowCount - 1, colIndex];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorTop);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.pivot = rTrans.anchorMin = rTrans.anchorMax = new Vector2 { x = 0.5f, y = 0f };
            rTrans.anchoredPosition = Vector2.zero;
        }

        return digitWidget;
    }

    private DigitWidget SetDigitFixedRow(int rowIndex, int number) {
        var digitWidget = GetOrGenerateDigitFixed(mDigitFixedVerticalActives, rowIndex);
        if(!digitWidget)
            return null;

        digitWidget.number = number;

        if(digitWidget.numberRoot) {
            digitWidget.numberRoot.anchorMin = digitWidget.numberRoot.anchorMax = digitWidget.numberRoot.pivot = new Vector2 { x = 1.0f, y = 0.5f };
        }

        //setup position
        var areaCellWidget = mAreaCellActives[rowIndex, mAreaOp.areaColCount - 1];
        if(areaCellWidget) {
            var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorLeft);

            var rTrans = digitWidget.rectTransform;

            rTrans.SetParent(anchorTrans, false);
            rTrans.pivot = rTrans.anchorMin = rTrans.anchorMax = new Vector2 { x = 1f, y = 0.5f };
            rTrans.anchoredPosition = Vector2.zero;
        }

        return digitWidget;
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

    private OpWidget GenerateOperatorColumn(int colIndex) {
        if(mOpCache.Count == 0 || colIndex < 0 || colIndex >= mAreaOp.areaColCount)
            return null;

        var newOpWidget = mOpCache.RemoveLast();

        mOpActives.Add(newOpWidget);

        var areaCellWidget = mAreaCellActives[mAreaOp.areaRowCount - 1, colIndex];

        var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorOpTop);

        var rectTrans = newOpWidget.rectTransform;

        rectTrans.SetParent(anchorTrans, false);
        rectTrans.anchoredPosition = Vector2.zero;

        return newOpWidget;
    }

    private OpWidget GenerateOperatorRow(int rowIndex) {
        if(mOpCache.Count == 0 || rowIndex < 0 || rowIndex >= mAreaOp.areaRowCount)
            return null;

        var newOpWidget = mOpCache.RemoveLast();

        mOpActives.Add(newOpWidget);

        var areaCellWidget = mAreaCellActives[rowIndex, mAreaOp.areaColCount - 1];

        var anchorTrans = areaCellWidget.GetAnchor(areaCellAnchorOpLeft);

        var rectTrans = newOpWidget.rectTransform;

        rectTrans.SetParent(anchorTrans, false);
        rectTrans.anchoredPosition = Vector2.zero;

        return newOpWidget;
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
            if(digitWidget) {
                digitWidget.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(digitWidget);

                mDigitFixedHorizontalActives[i] = null;
            }
        }

        for(int i = 0; i < mDigitFixedVerticalActives.Length; i++) {
            var digitWidget = mDigitFixedVerticalActives[i];
            if(digitWidget) {
                digitWidget.rectTransform.SetParent(cacheRoot, false);

                mDigitFixedCache.Add(digitWidget);

                mDigitFixedVerticalActives[i] = null;
            }
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
