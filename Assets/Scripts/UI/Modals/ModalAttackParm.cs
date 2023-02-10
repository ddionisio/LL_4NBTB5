using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Common parameters shared by Attack Phase
/// </summary>
public struct ModalAttackParm {
    public const string areaOp = "ao";
    public const string mistakeCount = "hp";

    public static AreaOperation GetAreaOperation(M8.GenericParams parms) {
        if(parms != null && parms.ContainsKey(areaOp))
            return parms.GetValue<AreaOperation>(areaOp);

        return null;
    }

    public static int GetMistakeCount(M8.GenericParams parms) {
        if(parms != null && parms.ContainsKey(mistakeCount))
            return parms.GetValue<int>(mistakeCount);

        return 0;
    }
}
