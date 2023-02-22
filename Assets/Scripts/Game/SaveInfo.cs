using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SaveInfo {
    public const string userKeyLevelScore = "s";
    public const string userKeyComboCount = "c";
    public const string userKeyBonusCount = "b";
    public const string userKeyErrorMultCount = "em";
    public const string userKeyErrorSumsCount = "es";

    public int score;
    public int combo;
    public int bonus;
    public int areaEvaluateMistakeCount;
    public int sumsMistakeCount;

    public static SaveInfo LoadFrom(M8.UserData userData, int level) {
        var header = level.ToString();

        int score = userData.GetInt(header + userKeyLevelScore);
        int combo = userData.GetInt(header + userKeyComboCount);
        int bonus = userData.GetInt(header + userKeyBonusCount);
        int areaEvaluateMistakeCount = userData.GetInt(header + userKeyErrorMultCount);
        int sumsMistakeCount = userData.GetInt(header + userKeyErrorSumsCount);

        return new SaveInfo { score = score, combo = combo, bonus = bonus, areaEvaluateMistakeCount = areaEvaluateMistakeCount, sumsMistakeCount = sumsMistakeCount };
    }

    public SaveInfo(int aScore, int aCombo, int aBonus, MistakeInfo mistakeInfo) {
        score = aScore;
        combo = aCombo;
        bonus = aBonus;

        if(mistakeInfo != null) {
            areaEvaluateMistakeCount = mistakeInfo.areaEvaluateMistakeCount;
            sumsMistakeCount = mistakeInfo.sumsMistakeCount;
        }
        else {
            areaEvaluateMistakeCount = 0;
            sumsMistakeCount = 0;
        }
    }

    public void SaveTo(M8.UserData userData, int level) {
        var header = level.ToString();

        userData.SetInt(header + userKeyLevelScore, score);
        userData.SetInt(header + userKeyComboCount, combo);
        userData.SetInt(header + userKeyBonusCount, bonus);
        userData.SetInt(header + userKeyErrorMultCount, areaEvaluateMistakeCount);
        userData.SetInt(header + userKeyErrorSumsCount, sumsMistakeCount);
    }
}