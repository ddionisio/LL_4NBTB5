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
    public string modalNumpad = "numpad";

    [Header("Rank Settings")]
    public RankData[] ranks; //highest to lowest
    public int rankIndexRetry; //threshold for retry    

    [Header("Play Settings")]
    public int mistakeCount = 3;
    public int areaRowCapacity = 2;
    public int areaColCapacity = 4;
    public float hintDelay = 15f;
    public int hintErrorCount = 5;
    public int correctPoints = 100;
    public int correctDecayPoints = 25; //if hint was shown
    public int perfectPoints = 1000;
    public float timeParPerRound = 6f; //in seconds
    public float timeParPerRoundRanking; //in seconds, used for ranking, make sure it's less than timeParPerRound
    public int timeBonus = 50; //per second based on (timePar - time)

    public int maxRetryCount = 2;

    [Header("Debug")]
    public bool debugAutoGenerateAttackBlob;

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

        maxScore += perfectPoints;

        float timePar = timeParPerRound * roundCount;
        float rankTime = timeParPerRoundRanking * roundCount;

        maxScore += Mathf.RoundToInt((timePar - rankTime) * timeBonus);

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
