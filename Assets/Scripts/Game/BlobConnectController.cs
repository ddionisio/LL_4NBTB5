using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manage links between blobs with connects
/// </summary>
public class BlobConnectController : MonoBehaviour {
    public class Group {
        public BlobConnect connectOp;
        public BlobConnect connectEq;

        public Blob blobOpLeft;
        public Blob blobOpRight;

        public Blob blobEq;

        public bool isEmpty {
            get {
                return !(blobOpLeft || blobOpRight || blobEq);
            }
        }

        public bool isOpFilled {
            get {
                return blobOpLeft && blobOpRight;
            }
        }

        public bool isComplete {
            get {
                return isOpFilled && blobEq;
            }
        }

        public void GetBlobOrder(out Blob blobOpLeft, out Blob blobOpRight, out Blob blobEqual) {
            blobEqual = blobEq;
            blobOpRight = connectEq.GetLinkedBlob(blobEqual);
            blobOpLeft = connectOp.GetLinkedBlob(blobOpRight);
        }

        public void GetNumbers(out int numOpLeft, out int numOpRight, out int numEqual) {
            var blobOpRight = connectEq.GetLinkedBlob(blobEq);
            var blobOpLeft = connectOp.GetLinkedBlob(blobOpRight);

            numOpLeft = blobOpLeft ? blobOpLeft.number : 0;
            numOpRight = blobOpRight ? blobOpRight.number : 0;
            numEqual = blobEq ? blobEq.number : 0;
        }

        public void GetNumbers(out float numOpLeft, out float numOpRight, out float numEqual) {
            var blobOpRight = connectEq.GetLinkedBlob(blobEq);
            var blobOpLeft = connectOp.GetLinkedBlob(blobOpRight);

            numOpLeft = blobOpLeft ? blobOpLeft.number : 0;
            numOpRight = blobOpRight ? blobOpRight.number : 0;
            numEqual = blobEq ? blobEq.number : 0;
        }

        public bool IsBlobOp(Blob blob) {
            return blobOpLeft == blob || blobOpRight == blob;
        }

        public bool IsBlobOp(GameObject blobGO) {
            if(blobOpLeft && blobOpLeft.gameObject == blobGO)
                return true;

            if(blobOpRight && blobOpRight.gameObject == blobGO)
                return true;

            return false;
        }

        public bool IsBlobInGroup(Blob blob) {
            return blobOpLeft == blob || blobOpRight == blob || blobEq == blob;
        }

        public bool IsBlobInGroup(GameObject blobGO) {
            if(blobOpLeft && blobOpLeft.gameObject == blobGO)
                return true;

            if(blobOpRight && blobOpRight.gameObject == blobGO)
                return true;

            if(blobEq && blobEq.gameObject == blobGO)
                return true;

            return false;
        }

        public void ClearEq() {
            if(connectEq) {
                if(!connectEq.isReleasing)
                    connectEq.state = BlobConnect.State.Releasing;
                connectEq = null;
            }

            blobEq = null;
        }

        public void Clear() {
            if(connectOp) {
                if(!connectOp.isReleasing)
                    connectOp.state = BlobConnect.State.Releasing;
                connectOp = null;
            }

            blobOpLeft = null;
            blobOpRight = null;

            ClearEq();
        }
                
        public void SetOp(Blob left, Blob right, BlobConnect connect) {
            Clear(); //fail-safe

            blobOpLeft = left;
            blobOpRight = right;

            connectOp = connect;
        }

        public void SetEq(Blob eq, BlobConnect connect) {
            if(connectEq && !connectEq.isReleasing)
                connectEq.state = BlobConnect.State.Releasing;

            blobEq = eq;

            connectEq = connect;
        }

        /// <summary>
        /// Remove given blob from this group if it matches, returns true if able to match and clean-up.
        /// </summary>
        public bool RemoveBlob(Blob blob) {
            if(blobOpLeft == blob || blobOpRight == blob) {
                Clear();
                return true;
            }
            else if(blobEq == blob) {
                if(connectEq) {
                    if(!connectEq.isReleasing)
                        connectEq.state = BlobConnect.State.Releasing;
                    connectEq = null;
                }

                blobEq = null;

                return true;
            }

            return false;
        }
    }

    [Header("Connect Template")]
    public string poolGroup = "connect";
    public GameObject connectTemplate;
    public int capacity = 5;

    [Header("Data")]
    public int groupCapacity = 3;
    

    [Header("Signal Listens")]
    public SignalBlob signalListenBlobDragBegin;
    public SignalBlob signalListenBlobDragEnd;
    public SignalBlob signalListenBlobDespawn;

    public SignalBlobConnect signalListenBlobConnectDelete;

    /// <summary>
    /// Current operator type when connecting two unlinked blob
    /// </summary>
    public OperatorType curOp { get { return mCurOp; } set { mCurOp = value; } }

    /// <summary>
    /// Grab the first element active group
    /// </summary>
    public Group activeGroup { get { return mGroupActives.Count > 0 ? mGroupActives[0] : null; } }

    public Blob curBlobDragging { get; private set; }
    public Group curGroupDragging { get; private set; } //which group is involved while dragging

    public event System.Action<Group> evaluateCallback;
    public event System.Action<Group> groupAddedCallback;

    private M8.PoolController mPool;

    private OperatorType mCurOp = OperatorType.Multiply;

    private BlobConnect mCurConnectDragging; //when dragging a blob around.
        
    private M8.GenericParams mConnectSpawnParms = new M8.GenericParams();

    private M8.CacheList<Group> mGroupActives;
    private M8.CacheList<Group> mGroupCache;

    public bool IsGroupActive(Group grp) {
        return mGroupActives.Exists(grp);
    }
        
    public void ReleaseDragging() {
        if(mCurConnectDragging) {
            mCurConnectDragging.Release();
            mCurConnectDragging = null;
        }

        curBlobDragging = null;
        curGroupDragging = null;
    }

    public void ClearGroup(Group group) {
        for(int i = 0; i < mGroupActives.Count; i++) {
            if(mGroupActives[i] == group) {
                group.Clear();

                mGroupActives.RemoveAt(i);
                mGroupCache.Add(group);
                break;
            }
        }
    }

    public void ClearAllGroup() {
        for(int i = 0; i < mGroupActives.Count; i++) {
            var grp = mGroupActives[i];
            grp.Clear();
            mGroupCache.Add(grp);
        }

        mGroupActives.Clear();
    }

    public Group GetGroup(Blob blob) {
        for(int i = 0; i < mGroupActives.Count; i++) {
            var grp = mGroupActives[i];
            if(grp.IsBlobInGroup(blob))
                return grp;
        }

        return null;
    }

    void OnDestroy() {
        signalListenBlobDragBegin.callback -= OnBlobDragBegin;
        signalListenBlobDragEnd.callback -= OnBlobDragEnd;
        signalListenBlobDespawn.callback -= OnBlobDespawn;

        signalListenBlobConnectDelete.callback -= OnBlobConnectDelete;
    }

    void Awake() {
        mPool = M8.PoolController.CreatePool(poolGroup);
        mPool.AddType(connectTemplate, capacity, capacity);

        //setup group
        mGroupActives = new M8.CacheList<Group>(groupCapacity);
        mGroupCache = new M8.CacheList<Group>(groupCapacity);

        //fill up cache
        for(int i = 0; i < groupCapacity; i++)
            mGroupCache.Add(new Group());

        signalListenBlobDragBegin.callback += OnBlobDragBegin;
        signalListenBlobDragEnd.callback += OnBlobDragEnd;
        signalListenBlobDespawn.callback += OnBlobDespawn;

        signalListenBlobConnectDelete.callback += OnBlobConnectDelete;
    }

    void SetCurGroupDraggingOtherBlobHighlight(bool isHighlight) {
        if(curGroupDragging != null) {
            Blob otherBlob = null;
            if(curGroupDragging.connectOp)
                otherBlob = curGroupDragging.connectOp.GetLinkedBlob(curBlobDragging);
            else if(curGroupDragging.connectEq)
                otherBlob = curGroupDragging.connectEq.GetLinkedBlob(curBlobDragging);
            if(otherBlob)
                otherBlob.ApplyJellySpriteMaterial(isHighlight ? otherBlob.hoverDragMaterial : null);
        }
    }

    void Update() {
        //we are dragging
        if(mCurConnectDragging) {
            if(curBlobDragging) {
                //setup op
                OperatorType dragOp = mCurOp;

                if(curGroupDragging != null) {
                    if(curGroupDragging.IsBlobOp(curBlobDragging))
                        dragOp = OperatorType.Equal;
                }

                //setup link position
                Vector2 connectPtStart, connectPtEnd;

                connectPtEnd = curBlobDragging.dragPoint;

                //check if dragging inside
                var dragJellySprRef = curBlobDragging.dragPointerJellySpriteRefPt;
                if(dragJellySprRef && dragJellySprRef.ParentJellySprite == curBlobDragging.gameObject) {
                    //start set the same as end.
                    connectPtStart = connectPtEnd;

                    //unhighlight the other connection
                    SetCurGroupDraggingOtherBlobHighlight(false);
                }
                else {
                    connectPtStart = curBlobDragging.transform.position;

                    //check if we are over another blob
                    if(curBlobDragging.dragPointerJellySpriteRefPt) {
                        //check if it is in a group and we are setting it up as an equal connect
                        var blobGO = curBlobDragging.dragPointerJellySpriteRefPt.ParentJellySprite;
                        var toGrp = GetGroup(blobGO);
                        if(toGrp != null && toGrp != curGroupDragging) {
                            if(toGrp.IsBlobOp(blobGO))
                                dragOp = OperatorType.Equal;
                        }

                        //highlight the other connection
                        SetCurGroupDraggingOtherBlobHighlight(true);
                    }
                    else {
                        //unhighlight the other connection
                        SetCurGroupDraggingOtherBlobHighlight(false);
                    }
                }

                mCurConnectDragging.op = dragOp;
                mCurConnectDragging.UpdateConnecting(connectPtStart, connectPtEnd, curBlobDragging.radius, curBlobDragging.color);
            }
            else //blob being dragged on released?
                ReleaseDragging();
        }
    }

    void OnBlobDragBegin(Blob blob) {
        if(!mCurConnectDragging) {
            mConnectSpawnParms.Clear();
            //params?

            mCurConnectDragging = mPool.Spawn<BlobConnect>(connectTemplate.name, "", null, mConnectSpawnParms);
        }

        curBlobDragging = blob;

        var toOp = mCurOp;

        //determine what the operator is based on blob's current connect state

        mCurConnectDragging.op = toOp;
        mCurConnectDragging.state = BlobConnect.State.Connecting;

        if(mCurConnectDragging.connectingSpriteRender)
            mCurConnectDragging.connectingSpriteRender.color = curBlobDragging.color;

        //determine if this is in a group
        curGroupDragging = GetGroup(blob);
        if(curGroupDragging != null) {
            //highlight entire group

            //flip operand order based if it's the first operand
            if(blob == curGroupDragging.blobOpLeft) {
                curGroupDragging.blobOpLeft = curGroupDragging.blobOpRight;
                curGroupDragging.blobOpRight = blob;
            }
        }
    }

    void OnBlobDragEnd(Blob blob) {
        SetCurGroupDraggingOtherBlobHighlight(false);

        //determine if we can connect to a new blob
        var blobRefPt = blob.dragPointerJellySpriteRefPt;
        if(blobRefPt != null && blobRefPt.ParentJellySprite != blob.gameObject) {
            Group evalGroup = null;

            var endBlob = blobRefPt.ParentJellySprite.GetComponent<Blob>();

            //determine op
            var toOp = mCurOp;
            if(curGroupDragging != null) {
                if(curGroupDragging.IsBlobOp(curBlobDragging)) {
                    //cancel if dragging to the same group as ops
                    if(curGroupDragging.IsBlobOp(endBlob))
                        toOp = OperatorType.None;
                    else
                        toOp = OperatorType.Equal;
                }
            }

            //update link groups
            if(toOp != OperatorType.None) {
                //remove endBlob from its group
                var endGroup = GetGroup(endBlob);
                if(endGroup != null && endGroup != curGroupDragging) {
                    //if we are dragging to apply equal op, then remove it from end group and move to drag group
                    if(toOp == OperatorType.Equal) {
                        RemoveBlobFromGroup(endGroup, endBlob);

                        curGroupDragging.SetEq(endBlob, mCurConnectDragging);
                        evalGroup = curGroupDragging;
                    }
                    else {
                        //if dragging to one of the operands of end group, then move blob to this group
                        if(endGroup.IsBlobOp(endBlob)) {
                            toOp = OperatorType.Equal;

                            //move blob to end group as the equal
                            endGroup.SetEq(blob, mCurConnectDragging);
                            evalGroup = endGroup;

                            //remove from dragging group
                            if(curGroupDragging != null)
                                RemoveBlobFromGroup(curGroupDragging, blob);
                        }
                        else {
                            //remove blobs from its group, and create new group together
                            if(curGroupDragging != null)
                                RemoveBlobFromGroup(curGroupDragging, blob);

                            RemoveBlobFromGroup(endGroup, endBlob);

                            var newGrp = NewGroup();
                            newGrp.SetOp(blob, endBlob, mCurConnectDragging);
                            groupAddedCallback?.Invoke(newGrp);
                        }
                    }
                }
                else if(curGroupDragging != null) {
                    if(toOp == OperatorType.Equal) {
                        //refresh equal
                        curGroupDragging.SetEq(endBlob, mCurConnectDragging);
                        evalGroup = curGroupDragging;
                    }
                    else //re-establish group
                        curGroupDragging.SetOp(blob, endBlob, mCurConnectDragging);
                }
                else {
                    //create new group
                    var newGrp = NewGroup();
                    newGrp.SetOp(blob, endBlob, mCurConnectDragging);
                    groupAddedCallback?.Invoke(newGrp);
                }

                //setup link
                mCurConnectDragging.op = toOp;
                mCurConnectDragging.ApplyLink(blob, endBlob);
            }
            else //cancel
                mCurConnectDragging.Release();

            mCurConnectDragging = null;
            curBlobDragging = null;
            curGroupDragging = null;

            //send call to evaluate a group
            if(evalGroup != null && evalGroup.isComplete)
                evaluateCallback?.Invoke(evalGroup);
        }
        else
            ReleaseDragging();
    }

    void OnBlobDespawn(Blob blob) {
        //check which connects need to be purged.
        for(int i = mGroupActives.Count - 1; i >= 0; i--) {
            var grp = mGroupActives[i];
            if(grp.RemoveBlob(blob)) {
                if(grp.isEmpty) {
                    mGroupActives.RemoveAt(i);
                    mGroupCache.Add(grp);
                }
            }
        }
    }

    void OnBlobConnectDelete(BlobConnect blobConnect) {
        //check which connects need to be purged.
        for(int i = mGroupActives.Count - 1; i >= 0; i--) {
            var grp = mGroupActives[i];
            if(grp.connectOp == blobConnect)
                grp.Clear();
            else if(grp.connectEq == blobConnect)
                grp.ClearEq();

            if(grp.isEmpty) {
                mGroupActives.RemoveAt(i);
                mGroupCache.Add(grp);
            }
        }
    }

    private void RemoveBlobFromGroup(Group grp, Blob blob) {
        grp.RemoveBlob(blob);
        if(grp.isEmpty) {
            mGroupActives.Remove(grp);
            mGroupCache.Add(grp);
        }
    }

    private Group NewGroup() {
        var newGrp = mGroupCache.Remove();
        if(newGrp != null)
            mGroupActives.Add(newGrp);
        return newGrp;
    }

    private Group GetGroup(GameObject blobGO) {
        Group grp = null;

        for(int i = 0; i < mGroupActives.Count; i++) {
            if(mGroupActives[i].IsBlobInGroup(blobGO)) {
                grp = mGroupActives[i];
                break;
            }
        }

        return grp;
    }
}
