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
    public const string parmRoundCount = "rc";

    [Header("Combo Display")]
    public TMP_Text comboCountLabel;
    public string comboCountFormat = "x{0}";

    [Header("Bonus Display")]
    public GameObject bonusAchievedGO;

    [Header("Perfect Display")]
    public GameObject perfectAchievedGO;

    [Header("Errors Display")]
    public M8.TextMeshPro.TextMeshProCounter errorMultiplyCounterLabel;
    public M8.TextMeshPro.TextMeshProCounter errorSumsCounterLabel;

    [Header("Score Display")]
    public M8.TextMeshPro.TextMeshProCounter scoreCounterLabel;
    public RankWidget rankDisplay;

    private MistakeInfo mMistakeInfo;
    private int mScore;
    private int mComboCount;
    private int mBonusCount;
    
    public void Proceed() {
        Close();

        var levelIndex = GameData.instance.GetLevelIndexFromCurrentScene();

        //save level data and update score
        if(levelIndex >= 0) {
            GameData.instance.ScoreApply(levelIndex, mScore, mComboCount, mBonusCount, mMistakeInfo);

            LoLManager.instance.curScore += mScore;
        }

        //go to the next level
        GameData.instance.Proceed();
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        mMistakeInfo = null;
        mScore = 0;
        mComboCount = 1;
        mBonusCount = 0;

        int roundCount = 0;

        if(parms != null) {
            if(parms.ContainsKey(parmMistakeInfo))
                mMistakeInfo = parms.GetValue<MistakeInfo>(parmMistakeInfo);

            if(parms.ContainsKey(parmScore))
                mScore = parms.GetValue<int>(parmScore);

            if(parms.ContainsKey(parmComboCount))
                mComboCount = parms.GetValue<int>(parmComboCount);

            if(parms.ContainsKey(parmBonusCount))
                mBonusCount = parms.GetValue<int>(parmBonusCount);

            if(parms.ContainsKey(parmRoundCount))
                roundCount = parms.GetValue<int>(parmRoundCount);
        }

        bool isPerfect = mMistakeInfo != null ? mMistakeInfo.totalMistakeCount <= 0 : false;

        if(comboCountLabel)
            comboCountLabel.text = string.Format(comboCountFormat, mComboCount);

        if(bonusAchievedGO)
            bonusAchievedGO.SetActive(mBonusCount > 0);

        if(perfectAchievedGO)
            perfectAchievedGO.SetActive(isPerfect);

        if(errorMultiplyCounterLabel)
            errorMultiplyCounterLabel.count = mMistakeInfo != null ? mMistakeInfo.areaEvaluateMistakeCount : 0;

        if(errorSumsCounterLabel)
            errorSumsCounterLabel.count = mMistakeInfo != null ? mMistakeInfo.sumsMistakeCount : 0;

        if(scoreCounterLabel)
            scoreCounterLabel.count = mScore;

        var rankIndex = GameData.instance.GetRankIndex(roundCount, mScore);

        if(rankDisplay)
            rankDisplay.Apply(rankIndex);
    }

    void M8.IModalPop.Pop() {

    }
}
