using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragHolder : M8.SingletonBehaviour<DragHolder> {
    [SerializeField]
    RectTransform _dragRoot;

    public RectTransform dragRoot { get { return _dragRoot; } }
}
