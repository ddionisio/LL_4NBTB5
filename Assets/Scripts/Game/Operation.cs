using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Operation
{
    public int operand1;
    public OperatorType op;
    public int operand2;

    public int equal {
        get {
            switch(op) {
                case OperatorType.Multiply:
                    return operand1 * operand2;
                case OperatorType.Divide:
                    return operand2 != 0 ? operand1 / operand2 : 0;
                default:
                    return 0;
            }
        }
    }
}
