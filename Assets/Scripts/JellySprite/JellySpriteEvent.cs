using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[System.Serializable]
public class JellySpriteBaseEvent : UnityEvent<JellySprite, int, BaseEventData> {
}

[System.Serializable]
public class JellySpritePointerEvent : UnityEvent<JellySprite, int, PointerEventData> {
}

[System.Serializable]
public class JellySpriteAxisEvent : UnityEvent<JellySprite, int, AxisEventData> {
}