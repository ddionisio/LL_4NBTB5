using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaGridControl : MonoBehaviour {
    public class AreaData {
        public Vector2 position; //top-left local
        public Vector2 size;

        public float scale;
    }

    //these are ratio within the full size, so top-left should be set to 1 (last elements of both ratio)
    public float[] rowRatio; //bottom to top
    public float[] colRatio; //right to left

    public RectTransform rectTransform {
        get {
            if(!mRectTransform)
                mRectTransform = GetComponent<RectTransform>();
            return mRectTransform;
        }
    }

    private AreaData[,] mAreas;
    private RectTransform mRectTransform;

    void OnDrawGizmos() {
        if(rowRatio == null || colRatio == null || rowRatio.Length == 0 || colRatio.Length == 0)
            return;

        Gizmos.color = Color.yellow;

        var corners = new Vector3[4];

        rectTransform.GetWorldCorners(corners);

        M8.Gizmo.DrawWireRect(corners);

        for(int r = 0; r < rowRatio.Length; r++) {
            var ratio = rowRatio[r];
            if(ratio >= 1f)
                continue;

            var y = Mathf.Lerp(corners[0].y, corners[1].y, ratio);

            var pos1 = new Vector3(corners[0].x, y, corners[0].z);
            var pos2 = new Vector3(corners[2].x, y, corners[0].z);

            Gizmos.DrawLine(pos1, pos2);
        }

        for(int c = 0; c < colRatio.Length; c++) {
            var ratio = colRatio[c];
            if(ratio >= 1f)
                continue;

            var x = Mathf.Lerp(corners[2].x, corners[0].x, ratio);

            var pos1 = new Vector3(x, corners[0].y, corners[0].z);
            var pos2 = new Vector3(x, corners[1].y, corners[0].z);

            Gizmos.DrawLine(pos1, pos2);
        }
    }
}
