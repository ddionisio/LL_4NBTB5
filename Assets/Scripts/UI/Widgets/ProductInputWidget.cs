using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;

public class ProductInputWidget : MonoBehaviour, IPointerClickHandler {
    [Header("Display")]
    [SerializeField]
    TMP_Text _numberLabel;
    [SerializeField]
    GameObject _selectedGO;
    [SerializeField]
    GameObject _interactableActiveGO;
    [SerializeField]
    GameObject _correctActiveGO;

    [Header("Animation")]
    [SerializeField]
    M8.Animator.Animate _animator;
    [M8.Animator.TakeSelector(animatorField = "_animator")]
    [SerializeField]
    string _takeCorrect;
    [M8.Animator.TakeSelector(animatorField = "_animator")]
    [SerializeField]
    string _takeError;

    public int inputNumber { 
        get { return mInputNumber; }
        set {
            if(mInputNumber != value || isEmpty) {
                mInputNumber = value;

                //update text
                if(_numberLabel)
                    _numberLabel.text = mInputNumber.ToString();
            }
        }
    }

    public bool interactable {
        get { return mInteractable; }
        set {
            mInteractable = value;

            if(!mSelectable)
                mSelectable = GetComponent<Selectable>();

            if(mSelectable)
                mSelectable.interactable = value;

            if(_interactableActiveGO)
                _interactableActiveGO.SetActive(value);
        }
    }

    public bool correctActive {
        get { return _correctActiveGO ? _correctActiveGO.activeSelf : false; }
        set {
            if(_correctActiveGO)
                _correctActiveGO.SetActive(value);
        }
    }

    public bool selectedActive {
        get { return _selectedGO ? _selectedGO.activeSelf : false; }
        set {
            if(_selectedGO)
                _selectedGO.SetActive(value);
        }
    }

    public bool isAnimating {
        get {
            return _animator ? _animator.isPlaying : false;
        }
    }

    public RectTransform rectTransform {
        get {
            if(!mRectTrans)
                mRectTrans = GetComponent<RectTransform>();
            return mRectTrans;
        }
    }

    public bool isEmpty { get { return _numberLabel ? string.IsNullOrEmpty(_numberLabel.text) : true; } }

    public event System.Action<ProductInputWidget> clickCallback;

    private int mInputNumber;
    private Selectable mSelectable;

    private RectTransform mRectTrans;

    private bool mInteractable;

    public void SetEmpty() {
        mInputNumber = 0;

        if(_numberLabel)
            _numberLabel.text = "";
    }

    public void PlayCorrect() {
        if(_animator && !string.IsNullOrEmpty(_takeCorrect))
            _animator.Play(_takeCorrect);
    }

    public void PlayError() {
        if(_animator && !string.IsNullOrEmpty(_takeError))
            _animator.Play(_takeError);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData) {
        if(!interactable)
            return;

        clickCallback?.Invoke(this);
    }
}
