using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[AddComponentMenu("Jelly Sprite/Events/Pointer")]
public class JellySpriteEventPointer : JellySpriteEventBase {
    [Header("Events")]
    public JellySpritePointerEvent clickEvent;
    public JellySpritePointerEvent downEvent;
    public JellySpritePointerEvent upEvent;
    public JellySpritePointerEvent enterEvent;
    public JellySpritePointerEvent exitEvent;

    protected override void AddCallbacks(JellySpriteEventTrigger eventTrigger) {
        eventTrigger.pointerClickCallback += clickEvent.Invoke;
        eventTrigger.pointerDownCallback += downEvent.Invoke;
        eventTrigger.pointerUpCallback += upEvent.Invoke;
        eventTrigger.pointerEnterCallback += enterEvent.Invoke;
        eventTrigger.pointerExitCallback += exitEvent.Invoke;
    }

    protected override void RemoveCallbacks(JellySpriteEventTrigger eventTrigger) {
        eventTrigger.pointerClickCallback -= clickEvent.Invoke;
        eventTrigger.pointerDownCallback -= downEvent.Invoke;
        eventTrigger.pointerUpCallback -= upEvent.Invoke;
        eventTrigger.pointerEnterCallback -= enterEvent.Invoke;
        eventTrigger.pointerExitCallback -= exitEvent.Invoke;
    }
}
