using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace LoLExt {
    public class ModalMessage : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
        public const string modalNameGeneric = "message";

        public const string parmTitleTextRef = "confirmTitleTxtRef";
        public const string parmDescTextRef = "confirmDescTxtRef";
        public const string parmCallback = "confirmCB";

        [Header("UI")]
        public TMP_Text titleText;
        public TMP_Text descText;

        [Header("Speech")]
        public bool isSpeak;

        private static M8.GenericParams mParms = new M8.GenericParams();

        private System.Action mCallback;

        private string mDescTextRef;
        private bool mIsDescSpoken;

        /// <summary>
        /// Open generic dialog: modalNameGeneric
        /// </summary>
        public static void Open(string titleTextRef, string descTextRef, System.Action cb) {
            Open(modalNameGeneric, titleTextRef, descTextRef, cb);
        }

        public static void Open(string modalName, string titleTextRef, string descTextRef, System.Action cb) {
            //check to see if there's one already opened
            var uiMgr = M8.ModalManager.main;

            if(uiMgr.IsInStack(modalName)) { //fail-safe: modal is already open
                var dlg = uiMgr.GetBehaviour<ModalMessage>(modalName);

                //re-apply content
                if(dlg.titleText) dlg.titleText.text = M8.Localize.Get(titleTextRef);
                if(dlg.descText) dlg.descText.text = M8.Localize.Get(descTextRef);

                dlg.mCallback = cb;
            }
            else {
                mParms[parmTitleTextRef] = titleTextRef;
                mParms[parmDescTextRef] = descTextRef;
                mParms[parmCallback] = cb;

                uiMgr.Open(modalName, mParms);
            }
        }

        public void Confirm() {
            if(mCallback != null)
                mCallback();

            Close();
        }

        void M8.IModalActive.SetActive(bool aActive) {
            if(aActive) {
                if(isSpeak && !mIsDescSpoken) {
                    mIsDescSpoken = true;

                    if(!string.IsNullOrEmpty(mDescTextRef))
                        LoLManager.instance.SpeakText(mDescTextRef);
                }
            }
        }

        void M8.IModalPop.Pop() {
            mCallback = null;
        }

        void M8.IModalPush.Push(M8.GenericParams parms) {
            mDescTextRef = null;
            mIsDescSpoken = false;
            mCallback = null;

            if(parms != null) {
                if(parms.ContainsKey(parmTitleTextRef)) {
                    if(titleText) titleText.text = M8.Localize.Get(parms.GetValue<string>(parmTitleTextRef));
                }

                if(parms.ContainsKey(parmDescTextRef)) {
                    mDescTextRef = parms.GetValue<string>(parmDescTextRef);
                    if(descText) descText.text = M8.Localize.Get(mDescTextRef);
                }

                if(parms.ContainsKey(parmCallback))
                    mCallback = parms.GetValue<System.Action>(parmCallback);
            }
        }
    }
}