using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LoLExt;

public class SummaryController : GameModeController<SummaryController> {
    [Header("Display")]
    public SummaryLevelWidget[] levelSummaries;
    public M8.TextMeshPro.TextMeshProCounter totalScoreCounterLabel;
    public RankWidget rankDisplay;
    public GameObject proceedGO;

    [Header("Animation")]
    public M8.Animator.Animate animator;
    [M8.Animator.TakeSelector]
    public string takeEnter;

    [Header("Music")]
    [M8.MusicPlaylist]
    public string playMusic;

    public void Proceed() {
        GameData.instance.ProceedNext();
    }

    protected override void OnInstanceInit() {
        base.OnInstanceInit();

        if(animator && !string.IsNullOrEmpty(takeEnter))
            animator.ResetTake(takeEnter);

        if(totalScoreCounterLabel)
            totalScoreCounterLabel.SetCountImmediate(0);

        if(rankDisplay)
            rankDisplay.gameObject.SetActive(false);

        if(proceedGO)
            proceedGO.SetActive(false);
    }

    protected override void OnInstanceDeinit() {
        base.OnInstanceDeinit();
    }

    protected override IEnumerator Start() {
        yield return base.Start();

        while(!LoLManager.instance.isReady)
            yield return null;

        if(!string.IsNullOrEmpty(playMusic))
            M8.MusicPlaylist.instance.Play(playMusic, true, false);

        var gameDat = GameData.instance;

        int totalScore = 0;
        int maxScore = 0;

        //setup summaries
        int levelSummaryInd = 0;
        for(int i = 0; i < gameDat.levels.Length; i++) {
            var lvlDat = gameDat.levels[i];

            if(!lvlDat.isGameplay)
                continue;

            var saveDat = gameDat.ScoreGet(i);

            totalScore += saveDat.score;
            maxScore += gameDat.GetMaxScore(saveDat.roundCount);

            var levelSummaryWidget = levelSummaries[levelSummaryInd];
            if(levelSummaryWidget)
                levelSummaryWidget.Setup(M8.Localize.Get(lvlDat.titleRef), saveDat);

            levelSummaryInd++;
            if(levelSummaryInd == levelSummaries.Length)
                break;
        }

        //display summaries
        if(animator && !string.IsNullOrEmpty(takeEnter))
            yield return animator.PlayWait(takeEnter);

        //setup and show total score
        if(totalScoreCounterLabel) {
            totalScoreCounterLabel.count = totalScore;

            while(totalScoreCounterLabel.isPlaying)
                yield return null;
        }

        //setup rank
        if(rankDisplay) {
            int rankIndex = gameDat.GetRankIndexByScore(totalScore, maxScore);

            rankDisplay.Apply(rankIndex);
            rankDisplay.gameObject.SetActive(true);
        }

        if(proceedGO)
            proceedGO.SetActive(true);
    }
}
