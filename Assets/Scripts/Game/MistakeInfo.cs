using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MistakeInfo {

    public int areaEvaluateMistakeCount { get { return mAreaEvaluateMistakeCount; } }
    public int sumsMistakeCount { get { return mSumsMistakeCount; } }

    public int totalMistakeCount { get { return mAreaEvaluateMistakeCount + mSumsMistakeCount; } }

    public int maxMistakeCount { get; private set; }

    public bool isFull { get { return totalMistakeCount >= maxMistakeCount; } }

    private int mAreaEvaluateMistakeCount;
    private int mSumsMistakeCount;

    public MistakeInfo(int aMaxMistakeCount) {
        maxMistakeCount = aMaxMistakeCount;

        Reset();
    }

    public void AppendAreaEvaluateCount() {
        if(!isFull)
            mAreaEvaluateMistakeCount++;
    }

    public void AppendSumsCount() {
        if(!isFull)
            mSumsMistakeCount++;
    }

    public void Append(MistakeInfo other) {
        mAreaEvaluateMistakeCount += other.mAreaEvaluateMistakeCount;
        mSumsMistakeCount += other.mSumsMistakeCount;
    }

    public void Reset() {
        mAreaEvaluateMistakeCount = 0;
        mSumsMistakeCount = 0;
    }
}
