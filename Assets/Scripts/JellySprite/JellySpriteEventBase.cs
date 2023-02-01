using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class JellySpriteEventBase : MonoBehaviour {
    public JellySprite jellySprite;

    private JellySpriteEventTrigger[] mEventTriggers;
    private bool mIsInit = false;

    protected abstract void AddCallbacks(JellySpriteEventTrigger eventTrigger);
    protected abstract void RemoveCallbacks(JellySpriteEventTrigger eventTrigger);

    void OnDestroy() {
        if(mIsInit) {
            for(int i = 0; i < mEventTriggers.Length; i++) {
                var eventTrigger = mEventTriggers[i];
                if(eventTrigger)
                    RemoveCallbacks(eventTrigger);
            }
        }
    }

    void OnEnable() {
        if(!mIsInit)
            StartCoroutine(DoInit());
    }

    IEnumerator DoInit() {
        yield return null; //wait after an update to ensure

        if(!jellySprite)
            jellySprite = GetComponent<JellySprite>();

        //wait till jelly sprite initialize
        while(jellySprite.ReferencePoints == null || jellySprite.ReferencePoints.Count == 0)
            yield return null;

        mEventTriggers = new JellySpriteEventTrigger[jellySprite.ReferencePoints.Count];

        //get or add event trigger per reference point
        for(int i = 0; i < jellySprite.ReferencePoints.Count; i++) {
            var refPt = jellySprite.ReferencePoints[i];

            var eventTrigger = refPt.GameObject.GetComponent<JellySpriteEventTrigger>();
            if(!eventTrigger)
                eventTrigger = refPt.GameObject.AddComponent<JellySpriteEventTrigger>();

            mEventTriggers[i] = eventTrigger;

            eventTrigger.jellySprite = jellySprite;
            eventTrigger.index = i;

            AddCallbacks(eventTrigger);
        }

        mIsInit = true;
    }
}
