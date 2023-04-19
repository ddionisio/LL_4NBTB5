using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModalDigitRemover : M8.ModalController, M8.IModalPush, M8.IModalPop {
    public const string parmBlob = "blob";

    [Header("Display")]
    public DigitGroupWidget blobDigitWidget;

    private Blob mBlob;
    private bool mIsComplete;

    void M8.IModalPush.Push(M8.GenericParams parms) {
        mBlob = null;

        if(parms != null) {
            if(parms.ContainsKey(parmBlob))
                mBlob = parms.GetValue<Blob>(parmBlob);
        }

        if(blobDigitWidget) {
            blobDigitWidget.Init();

            if(mBlob) {
                var num = mBlob.number;

                blobDigitWidget.number = num;

                //setup interactive for non-zero digits
                for(int i = 0; i < blobDigitWidget.digitCount; i++) {
                    //don't allow blob to become single digit
                    var digitNum = blobDigitWidget.GetDigitNumber(i);
                    if(digitNum > 0) {
                        var numResult = num - (digitNum * WholeNumber.TenExponent(i));
                        if(WholeNumber.DigitCount(numResult) < 2) {
                            blobDigitWidget.SetDigitInteractive(i, false);
                            continue;
                        }
                    }

                    blobDigitWidget.SetDigitInteractive(i, digitNum > 0);
                }
            }

            blobDigitWidget.clickCallback += OnDigitClick;
        }

        mIsComplete = false;
    }

    void M8.IModalPop.Pop() {
        mBlob = null;

        if(blobDigitWidget) blobDigitWidget.clickCallback -= OnDigitClick;
    }

    void OnDigitClick(int digitIndex) {
        if(mIsComplete)
            return;

        blobDigitWidget.SetDigitNumber(digitIndex, 0);
        blobDigitWidget.SetDigitInteractive(digitIndex, false);

        if(mBlob) {
            mBlob.number = blobDigitWidget.number;
        }

        mIsComplete = true;

        Close();
    }
}
