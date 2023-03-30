using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LoLExt;
using TMPro;

public class SummaryLevelWidget : MonoBehaviour {
    [Header("Display")]
    public TMP_Text titleLabel;
    public TMP_Text errorMultiplyCountLabel;
    public TMP_Text errorSumsCountLabel;
    public TMP_Text scoreCountLabel;
    public RankWidget rankDisplay;

    public void Setup(string title, SaveInfo info) {
        if(titleLabel) titleLabel.text = title;

        if(errorMultiplyCountLabel) errorMultiplyCountLabel.text = info.areaEvaluateMistakeCount.ToString();
        if(errorSumsCountLabel) errorSumsCountLabel.text = info.sumsMistakeCount.ToString();
        if(scoreCountLabel) scoreCountLabel.text = info.score.ToString();

        int rankIndex = GameData.instance.GetRankIndex(info.roundCount, info.score);

        rankDisplay.Apply(rankIndex);
    }
}