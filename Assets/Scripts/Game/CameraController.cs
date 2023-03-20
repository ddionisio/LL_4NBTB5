using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraController : MonoBehaviour {

    public static CameraController main {
        get {
            if(!mMain) {
                var camCtrlGO = GameObject.FindGameObjectWithTag("MainCamera");
                if(camCtrlGO) {
                    mMain = camCtrlGO.GetComponentInParent<CameraController>();
                    if(mMain)
                        mMain.Init(); //in case awake hasn't been called
                }
            }

            return mMain;
        }
    }

    public Camera cameraTarget { get { return mCamera; } }

    public bool raycastTarget {
        get { return mRaycaster ? mRaycaster.enabled : false; }
        set {
            if(mRaycaster)
                mRaycaster.enabled = value;
        }
    }

    private static CameraController mMain;

    private Camera mCamera;
    private Physics2DRaycaster mRaycaster;

    private bool mIsInit;

    void Awake() {
        Init();
    }

    private void Init() {
        if(!mIsInit) {
            mCamera = GetComponentInChildren<Camera>(true);

            if(mCamera)
                mRaycaster = mCamera.GetComponent<Physics2DRaycaster>();

            mIsInit = true;
        }
    }
}
