using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;

[CreateAssetMenu(fileName = "gameData", menuName = "Game/GameData")]
public class GameData : M8.SingletonScriptableObject<GameData> {

    public const string modalParamOperationText = "opTxt";

    public const string userDataKeyWarning = "healthWarning";
    public const string userDataKeyFTUEBonusBlob = "ftue_bblob";
    public const string userDataKeyFTUEDistributeMixup = "ftue_distmix";
    public const string userDataKeyFTUEPartialProduct = "ftue_pprod";

    [System.Serializable]
    public struct LevelInfo {
        public M8.SceneAssetPath scene;
        [M8.Localize]
        public string titleRef;
        public bool isGameplay; //used to determine which scene is gameplay (e.g. score tracking)
        public int index; //used for certain scene
    }

    [System.Serializable]
    public struct RankData {
        public string grade; //SS, S, A, B, C, D
        public Sprite icon;
        public float scale;
    }

    [Header("Modals")]
    public string modalAttackDistributive = "attackDistributive";
    public string modalAttackAreaEvaluate = "attackAreaEvaluate";
    public string modalAttackSums = "attackSums";
    public string modalNumpad = "numpad";
    public string modalVictory = "victory";

    [Header("Game Flow")]
    public LevelInfo[] levels;
    public M8.SceneAssetPath endScene;

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
    public int mistakePenaltyPoints = 10;

    public int maxRetryCount = 2;

    [Header("FTUE")] //first time user experience
    public ModalDialogFlow bonusBlobDialog;

    public int retryCounter { get; set; }

    public bool isProceed { get; private set; }

    public void ScoreApply(int level, int aScore, int aRoundCount, MistakeInfo mistakeInfo) {
        var saveInfo = new SaveInfo(aScore, aRoundCount, mistakeInfo);

        saveInfo.SaveTo(LoLManager.instance.userData, level);
    }

    public SaveInfo ScoreGet(int level) {
        return SaveInfo.LoadFrom(LoLManager.instance.userData, level);
    }

    public int GetMaxScore(int roundCount) {
        int maxScore = 0;

        for(int i = 0; i < roundCount; i++) {
            maxScore += (i + 1) * correctPoints;
        }

        maxScore += perfectPoints;

        return maxScore;
    }

    public int GetRankIndex(int roundCount, int score) {
        var maxScore = GetMaxScore(roundCount);

        return GetRankIndexByScore(score, maxScore);
    }

    public int GetRankIndexByScore(int score, int maxScore) {
        float scoreScale = (float)score / maxScore;

        for(int i = 0; i < ranks.Length; i++) {
            var rank = ranks[i];
            if(scoreScale >= rank.scale)
                return i;
        }

        return ranks.Length - 1;
    }

    public int GetLevelIndexFromCurrentScene() {
        var curScene = M8.SceneManager.instance.curScene;

        for(int i = 0; i < levels.Length; i++) {
            if(levels[i].scene == curScene)
                return i;
        }

        return -1;
    }

    public void ResetProgress() {
        LoLManager.instance.userData.Delete();

        LoLManager.instance.ApplyProgress(0, 0);
    }

    public void ProceedNext() {
        int curProgress;

        //var nextProgress = LoLManager.instance.curProgress + 1;
        if(isProceed)
            curProgress = LoLManager.instance.curProgress;
        else {
            LoLManager.instance.progressMax = levels.Length;

            curProgress = GetLevelIndexFromCurrentScene();

            isProceed = true;
        }

        if(curProgress >= 0) {
            int nextProgress = curProgress + 1;

            LoLManager.instance.ApplyProgress(nextProgress);

            if(nextProgress < levels.Length)
                levels[nextProgress].scene.Load();
            else
                endScene.Load();
        }
    }

    public void ProceedFromCurrentProgress() {
        isProceed = true;

        LoLManager.instance.progressMax = levels.Length;

        var curProgress = LoLManager.instance.curProgress;
        if(curProgress >= levels.Length)
            endScene.Load();
        else
            levels[curProgress].scene.Load();
    }
}
