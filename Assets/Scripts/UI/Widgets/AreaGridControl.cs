using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaGridControl : MonoBehaviour {
    public enum AnimMode {
        RowShow,
        RowHide,
        ColumnShow,
        ColumnHide
    }

    public class AreaData {
        public Vector2 position; //bottom-right local (pivot: [1, 0], anchors: [1, 0])
        public Vector2 size;

        public Vector2 scale;

        public AreaData() {

        }

        public AreaData(Vector2 aScale) {
            scale = aScale;
        }
    }

    public struct AnimQueueInfo {
        public AnimMode mode;
        public int index;
    }

    [Header("Data")]
    //these are ratio within the full size, so top-left should be set to 1 (last elements of both ratio)
    [SerializeField]
    float[] _rowRatio; //bottom to top
    [SerializeField]
    float[] _colRatio; //right to left

    [Header("Animation")]
    [SerializeField]
    DG.Tweening.Ease _animScaleUpEase = DG.Tweening.Ease.OutSine;
    [SerializeField]
    DG.Tweening.Ease _animScaleDownEase = DG.Tweening.Ease.InSine;
    [SerializeField]
    float _animDelay = 0.5f;

    public RectTransform rectTransform {
        get {
            if(!mRectTransform)
                mRectTransform = GetComponent<RectTransform>();
            return mRectTransform;
        }
    }

    public int numRow { get { return mAreas != null ? mAreas.GetLength(0) : 0; } }
    public int numCol { get { return mAreas != null ? mAreas.GetLength(1) : 0; } }

    public bool isAnimating { get { return mAnimRout != null; } }

    public bool isInit { get { return mIsInit; } }

    private AreaData[,] mAreas; //[row, col]
    private RectTransform mRectTransform;
    private Queue<AnimQueueInfo> mAnimQueue;

    private Coroutine mAnimRout;
    private bool mIsInit;

    private DG.Tweening.EaseFunction mAnimScaleUpFunc;
    private DG.Tweening.EaseFunction mAnimScaleDownFunc;

    public void Init(bool onlyScaleLastCell) {
        if(!mIsInit) {
            int numRow = _rowRatio != null ? _rowRatio.Length : 0,
                numCol = _colRatio != null ? _colRatio.Length : 0;

            mAreas = new AreaData[numRow, numCol];

            for(int r = 0; r < numRow; r++)
                for(int c = 0; c < numCol; c++)
                    mAreas[r, c] = new AreaData();

            mAnimQueue = new Queue<AnimQueueInfo>(numRow * numCol);

            mAnimScaleUpFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_animScaleUpEase);
            mAnimScaleDownFunc = DG.Tweening.Core.Easing.EaseManager.ToEaseFunction(_animScaleDownEase);

            mIsInit = true;
        }

        ResetAreas(onlyScaleLastCell);
    }

    public void ResetAreas(bool onlyScaleLastCell) {
        StopAnim();

        for(int r = 0; r < numRow; r++) {
            for(int c = 0; c < numCol; c++) {
                Vector2 scale;
                if(onlyScaleLastCell) {
                    if(r == numRow - 1) {
                        if(c == numCol - 1)
                            scale = Vector2.one;
                        else
                            scale = new Vector2(0f, 1f);
                    }
                    else
                        scale = Vector2.zero;
                }
                else
                    scale = Vector2.one;

                mAreas[r, c].scale = scale;
            }
        }

        RefreshAreas();
    }

    public void ApplyArea(int row, int col, RectTransform rTrans) {
        if(row >= numRow || col >= numCol)
            return;

        rTrans.pivot = new Vector2 { x = 1f, y = 0f };
        rTrans.anchorMin = new Vector2 { x = 1f, y = 0f };
        rTrans.anchorMax = new Vector2 { x = 1f, y = 0f };

        var area = mAreas[row, col];

        rTrans.anchoredPosition = area.position;
        rTrans.sizeDelta = area.size;
    }

    public Vector2 GetAreaScale(int row, int col) {
        if(row >= numRow || col >= numCol)
            return Vector2.zero;

        return mAreas[row, col].scale;
    }

    public void ShowRow(int rowIndex) {
        AddAnim(AnimMode.RowShow, rowIndex);
    }

    public void HideRow(int rowIndex) {
        AddAnim(AnimMode.RowHide, rowIndex);
    }

    public void ShowColumn(int colIndex) {
        AddAnim(AnimMode.ColumnShow, colIndex);
    }

    public void HideColumn(int colIndex) {
        AddAnim(AnimMode.ColumnHide, colIndex);
    }

    void OnDisable() {
        StopAnim();
    }

    IEnumerator DoAnim() {
        while(mAnimQueue.Count > 0) {
            var animInfo = mAnimQueue.Dequeue();
            var ind = animInfo.index;

            switch(animInfo.mode) {
                case AnimMode.RowShow:
                    yield return DoAnimRow(ind, mAreas[ind, 0].scale.y, 1f, mAnimScaleUpFunc);
                    break;
                case AnimMode.RowHide:
                    yield return DoAnimRow(ind, mAreas[ind, 0].scale.y, 0f, mAnimScaleDownFunc);
                    break;

                case AnimMode.ColumnShow:
                    yield return DoAnimCol(ind, mAreas[0, ind].scale.x, 1f, mAnimScaleUpFunc);
                    break;
                case AnimMode.ColumnHide:
                    yield return DoAnimCol(ind, mAreas[0, ind].scale.x, 0f, mAnimScaleDownFunc);
                    break;
                default:
                    yield return null;
                    break;
            }
        }

        mAnimRout = null;
    }

    IEnumerator DoAnimRow(int rowIndex, float scaleStart, float scaleEnd, DG.Tweening.EaseFunction easeFunc) {
        var curTime = 0f;
        while(curTime < _animDelay) {
            curTime += Time.deltaTime;

            var t = easeFunc(curTime, _animDelay, 0f, 0f);

            var scaleY = Mathf.Lerp(scaleStart, scaleEnd, t);

            for(int c = 0; c < numCol; c++) {
                var area = mAreas[rowIndex, c];

                area.scale.y = scaleY;
            }

            RefreshAreas();

            yield return null;
        }
    }

    IEnumerator DoAnimCol(int colIndex, float scaleStart, float scaleEnd, DG.Tweening.EaseFunction easeFunc) {
        var curTime = 0f;
        while(curTime < _animDelay) {
            curTime += Time.deltaTime;

            var t = easeFunc(curTime, _animDelay, 0f, 0f);

            var scaleX = Mathf.Lerp(scaleStart, scaleEnd, t);

            for(int r = 0; r < numRow; r++) {
                var area = mAreas[r, colIndex];

                area.scale.x = scaleX;
            }

            RefreshAreas();

            yield return null;
        }
    }

    private void AddAnim(AnimMode mode, int index) {
        mAnimQueue.Enqueue(new AnimQueueInfo { mode = mode, index = index });

        if(mAnimRout == null)
            mAnimRout = StartCoroutine(DoAnim());
    }

    private void StopAnim() {
        if(mAnimRout != null) {
            StopCoroutine(mAnimRout);
            mAnimRout = null;
        }

        if(mAnimQueue != null)
            mAnimQueue.Clear();
    }

    private void RefreshAreas() {
        var rTrans = rectTransform;
        var rect = rTrans.rect;

        Vector2 lastSize = Vector2.zero;

        int nRow = mAreas.GetLength(0), nCol = mAreas.GetLength(1);


        for(int r = 0; r < nRow; r++) {
            var curRowRatio = _rowRatio[r];

            lastSize.x = 0.0f;

            for(int c = 0; c < nCol; c++) {
                var area = mAreas[r, c];

                area.position = new Vector2 { x = -lastSize.x, y = lastSize.y };

                //take into account the last cell, which will just be the leftover area size
                area.size = new Vector2 {
                    x = c == nCol - 1 ? rect.size.x - lastSize.x : rect.size.x * _colRatio[c] * area.scale.x,
                    y = r == nRow - 1 ? rect.size.y - lastSize.y : rect.size.y * curRowRatio * area.scale.y
                };

                lastSize.x += area.size.x;
            }

            lastSize.y += mAreas[r, 0].size.y;
        }
    }

    void OnDrawGizmos() {
        if(_rowRatio == null || _colRatio == null || _rowRatio.Length == 0 || _colRatio.Length == 0)
            return;

        Gizmos.color = Color.yellow;

        var corners = new Vector3[4];

        rectTransform.GetWorldCorners(corners);

        M8.Gizmo.DrawWireRect(corners);

        float curRatio = 0.0f;

        for(int r = 0; r < _rowRatio.Length; r++) {
            curRatio += _rowRatio[r];
            if(curRatio < 1f) {
                var y = Mathf.Lerp(corners[0].y, corners[1].y, curRatio);

                var pos1 = new Vector3(corners[0].x, y, corners[0].z);
                var pos2 = new Vector3(corners[2].x, y, corners[0].z);

                Gizmos.DrawLine(pos1, pos2);
            }
            
        }

        curRatio = 0.0f;

        for(int c = 0; c < _colRatio.Length; c++) {
            curRatio += _colRatio[c];
            if(curRatio < 1f) {
                var x = Mathf.Lerp(corners[2].x, corners[0].x, curRatio);

                var pos1 = new Vector3(x, corners[0].y, corners[0].z);
                var pos2 = new Vector3(x, corners[1].y, corners[0].z);

                Gizmos.DrawLine(pos1, pos2);
            }
        }
    }
}
