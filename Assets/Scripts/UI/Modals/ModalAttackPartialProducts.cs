using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalAttackPartialProducts : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmEmptyCount = "apec"; //number of partial products that need to be filled by player
    public const string parmAttackEmpty = "apea"; //if true, then player needs to fill in answer

    [Header("Product Display")]
    public TMP_Text factorLeftLabel;
    public TMP_Text factorRightLabel;

    [Header("Partial Product Info")]
    public ProductInputWidget partialProductTemplate;
    public int partialProductCapacity = 4;
    public Transform partialProductCacheRoot;
    public RectTransform partialProductRoot;

    [Header("Answer Display")]
    public ProductInputWidget answerWidget;

    [Header("Mistake Display")]
    public MistakeCounterWidget mistakeCounterDisplay;

    [Header("Evaluate Info")]
    public float evaluateEachDelay = 0.3f;
    [M8.SoundPlaylist]
    public string evaluateSfxCorrect;
    [M8.SoundPlaylist]
    public string evaluateSfxWrong;

    [Header("Signal Invoke")]
    public M8.SignalFloat signalInvokeValueChange;
    public SignalAttackState signalInvokeAttackStateChange;

    [Header("Signal Listen")]
    public M8.Signal signalListenPrev;
    public M8.Signal signalListenNext;
    public M8.SignalFloat signalListenNumpadProceed;

    private List<ProductInputWidget> mPartialProductActives;
    private M8.CacheList<ProductInputWidget> mPartialProductCache;

    private List<ProductInputWidget> mPartialProductInputs;
    private M8.CacheList<ProductInputWidget> mCorrectProductWidgets;
    private M8.CacheList<ProductInputWidget> mIncorrectProductWidgets;

    private ProductInputWidget mSelectedProductWidget;

    private ModalAttackParams mAttackParms;

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private M8.GenericParams mNumpadParms;

    private bool mIsInit;

    public void Proceed() {

    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        if(!mIsInit) {
            //initialize partial product widgets
            mPartialProductActives = new List<ProductInputWidget>(partialProductCapacity);
            mPartialProductCache = new M8.CacheList<ProductInputWidget>(partialProductCapacity);

            for(int i = 0; i < partialProductCapacity; i++) {
                var partialProductWidget = Instantiate(partialProductTemplate);

                partialProductWidget.clickCallback += OnProductWidgetClick;

                partialProductWidget.rectTransform.SetParent(partialProductCacheRoot, false);

                mPartialProductCache.Add(partialProductWidget);
            }

            //initialize caches (account for answer)
            mPartialProductInputs = new List<ProductInputWidget>(partialProductCapacity + 1);
            mCorrectProductWidgets = new M8.CacheList<ProductInputWidget>(partialProductCapacity + 1);
            mIncorrectProductWidgets = new M8.CacheList<ProductInputWidget>(partialProductCapacity + 1);

            answerWidget.clickCallback += OnProductWidgetClick;

            mNumpadParms = new M8.GenericParams();
            mNumpadParms[ModalCalculator.parmMaxDigit] = 8;
            mNumpadParms["showPrevNext"] = true;

            mIsInit = true;
        }

        //setup shared data across attack phases
        int emptyCount = 0;
        bool answerIsEmpty = false;

        mAttackParms = parms as ModalAttackParams;
        if(mAttackParms != null) {
            mAreaOp = mAttackParms.GetAreaOperation();
            mMistakeInfo = mAttackParms.GetMistakeInfo();

            if(mAttackParms.ContainsKey(parmEmptyCount))
                emptyCount = mAttackParms.GetValue<int>(parmEmptyCount);

            if(mAttackParms.ContainsKey(parmAttackEmpty))
                answerIsEmpty = mAttackParms.GetValue<bool>(parmAttackEmpty);
        }
        else {
            mAreaOp = null;
            mMistakeInfo = null;
        }

        if(mAreaOp != null) {
            //divide up areas
            mAreaOp.SplitAll();

            //setup main factors
            if(factorLeftLabel)
                factorLeftLabel.text = mAreaOp.operation.operand1.ToString();

            if(factorRightLabel)
                factorRightLabel.text = mAreaOp.operation.operand2.ToString();

            //setup partial products
            for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                for(int col = 0; col < mAreaOp.areaColCount; col++) {
                    var cell = mAreaOp.GetAreaOperation(row, col);
                    if(cell.op.equal > 0) {
                        var partialProductWidget = GeneratePartialProductWidget();

                        partialProductWidget.correctNumber = cell.op.equal;
                        partialProductWidget.correctActive = false;
                        partialProductWidget.selectedActive = false;
                    }
                }
            }

            if(emptyCount > 0) {
                //shuffle items
                for(int i = 0, max = mPartialProductActives.Count; i < max; i++) {
                    int r = Random.Range(i, max);
                    var obj = mPartialProductActives[i];
                    var robj = mPartialProductActives[r];
                    mPartialProductActives[i] = robj;
                    mPartialProductActives[r] = obj;
                }

                //apply empty items on top, everything else is fixed
                for(int i = 0; i < mPartialProductActives.Count; i++) {
                    var partialProductWidget = mPartialProductActives[i];

                    if(i < emptyCount) {
                        partialProductWidget.SetEmpty();
                        partialProductWidget.interactable = true;

                        mPartialProductInputs.Add(partialProductWidget);
                    }
                    else {
                        partialProductWidget.inputNumber = partialProductWidget.correctNumber;
                        partialProductWidget.interactable = false;
                    }
                }
            }
            else {
                for(int i = 0; i < mPartialProductActives.Count; i++) {
                    var partialProductWidget = mPartialProductActives[i];

                    partialProductWidget.inputNumber = partialProductWidget.correctNumber;
                    partialProductWidget.interactable = false;
                }
            }

            //sort from highest to lowest
            mPartialProductActives.Sort(PartialProductWidgetSortCompare);

            //add partial products to layout
            float factorsHeight = 0; //assume size delta is the actual size of the widget

            for(int i = 0; i < mPartialProductActives.Count; i++) {
                factorsHeight += mPartialProductActives[i].rectTransform.sizeDelta.y;

                mPartialProductActives[i].rectTransform.SetParent(partialProductRoot);
            }

            //adjust root size
            //factorRoot.SetSizeWithCurrentAnchors((RectTransform.Axis)axis, LayoutUtility.GetPreferredSize(factorRoot, axis)); //Axis-Y
            var partialProductRootSizeDelta = partialProductRoot.sizeDelta;
            partialProductRootSizeDelta.y = factorsHeight;
            partialProductRoot.sizeDelta = partialProductRootSizeDelta;

            //setup answer
            answerWidget.correctNumber = mAreaOp.operation.equal;

            if(answerIsEmpty) {
                answerWidget.SetEmpty();
                answerWidget.interactable = true;

                mPartialProductInputs.Add(answerWidget);
            }
            else {
                answerWidget.inputNumber = answerWidget.correctNumber;
                answerWidget.interactable = false;
            }

            answerWidget.correctActive = false;
            answerWidget.selectedActive = false;
        }

        //setup mistake counter
        if(mMistakeInfo != null) {
            if(mistakeCounterDisplay)
                mistakeCounterDisplay.Init(mMistakeInfo);
        }

        if(signalListenPrev)
            signalListenPrev.callback += OnNumpadPrev;
        if(signalListenNext)
            signalListenNext.callback += OnNumpadNext;
        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback += OnNumpadProceed;
    }

    void M8.IModalPop.Pop() {
        ClearPartialProductWidgets();

        mPartialProductInputs.Clear();
        mCorrectProductWidgets.Clear();
        mIncorrectProductWidgets.Clear();

        mSelectedProductWidget = null;

        if(signalListenPrev)
            signalListenPrev.callback -= OnNumpadPrev;
        if(signalListenNext)
            signalListenNext.callback -= OnNumpadNext;
        if(signalListenNumpadProceed)
            signalListenNumpadProceed.callback -= OnNumpadProceed;
    }

    void OnNumpadPrev() {
        //get current index
        int inputInd = -1;
        if(mSelectedProductWidget)
            inputInd = mPartialProductInputs.IndexOf(mSelectedProductWidget);

        ClearSelected();

        //get previous
        if(inputInd != -1)
            mSelectedProductWidget = inputInd > 0 ? mPartialProductInputs[inputInd - 1] : mPartialProductInputs[mPartialProductInputs.Count - 1];
        else if(mPartialProductInputs.Count > 0) //failsafe
            mSelectedProductWidget = mPartialProductInputs[0];

        //update select display and numpad number input
        if(mSelectedProductWidget) {
            mSelectedProductWidget.selectedActive = true;

            signalInvokeValueChange?.Invoke(mSelectedProductWidget.inputNumber);
        }
    }

    void OnNumpadNext() {
        //get current index
        int inputInd = -1;
        if(mSelectedProductWidget)
            inputInd = mPartialProductInputs.IndexOf(mSelectedProductWidget);

        ClearSelected();

        //get next
        if(inputInd != -1)
            mSelectedProductWidget = inputInd < mPartialProductInputs.Count - 1 ? mPartialProductInputs[inputInd + 1] : mPartialProductInputs[0];
        else if(mPartialProductInputs.Count > 0) //failsafe
            mSelectedProductWidget = mPartialProductInputs[0];

        //update select display and numpad number input
        if(mSelectedProductWidget) {
            mSelectedProductWidget.selectedActive = true;

            signalInvokeValueChange?.Invoke(mSelectedProductWidget.inputNumber);
        }
    }

    void OnNumpadProceed(float val) {
        M8.ModalManager.main.CloseUpTo(GameData.instance.modalNumpad, true);

        if(mSelectedProductWidget) {
            int iVal = Mathf.RoundToInt(val);

            mSelectedProductWidget.inputNumber = iVal;
        }

        ClearSelected();
    }

    void OnProductWidgetClick(ProductInputWidget widget) {
        ClearSelected();

        mSelectedProductWidget = widget;
        mSelectedProductWidget.selectedActive = true;

        mNumpadParms[ModalCalculator.parmInitValue] = widget.inputNumber;

        M8.ModalManager.main.Open(GameData.instance.modalNumpad, mNumpadParms);
    }

    private void ClearSelected() {
        if(mSelectedProductWidget) {
            mSelectedProductWidget.selectedActive = false;
            mSelectedProductWidget = null;
        }
    }

    private ProductInputWidget GeneratePartialProductWidget() {
        if(mPartialProductCache.Count == 0)
            return null;

        var partialProductWidget = mPartialProductCache.RemoveLast();

        mPartialProductActives.Add(partialProductWidget);

        return partialProductWidget;
    }

    private int PartialProductWidgetSortCompare(ProductInputWidget left, ProductInputWidget right) {
        if(!left)
            return -1;
        else if(!right)
            return 1;

        return right.correctNumber - left.correctNumber;
    }

    private void ClearPartialProductWidgets() {
        for(int i = 0; i < mPartialProductActives.Count; i++) {
            var partialProductWidget = mPartialProductActives[i];

            partialProductWidget.rectTransform.SetParent(partialProductCacheRoot, false);

            mPartialProductCache.Add(partialProductWidget);
        }

        mPartialProductActives.Clear();
    }
}