using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class OpWidget : MonoBehaviour {
    [SerializeField]
    TMP_Text _opLabel;
    [SerializeField]
    OperatorType _initialOperator;

    public OperatorType operatorType {
        get { return mOp; }
        set {
            if(mOp != value) {
                mOp = value;
                RefreshDisplay();
            }
        }
    }

    public RectTransform rectTransform {
        get {
            if(!mRectTrans)
                mRectTrans = GetComponent<RectTransform>();
            return mRectTrans;
        }
    }

    private OperatorType mOp;
    private RectTransform mRectTrans;

    void Awake() {
        mOp = _initialOperator;
        RefreshDisplay();
    }

    private void RefreshDisplay() {
        if(_opLabel)
            _opLabel.text = Operation.GetOperatorTypeString(mOp);
    }
}
