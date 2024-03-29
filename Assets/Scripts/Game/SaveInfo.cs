using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SaveInfo {
    public const string userKeyLevelScore = "s";
    public const string userKeyErrorMultCount = "em";
    public const string userKeyErrorSumsCount = "es";
    public const string userKeyRoundCount = "rc";

    public int score;
    public int areaEvaluateMistakeCount;
    public int sumsMistakeCount;
    public int roundCount;

    public static SaveInfo LoadFrom(M8.UserData userData, int level) {
        var header = level.ToString();

        int score = userData.GetInt(header + userKeyLevelScore);
        int areaEvaluateMistakeCount = userData.GetInt(header + userKeyErrorMultCount);
        int sumsMistakeCount = userData.GetInt(header + userKeyErrorSumsCount);
        int roundCount = userData.GetInt(header + userKeyRoundCount);

        return new SaveInfo { score = score, areaEvaluateMistakeCount = areaEvaluateMistakeCount, sumsMistakeCount = sumsMistakeCount, roundCount = roundCount };
    }

    public SaveInfo(int aScore, int aRoundCount, MistakeInfo mistakeInfo) {
        score = aScore;
        roundCount = aRoundCount;

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
        userData.SetInt(header + userKeyErrorMultCount, areaEvaluateMistakeCount);
        userData.SetInt(header + userKeyErrorSumsCount, sumsMistakeCount);
        userData.SetInt(header + userKeyRoundCount, roundCount);
    }
}