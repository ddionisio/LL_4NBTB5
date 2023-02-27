using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Common parameters shared by Attack Phase
/// </summary>
public class ModalAttackParams : M8.GenericParams {
    public const string areaOp = "ao";
    public const string mistakeInfo = "mi";

    public AreaOperation GetAreaOperation() {
        if(ContainsKey(areaOp))
            return GetValue<AreaOperation>(areaOp);

        return null;
    }

    public void SetAreaOperation(AreaOperation aOp) {
        this[areaOp] = aOp;
    }

    public MistakeInfo GetMistakeInfo() {
        if(ContainsKey(mistakeInfo))
            return GetValue<MistakeInfo>(mistakeInfo);

        return null;
    }

    public void SetMistakeInfo(MistakeInfo aInfo) {
        this[mistakeInfo] = aInfo;
    }
}
