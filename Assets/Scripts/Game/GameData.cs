using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {
    public const string levelScoreHeader = "levelScore_";

    public const string modalParamOperationText = "opTxt";

    [System.Serializable]
    public struct RankData {
        public string grade; //SS, S, A, B, C, D
        public float scale;
    }

    [Header("Modals")]
    public string modalAttackDistributive = "attackDistributive";
    public string modalAttackAreaEvaluate = "attackAreaEvaluate";
    public string modalAttackSums = "attackSums";
    public string modalNumpad = "numpad";

    [Header("Rank Settings")]
    public RankData[] ranks; //highest to lowest
    public int rankIndexRetry; //threshold for retry    

    [Header("Play Settings")]
    public int areaRowCapacity = 2;
    public int areaColCapacity = 4;
    public int mistakeCount = 3;    
    public int correctPoints = 100;
    public int bonusPoints = 1000;
    public int perfectPoints = 500;

    public int maxRetryCount = 2;

    public int retryCounter { get; set; }

    public void ScoreApply(int level, int score) {
        LoLManager.instance.userData.SetInt(levelScoreHeader + level.ToString(), score);
    }

    public int ScoreGet(int level) {
        return LoLManager.instance.userData.GetInt(levelScoreHeader + level.ToString());
    }

    public int GetRankIndex(int roundCount, int score) {
        int maxScore = 0;

        for(int i = 0; i < roundCount; i++)
            maxScore += (i + 1) * correctPoints;

        maxScore += bonusPoints;

        maxScore += perfectPoints;

        float scoreScale = (float)score / maxScore;

        for(int i = 0; i < ranks.Length; i++) {
            var rank = ranks[i];
            if(scoreScale >= rank.scale)
                return i;
        }

        return ranks.Length - 1;
    }

    protected override void OnInstanceInit() {

    }
}
