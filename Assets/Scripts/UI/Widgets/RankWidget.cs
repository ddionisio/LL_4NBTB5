using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public class RankWidget : MonoBehaviour {
    [Header("Display")]
    public TMP_Text rankText;
    public Image rankIcon;
    public bool rankIconApplySize;
    public GameObject[] rankPlatings; //highest to lowest

    public void Apply(int rankIndex) {
        var rank = GameData.instance.ranks[rankIndex];

        //setup plating
        if(rankPlatings.Length > 0) {
            var rankPlatingIndex = Mathf.Clamp(rankIndex, 0, rankPlatings.Length - 1);
            for(int i = 0; i < rankPlatings.Length; i++) {
                if(rankPlatings[i])
                    rankPlatings[i].SetActive(i == rankPlatingIndex);
            }
        }

        //setup rank display
        if(rankText)
            rankText.text = rank.grade;

        if(rankIcon) {
            if(rank.icon) {
                rankIcon.sprite = rank.icon;

                if(rankIconApplySize)
                    rankIcon.SetNativeSize();

                rankIcon.gameObject.SetActive(true);
            }
            else
                rankIcon.gameObject.SetActive(false);
        }
    }
}
