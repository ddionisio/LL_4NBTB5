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

        int interactCount = 0;

        if(blobDigitWidget) {
            blobDigitWidget.Init();
            blobDigitWidget.SetDigitVisibleAll(false);

            if(mBlob) {
                var num = mBlob.number;

                blobDigitWidget.number = num;

                //setup interactive for non-zero digits
                for(int i = 0; i < blobDigitWidget.digitCount; i++) {
                    blobDigitWidget.SetDigitVisible(i, true);
                    
                    var digitNum = blobDigitWidget.GetDigitNumber(i);

                    bool isInteract = digitNum > 0;

                    if(isInteract) {
                        //don't allow blob to become single digit
                        var numResult = num - (digitNum * WholeNumber.TenExponent(i));
                        if(WholeNumber.DigitCount(numResult) < 2)
                            blobDigitWidget.SetDigitInteractive(i, false);
                        else {
                            blobDigitWidget.SetDigitInteractive(i, true);
                            interactCount++;
                        }
                    }
                    else
                        blobDigitWidget.SetDigitInteractive(i, false);
                }
            }

            blobDigitWidget.clickCallback += OnDigitClick;
        }

        mIsComplete = false;

        //fail-safe
        if(interactCount <= 0)
            Close();
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
