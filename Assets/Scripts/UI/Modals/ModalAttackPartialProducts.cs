using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class ModalAttackPartialProducts : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmEmptyCount = "emptyCount"; //number of partial products that need to be filled by player
    public const string parmAttackEmpty = "isAttackEmpty"; //if true, then player needs to fill in answer

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

    public bool isProceeding { get { return mProceedRout != null; } }

    private List<ProductInputWidget> mPartialProductActives;
    private M8.CacheList<ProductInputWidget> mPartialProductCache;

    private List<ProductInputWidget> mPartialProductInputs;
    private M8.CacheList<ProductInputWidget> mCorrectProductWidgets;
    private M8.CacheList<ProductInputWidget> mIncorrectProductWidgets;

    private int[] mPartialProductIndices;

    private M8.CacheList<int> mCorrectAnswers;
    private M8.CacheList<int> mCorrectAnswersChecked;

    private ProductInputWidget mSelectedProductWidget;

    private ModalAttackParams mAttackParms;

    private AreaOperation mAreaOp;
    private MistakeInfo mMistakeInfo;

    private M8.GenericParams mNumpadParms;

    private bool mIsInit;

    private Coroutine mProceedRout;

    public void Proceed() {
        if(isProceeding)
            return;

        StartProceed();
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

            mPartialProductIndices = new int[partialProductCapacity];

            mCorrectAnswers = new M8.CacheList<int>(partialProductCapacity);
            mCorrectAnswersChecked = new M8.CacheList<int>(partialProductCapacity);

            answerWidget.clickCallback += OnProductWidgetClick;

            mNumpadParms = new M8.GenericParams();
            mNumpadParms[GameData.modalParamOperationText] = "";
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

            int partialProductCount = 0;

            //setup partial products
            for(int row = 0; row < mAreaOp.areaRowCount; row++) {
                for(int col = 0; col < mAreaOp.areaColCount; col++) {
                    var cell = mAreaOp.GetAreaOperation(row, col);
                    if(cell.op.equal > 0) {
                        var partialProductWidget = GeneratePartialProductWidget();

                        //setup as if it's a fixed number
                        partialProductWidget.inputNumber = cell.op.equal;
                        partialProductWidget.selectedActive = false;
                        partialProductWidget.correctActive = false;                        
                        partialProductWidget.interactable = false;

                        mPartialProductIndices[partialProductCount] = partialProductCount;
                        partialProductCount++;
                    }
                }
            }

            //sort from highest to lowest
            mPartialProductActives.Sort(PartialProductWidgetSortCompare);

            if(emptyCount > 0) {
                //shuffle indices, then use that to determine empty widgets
                M8.ArrayUtil.Shuffle(mPartialProductIndices, 0, partialProductCount);

                var count = Mathf.Clamp(emptyCount, 0, partialProductCount);
                for(int i = 0; i < count; i++) {
                    var partialProductWidget = mPartialProductActives[mPartialProductIndices[i]];

                    mCorrectAnswers.Add(partialProductWidget.inputNumber);

                    partialProductWidget.SetEmpty();
                    partialProductWidget.interactable = true;
                }

                //add empty widgets to input in the correct order
                for(int i = 0; i < mPartialProductActives.Count; i++) {
                    var partialProductWidget = mPartialProductActives[i];
                    if(partialProductWidget.interactable)
                        mPartialProductInputs.Add(partialProductWidget);
                }
            }

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
            if(answerIsEmpty) {
                answerWidget.SetEmpty();                
                answerWidget.interactable = true;

                mPartialProductInputs.Add(answerWidget);
            }
            else {
                answerWidget.inputNumber = mAreaOp.operation.equal;
                answerWidget.interactable = false;
            }

            answerWidget.selectedActive = false;
            answerWidget.correctActive = false;
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
        StopProceed();

        ClearPartialProductWidgets();

        mPartialProductInputs.Clear();

        mCorrectProductWidgets.Clear();
        mIncorrectProductWidgets.Clear();

        mCorrectAnswers.Clear();
        mCorrectAnswersChecked.Clear();

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
        if(mSelectedProductWidget) {
            //update value of current selected widget
            var numpadModal = M8.ModalManager.main.GetBehaviour<ModalCalculator>(GameData.instance.modalNumpad);
            if(numpadModal)
                mSelectedProductWidget.inputNumber = Mathf.RoundToInt(numpadModal.curValueFloat);

            inputInd = mPartialProductInputs.IndexOf(mSelectedProductWidget);
        }

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
        if(mSelectedProductWidget) {
            //update value of current selected widget
            var numpadModal = M8.ModalManager.main.GetBehaviour<ModalCalculator>(GameData.instance.modalNumpad);
            if(numpadModal)
                mSelectedProductWidget.inputNumber = Mathf.RoundToInt(numpadModal.curValueFloat);

            inputInd = mPartialProductInputs.IndexOf(mSelectedProductWidget);
        }

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

    IEnumerator DoProceed() {
        //grab new incorrects and corrects
        mCorrectProductWidgets.Clear();
        mIncorrectProductWidgets.Clear();

        for(int i = 0; i < mPartialProductInputs.Count; i++) {
            var productWidget = mPartialProductInputs[i];
            if(productWidget.interactable) {
                bool isCorrect = false;

                if(productWidget == answerWidget)
                    isCorrect = productWidget.inputNumber == mAreaOp.operation.equal;
                else {
                    for(int j = 0; j < mCorrectAnswers.Count; j++) {
                        var answer = mCorrectAnswers[j];

                        if(productWidget.inputNumber == answer) {
                            mCorrectAnswers.RemoveAt(j);
                            mCorrectAnswersChecked.Add(answer);

                            isCorrect = true;
                            break;
                        }
                    }
                }

                if(isCorrect)
                    mCorrectProductWidgets.Add(productWidget);
                else
                    mIncorrectProductWidgets.Add(productWidget);
            }
        }

        //reset correct answer buffer
        for(int i = 0; i < mCorrectAnswersChecked.Count; i++)
            mCorrectAnswers.Add(mCorrectAnswersChecked[i]);

        mCorrectAnswersChecked.Clear();

        //do animations

        int animFinishCount = 0;

        //animate correct widgets
        for(int i = 0; i < mCorrectProductWidgets.Count; i++)
            mCorrectProductWidgets[i].PlayCorrect();
        
        while(animFinishCount < mCorrectProductWidgets.Count) {
            yield return null;

            for(int i = 0; i < mCorrectProductWidgets.Count; i++) {
                if(!mCorrectProductWidgets[i].isAnimating)
                    animFinishCount++;
            }
        }

        //set as correct active, non-interactable
        for(int i = 0; i < mCorrectProductWidgets.Count; i++) {
            var correctWidget = mCorrectProductWidgets[i];

            correctWidget.correctActive = true;
            correctWidget.interactable = false;

            mPartialProductInputs.Remove(correctWidget);

            mCorrectAnswers.Remove(correctWidget.inputNumber);
        }

        yield return new WaitForSeconds(evaluateEachDelay);

        animFinishCount = 0;

        //animate incorrect widgets
        for(int i = 0; i < mIncorrectProductWidgets.Count; i++)
            mIncorrectProductWidgets[i].PlayError();

        while(animFinishCount < mIncorrectProductWidgets.Count) {
            yield return null;

            for(int i = 0; i < mIncorrectProductWidgets.Count; i++) {
                if(!mIncorrectProductWidgets[i].isAnimating)
                    animFinishCount++;
            }
        }

        //all correct?
        if(mIncorrectProductWidgets.Count == 0) {
            //TODO: fanfare animation

            Close();

            signalInvokeAttackStateChange?.Invoke(AttackState.Success);
        }
        else {
            //error animation

            if(mMistakeInfo != null) {
                mMistakeInfo.AppendAreaEvaluateCount();

                //update mistake display
                mistakeCounterDisplay.UpdateMistakeCount(mMistakeInfo);

                if(mMistakeInfo.isFull) {
                    Close();

                    signalInvokeAttackStateChange?.Invoke(AttackState.Fail);
                }
            }
        }

        mProceedRout = null;
    }

    private void StartProceed() {
        mProceedRout = StartCoroutine(DoProceed());
    }

    private void StopProceed() {
        if(mProceedRout != null) {
            StopCoroutine(mProceedRout);
            mProceedRout = null;
        }
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

        return right.inputNumber - left.inputNumber;
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