using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;
using TMPro;

public class ModalVictory : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmMistakeInfo = "mi";
    public const string parmScore = "sc";
    public const string parmComboCount = "cc";
    public const string parmBonusCount = "bc";
    public const string parmBonusRoundIndex = "bi";
    public const string parmRoundCount = "rc";

    [Header("Combo Display")]
    public GameObject comboCountGO;
    public TMP_Text comboCountLabel;    
    public string comboCountFormat = "x{0}";
    public TMP_Text comboCountTitleLabel;

    [Header("Bonus Display")]
    public GameObject bonusAchievedGO;
    public TMP_Text bonusAchievedTitleLabel;

    [Header("Perfect Display")]
    public GameObject perfectAchievedGO;
    public TMP_Text perfectAchievedTitleLabel;

    [Header("Errors Display")]
    public M8.TextMeshPro.TextMeshProCounter errorMultiplyCounterLabel;
    public M8.TextMeshPro.TextMeshProCounter errorSumsCounterLabel;

    [Header("Score Display")]
    public M8.TextMeshPro.TextMeshProCounter scoreCounterLabel;
    public RankWidget rankDisplay;

    [Header("Proceed Display")]
    public GameObject proceedGO;

    [Header("Flow Info")]
    public Color flowLabelDisabled = Color.white;
    public Color flowLabelEnabled = Color.white;
    public float flowDelay = 0.3f;    

    private MistakeInfo mMistakeInfo;
    private int mScore;
    private int mBonusCount;
    private int mRankIndex;

    public void Proceed() {
        Close();

        var levelIndex = GameData.instance.GetLevelIndexFromCurrentScene();

        //save level data and update score
        if(levelIndex >= 0) {
            GameData.instance.ScoreApply(levelIndex, mScore, mBonusCount, mRankIndex, mMistakeInfo);

            LoLManager.instance.curScore += mScore;
        }

        //go to the next level
        GameData.instance.ProceedNext();
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        mMistakeInfo = null;
        mScore = 0;        
        mBonusCount = 0;

        int comboCount = 1;
        int bonusRoundIndex = -1;
        int roundCount = 0;

        if(parms != null) {
            if(parms.ContainsKey(parmMistakeInfo))
                mMistakeInfo = parms.GetValue<MistakeInfo>(parmMistakeInfo);

            if(parms.ContainsKey(parmScore))
                mScore = parms.GetValue<int>(parmScore);

            if(parms.ContainsKey(parmComboCount))
                comboCount = parms.GetValue<int>(parmComboCount);

            if(parms.ContainsKey(parmBonusCount))
                mBonusCount = parms.GetValue<int>(parmBonusCount);

            if(parms.ContainsKey(parmBonusRoundIndex))
                bonusRoundIndex = parms.GetValue<int>(parmBonusRoundIndex);

            if(parms.ContainsKey(parmRoundCount))
                roundCount = parms.GetValue<int>(parmRoundCount);
        }

        //setup initial display state
        if(comboCountGO)
            comboCountGO.SetActive(false);

        if(bonusAchievedGO)
            bonusAchievedGO.SetActive(false);

        if(perfectAchievedGO)
            perfectAchievedGO.SetActive(false);

        if(comboCountTitleLabel)
            comboCountTitleLabel.color = flowLabelDisabled;

        if(bonusAchievedTitleLabel)
            bonusAchievedTitleLabel.color = flowLabelDisabled;

        if(perfectAchievedTitleLabel)
            perfectAchievedTitleLabel.color = flowLabelDisabled;


        if(comboCountLabel)
            comboCountLabel.text = string.Format(comboCountFormat, comboCount);


        if(errorMultiplyCounterLabel)
            errorMultiplyCounterLabel.SetCountImmediate(0);

        if(errorSumsCounterLabel)
            errorSumsCounterLabel.SetCountImmediate(0);

        if(scoreCounterLabel)
            scoreCounterLabel.SetCountImmediate(0);


        mRankIndex = GameData.instance.GetRankIndex(roundCount, bonusRoundIndex, mScore);

        if(rankDisplay) {
            rankDisplay.gameObject.SetActive(false);
            rankDisplay.Apply(mRankIndex);
        }

        if(proceedGO)
            proceedGO.SetActive(false);

        StartCoroutine(DoProcess());
    }

    void M8.IModalPop.Pop() {

    }

    IEnumerator DoProcess() {
        //wait for modal to finish entering
        var modalMain = M8.ModalManager.main;
        while(modalMain.isBusy)
            yield return null;

        var flowWait = new WaitForSeconds(flowDelay);

        //show combo
        if(comboCountGO)
            comboCountGO.SetActive(true);

        if(comboCountTitleLabel)
            comboCountTitleLabel.color = flowLabelEnabled;

        yield return flowWait;

        //show bonus
        if(mBonusCount > 0) {
            if(bonusAchievedGO)
                bonusAchievedGO.SetActive(true);

            if(bonusAchievedTitleLabel)
                bonusAchievedTitleLabel.color = flowLabelEnabled;

            yield return flowWait;
        }

        if(mMistakeInfo != null) {
            //show perfect
            if(mMistakeInfo.totalMistakeCount <= 0) {
                if(perfectAchievedGO)
                    perfectAchievedGO.SetActive(true);

                if(perfectAchievedTitleLabel)
                    perfectAchievedTitleLabel.color = flowLabelEnabled;

                yield return flowWait;
            }
            else {
                //apply error counters
                if(errorMultiplyCounterLabel)
                    errorMultiplyCounterLabel.count = mMistakeInfo.areaEvaluateMistakeCount;

                if(errorSumsCounterLabel)
                    errorSumsCounterLabel.count = mMistakeInfo.sumsMistakeCount;

                yield return flowWait;
            }
        }

        //apply score
        if(scoreCounterLabel)
            scoreCounterLabel.count = mScore;

        yield return flowWait;

        //show rank and proceed display
        if(rankDisplay)
            rankDisplay.gameObject.SetActive(true);

        if(proceedGO)
            proceedGO.SetActive(true);
    }
}
