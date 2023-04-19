using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

public class HintWidget : MonoBehaviour {
    [Header("Texts")]
    [M8.Localize]
    public string hintZeroesFew;
    [M8.Localize]
    public string hintZeroesMany;
    [M8.Localize]
    public string hintMultTwo;
    [M8.Localize]
    public string hintMultThree;
    [M8.Localize]
    public string hintMultFour;
    [M8.Localize]
    public string hintMultFive;
    [M8.Localize]
    public string hintMultSix;
    [M8.Localize]
    public string hintMultSeven;
    [M8.Localize]
    public string hintMultEight;
    [M8.Localize]
    public string hintMultNine;

    [Header("Display")]
    public GameObject displayGO;
    public M8.TextMeshPro.TextMeshProTypewriter label;
    public GameObject talkActiveGO; //while talking
    public float endDelay = 1f;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeEnter;
    [M8.Animator.TakeSelector]
    public string takeExit;

    [Header("Signal Listen")]
    public M8.Signal signalListenMistake;

    private string mCurTextRef = null;

    private Coroutine mRout;    

    void OnDestroy() {
        if(signalListenMistake) signalListenMistake.callback -= OnSignalMistake;
    }

    void Awake() {
        if(displayGO) displayGO.SetActive(false);
        if(talkActiveGO) talkActiveGO.SetActive(false);

        signalListenMistake.callback += OnSignalMistake;
    }

    IEnumerator DoShowHint(string textRef) {
        mCurTextRef = textRef;

        label.Clear();

        if(displayGO) displayGO.SetActive(true);
        if(talkActiveGO) talkActiveGO.SetActive(false);

        //enter
        if(animator && !string.IsNullOrEmpty(takeEnter))
            yield return animator.PlayWait(takeEnter);

        //speak
        if(talkActiveGO) talkActiveGO.SetActive(true);

        var lolLocalize = M8.Localize.instance as LoLLocalize;

        var textInfo = lolLocalize.GetExtraInfo(mCurTextRef);

        label.text = lolLocalize.GetText(mCurTextRef);
        label.Play();

        LoLManager.instance.SpeakText(mCurTextRef);

        var isSkip = false;

        var modalMgr = M8.ModalManager.main;

        var curTime = 0f;

        //wait based on duration of speech
        while(!isSkip && curTime < textInfo.voiceDuration) {
            yield return null;
            curTime += Time.deltaTime;

            //cancel if dialog appears
            if(modalMgr.IsInStack(ModalDialog.modalNameGeneric))
                isSkip = true;
        }

        if(talkActiveGO) talkActiveGO.SetActive(false);

        //wait a bit
        curTime = 0f;
        while(!isSkip && curTime < endDelay) {
            yield return null;
            curTime += Time.deltaTime;

            //cancel if dialog appears
            if(modalMgr.IsInStack(ModalDialog.modalNameGeneric))
                isSkip = true;
        }

        //exit
        if(animator && !string.IsNullOrEmpty(takeExit))
            yield return animator.PlayWait(takeExit);
        
        if(displayGO) displayGO.SetActive(false);

        mCurTextRef = null;
        mRout = null;
    }

    void OnSignalMistake() {
        var playCtrl = PlayController.instance;

        //only show if we at least had 3 mistakes
        if(playCtrl.mistakeTotal.totalMistakeCount < 3)
            return;

        //don't show if we are at last mistake
        if(playCtrl.mistakeCurrent.isFull)
            return;

        string textRef = null;

        //check which modal is active
        if(M8.ModalManager.main.IsInStack(GameData.instance.modalAttackAreaEvaluate)) {
            textRef = GetHintTextRefForAreaEvaluate();
        }

        //already showing, or nothing to show
        if(textRef == mCurTextRef || string.IsNullOrEmpty(textRef))
            return;

        //show
        if(mRout != null)
            StopCoroutine(mRout);

        mRout = StartCoroutine(DoShowHint(textRef));
    }

    private string GetHintTextRefForAreaEvaluate() {
        var modalAreaEval = M8.ModalManager.main.GetBehaviour<ModalAttackAreaEvaluate>(GameData.instance.modalAttackAreaEvaluate);
        if(!modalAreaEval) //fail-safe
            return null;

        var areaCellWidget = modalAreaEval.areaOpCellWidgetSelected;
        if(!areaCellWidget) //fail-safe
            return null;

        var answer = modalAreaEval.lastAnswerNumber;

        var evalOp = areaCellWidget.cellData.op;

        //NOTE: assume we are solving in multiples of 10's
        var lFactorZeroCount = WholeNumber.ZeroCount(evalOp.operand1);
        var rFactorZeroCount = WholeNumber.ZeroCount(evalOp.operand2);

        var factorZeroTotal = lFactorZeroCount + rFactorZeroCount;

        var lFactorNonZero = evalOp.operand1 / WholeNumber.TenExponent(lFactorZeroCount);
        var rFactorNonZero = evalOp.operand2 / WholeNumber.TenExponent(rFactorZeroCount);

        var productNonZero = lFactorNonZero * rFactorNonZero;

        int answerNonZero = answer;
        int answerZeroCount = 0;
        bool isAnswerMatch = false;

        for(; answerNonZero > 0; answerNonZero /= 10, answerZeroCount++) {
            if(answerNonZero == productNonZero) {                
                isAnswerMatch = true;
                break;
            }

            if(answerNonZero % 10 != 0)
                break;
        }

        string ret = null;

        if(!isAnswerMatch) {
            switch(rFactorNonZero) {
                case 2:
                    ret = hintMultTwo;
                    break;
                case 3:
                    ret = hintMultThree;
                    break;
                case 4:
                    ret = hintMultFour;
                    break;
                case 5:
                    ret = hintMultFive;
                    break;
                case 6:
                    ret = hintMultSix;
                    break;
                case 7:
                    ret = hintMultSeven;
                    break;
                case 8:
                    ret = hintMultEight;
                    break;
                case 9:
                    ret = hintMultNine;
                    break;
                default: //???
                    break;
            }
        }
        else { //must be mismatched zero counts
            if(factorZeroTotal > answerZeroCount)
                ret = hintZeroesFew;
            else if(factorZeroTotal < answerZeroCount)
                ret = hintZeroesMany;
        }

        return ret;
    }
}
