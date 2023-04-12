using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

using TMPro;

/// <summary>
/// Basic calculator
/// </summary>
public class ModalCalculator : M8.ModalController, M8.IModalPush, M8.IModalPop, M8.IModalActive {
    public const string parmInitValue = "initialValue";
    public const string parmMaxDigit = "maxDigit";
    public const string parmKeyboardFlags = "keyboardFlags";

    [System.Flags]
    public enum InputKeyboardFlag {
        None = 0x0,

        Numeric = 0x1,

        //functions
        Proceed = 0x2,
        Erase = 0x4
    }

    public enum InputType {
        Invalid,

        Numeric,

        //operators        
        Add,
        Subtract,
        Multiply,
        Divide
    }

    public enum ValueMod {
        Square,
        SquareRoot,
        Invert,
        Negate,
        Cos,
        Sin,
        Tan
    }

    public struct InputData {
        public InputType type;
        public string displayText;
        public double value;

        public bool isOperator {
            get {
                return IsOperator(type);
            }
        }
    }
    
    [Header("Data")]
    [SerializeField]
    int _defaultMaxDigits = 16;

    [M8.EnumMask]
    [SerializeField]
    InputKeyboardFlag _inputKeyboardFlags = InputKeyboardFlag.Numeric | InputKeyboardFlag.Proceed | InputKeyboardFlag.Erase;

    [Header("Display")]
    public TMP_Text inputLabel;
    public TMP_Text numericLabel;

    [Header("Signal Listens")]
    public M8.SignalFloat signalValueChanged;
    public M8.SignalInteger signalInputNumber;
    public M8.Signal signalErase;

    [Header("Signal Invokes")]
    public M8.SignalFloat signalValueUpdate;
    public M8.SignalFloat signalProceed;

    public float curValueFloat { get { return (float)mCurValue; } }

    private InputKeyboardFlag mInputKeyboardFlags;

    private double mCurValue;
    private bool mCurValueIsSpecial; //when we click on constants such as PI
    private int mMaxDigits;

    private StringBuilder mCurInput = new StringBuilder();
    private List<InputData> mInputs = new List<InputData>();

    private bool mIsActive;

    public void Clear() {
        mInputs.Clear();
        UpdateInputDisplay();

        ClearEntry();
    }

    public void ClearEntry() {
        //if only one input, clear that as well
        if(mInputs.Count == 1) {
            mInputs.Clear();
            UpdateInputDisplay();
        }

        ApplyCurrentValue(0);
    }

    public void Erase() {
        if(mCurInput.Length > 0) {
            //reset to zero if single digit with negative, or if it's just single digit
            if((mCurInput.Length == 2 && mCurInput[0] == '-') || mCurInput.Length == 1) {
                mCurInput.Remove(0, mCurInput.Length);
                mCurInput.Append('0');
            }
            else
                mCurInput.Remove(mCurInput.Length - 1, 1);
        }

        UpdateCurrentValueFromInput();
    }

    public void InputNumber(int num) {        
        //don't add if we are already at limit
        int count = mCurInput.Length;
        if(count == 1 && mCurInput[0] == '0') {
            //special case, replace 0 if it's the only character
            mCurInput.Clear();
        }
        else if(count > 0) {
            if(mCurInput[0] == '-')
                count--;
            if(CurrentInputGetPeriodIndex() != -1)
                count--;

            if(count >= mMaxDigits)
                return;
        }

        mCurInput.Append(num);

        UpdateCurrentValueFromInput();
    }

    public void Square() {
        ApplyMod(ValueMod.Square);
    }

    public void SquareRoot() {
        ApplyMod(ValueMod.SquareRoot);
    }

    public void Invert() {
        ApplyMod(ValueMod.Invert);
    }

    public void Negate() {
        //ignore if value is 0
        if(mCurValue == 0) {
            return;
        }
        if(mCurInput.Length > 0) {
            //just invert current input
            if(mCurInput[0] == '-')
                mCurInput.Remove(0, 1);
            else
                mCurInput.Insert(0, '-');

            UpdateCurrentValueFromInput();
        }
        else {
            ApplyMod(ValueMod.Negate);
        }
    }

    public void Period() {
        if(mCurInput.Length == 0) {
            mCurInput.Append("0.");
            UpdateCurrentValueFromInput();
        }
        else if(CurrentInputGetPeriodIndex() == -1) {
            mCurInput.Append('.');
            UpdateCurrentValueFromInput();
        }
    }

    public void Divide() {
        AddOperator(InputType.Divide);
    }

    public void Multiply() {
        AddOperator(InputType.Multiply);
    }

    public void Add() {
        AddOperator(InputType.Add);
    }

    public void Subtract() {
        AddOperator(InputType.Subtract);
    }

    public void Equal() {
        EvaluateInputsToCurrentValue();

        mInputs.Clear();
        UpdateInputDisplay();
    }

    public void PI() {
        ApplyCurrentValue(System.Math.PI);
        mCurValueIsSpecial = true;
    }

    public void Cos() {
        ApplyMod(ValueMod.Cos);
    }

    public void Sin() {
        ApplyMod(ValueMod.Sin);
    }

    public void Tan() {
        ApplyMod(ValueMod.Tan);
    }

    public void Proceed() {
        if(signalProceed)
            signalProceed.Invoke((float)mCurValue);
    }

    public void SetCurrentValue(double val) {
        mCurValue = val; //prevent value update signal from invoking when calling SetCurrentValue
        ApplyCurrentValue(val);

        ClearInput();
    }

    void M8.IModalPop.Pop() {
        if(signalValueChanged)
            signalValueChanged.callback -= OnValueChanged;
        if(signalInputNumber)
            signalInputNumber.callback -= InputNumber;
        if(signalErase)
            signalErase.callback -= Erase;
    }

    void M8.IModalPush.Push(M8.GenericParams parms) {
        double val = 0;

        if(parms != null) {
            if(parms.ContainsKey(parmInitValue)) {
                object obj = parms.GetValue<object>(parmInitValue);
                if(obj is float)
                    val = (float)obj;
                else if(obj is double)
                    val = (double)obj;
                else if(obj is int)
                    val = (int)obj;
                else
                    val = 0;
            }

            if(parms.ContainsKey(parmMaxDigit)) {
                mMaxDigits = parms.GetValue<int>(parmMaxDigit);
                if(mMaxDigits <= 0)
                    mMaxDigits = _defaultMaxDigits;
            }
            else
                mMaxDigits = _defaultMaxDigits;

            if(parms.ContainsKey(parmKeyboardFlags)) {
                mInputKeyboardFlags = parms.GetValue<InputKeyboardFlag>(parmKeyboardFlags);
            }
            else
                mInputKeyboardFlags = _inputKeyboardFlags;
        }

        SetCurrentValue(val);

        if(signalValueChanged)
            signalValueChanged.callback += OnValueChanged;
        if(signalInputNumber)
            signalInputNumber.callback += InputNumber;
        if(signalErase)
            signalErase.callback += Erase;
    }

    void M8.IModalActive.SetActive(bool aActive) {
        mIsActive = aActive;
    }

    void Update() {
        if(mIsActive) {
            //allow keyboard input?
            if((mInputKeyboardFlags & InputKeyboardFlag.Numeric) != InputKeyboardFlag.None) {
                if(Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
                    InputNumber(0);
                else if(Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                    InputNumber(1);
                else if(Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                    InputNumber(2);
                else if(Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
                    InputNumber(3);
                else if(Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
                    InputNumber(4);
                else if(Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
                    InputNumber(5);
                else if(Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
                    InputNumber(6);
                else if(Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
                    InputNumber(7);
                else if(Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
                    InputNumber(8);
                else if(Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
                    InputNumber(9);
            }

            if((mInputKeyboardFlags & InputKeyboardFlag.Proceed) != InputKeyboardFlag.None) {
                if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    Proceed();
            }

            if((mInputKeyboardFlags & InputKeyboardFlag.Erase) != InputKeyboardFlag.None) {
                if(Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace))
                    Erase();
            }
        }
    }

    void OnValueChanged(float val) {
        SetCurrentValue(val);
    }

    private static double EvaluateInputType(InputType type, double lhs, double rhs) {
        switch(type) {
            case InputType.Add:
                return lhs + rhs;
            case InputType.Subtract:
                return lhs - rhs;
            case InputType.Multiply:
                return lhs * rhs;
            case InputType.Divide:
                return lhs / rhs;
            default:
                return lhs;
        }
    }

    private static bool IsOperator(InputType type) {
        return type != InputType.Invalid && type != InputType.Numeric;
    }

    private void ApplyMod(ValueMod mod) {
        var inputLastInd = mInputs.Count - 1;

        double val;
        if(inputLastInd != -1 && mInputs[inputLastInd].type == InputType.Numeric)
            val = mInputs[inputLastInd].value;
        else
            val = mCurValue;

        string strFormat;
        double newVal;

        switch(mod) {
            case ValueMod.Square:
                strFormat = "({0})²";
                newVal = val * val;
                break;
            case ValueMod.SquareRoot:
                strFormat = "√({0})";
                newVal = System.Math.Sqrt(val);
                break;
            case ValueMod.Invert:
                strFormat = "1/({0})";
                newVal = 1.0 / val;
                break;
            case ValueMod.Negate:
                strFormat = "-({0})";
                newVal = -val;
                break;
            case ValueMod.Cos:
                strFormat = "cos({0})";
                newVal = System.Math.Cos(val * (System.Math.PI / 180.0));
                break;
            case ValueMod.Sin:
                strFormat = "sin({0})";
                newVal = System.Math.Sin(val * (System.Math.PI / 180.0));
                break;
            case ValueMod.Tan:
                strFormat = "tan({0})";
                newVal = System.Math.Tan(val * (System.Math.PI / 180.0));
                break;
            default:
                strFormat = "{0}";
                newVal = 0f;
                break;
        }

        //check for last value from input and modify it
        if(inputLastInd != -1 && mInputs[inputLastInd].type == InputType.Numeric) {
            //encapsulate value
            var inputVal = mInputs[inputLastInd];
            ModifyInput(inputLastInd, InputType.Numeric, string.Format(strFormat, inputVal.displayText), newVal);
        }
        else {
            //apply to current input and add to input
            AddInput(InputType.Numeric, string.Format(strFormat, mCurValue), newVal);
        }

        //clear input and apply value
        ApplyCurrentValue(newVal);
    }

    private void AddOperator(InputType type) {
        string opText;

        switch(type) {
            case InputType.Add:
                opText = "+";
                break;
            case InputType.Subtract:
                opText = "-";
                break;
            case InputType.Multiply:
                opText = "x";
                break;
            case InputType.Divide:
                opText = "/";
                break;
            default:
                opText = "";
                break;
        }

        int inputLastIndex = mInputs.Count - 1;

        //input empty?
        if(inputLastIndex < 0) {
            //add current input and operator
            AddInput(InputType.Numeric, mCurValue.ToString(), mCurValue);

            AddInput(type, opText, 0);

            mCurInput.Remove(0, mCurInput.Length);
        }
        else {
            if(mInputs[inputLastIndex].type == InputType.Numeric) {
                //add operator
                AddInput(type, opText, 0);
            }
            //check if current input is filled
            else if(mCurInput.Length == 0 && !mCurValueIsSpecial) {
                //replace last operator
                if(mInputs[inputLastIndex].isOperator)
                    ModifyInput(inputLastIndex, type, opText, 0);
            }
            else {
                //add current input, evaluate, and then add operator
                AddInput(InputType.Numeric, mCurValue.ToString(), mCurValue);

                //evaluate value
                EvaluateInputsToCurrentValue();

                AddInput(type, opText, 0);
            }
        }
    }

    private void EvaluateInputsToCurrentValue() {
        if(mInputs.Count < 2)
            return;

        double evalValue = 0;
        InputType lastType = InputType.Invalid;

        for(int i = 0; i < mInputs.Count; i++) {
            var inp = mInputs[i];

            if(!inp.isOperator) {
                //value?
                if(lastType == InputType.Invalid) //just set current value
                    evalValue = inp.value;
                else if(inp.type == InputType.Numeric) {
                    //apply last operator and update curVal
                    evalValue = EvaluateInputType(lastType, evalValue, inp.value);
                }
            }

            lastType = inp.type;
        }

        //operate on current value
        if(IsOperator(lastType))
            evalValue = EvaluateInputType(lastType, evalValue, mCurValue);

        ApplyCurrentValue(evalValue);
    }

    private void ModifyInput(int ind, InputType type, string displayText, double val) {
        mInputs[ind] = new InputData() { type = type, displayText = displayText, value = val };
        UpdateInputDisplay();
    }

    private void AddInput(InputType type, string displayText, double val) {
        mInputs.Add(new InputData() { type = type, displayText = displayText, value = val });
        UpdateInputDisplay();
    }

    private int GetLastInputNumericIndex() {
        for(int i = mInputs.Count - 1; i >= 0; i--) {
            if(mInputs[i].type == InputType.Numeric) {
                return i;
            }
        }

        return -1;
    }

    private int CurrentInputGetPeriodIndex() {
        for(int i = mCurInput.Length - 1; i >= 0; i--) {
            if(mCurInput[i] == '.')
                return i;
        }
        return -1;
    }

    private void ClearInput() {
        mInputs.Clear();
        UpdateInputDisplay();
    }

    private void UpdateInputDisplay() {
        if(inputLabel) {
            if(mInputs.Count > 0) {
                var sb = new StringBuilder();
                for(int i = 0; i < mInputs.Count; i++) {
                    sb.Append(mInputs[i].displayText);

                    if(i < mInputs.Count - 1)
                        sb.Append(' ');
                }

                inputLabel.text = sb.ToString();
            }
            else
                inputLabel.text = "";
        }
    }

    private void ApplyCurrentValue(double val) {
        var prevVal = mCurValue;

        mCurInput.Remove(0, mCurInput.Length);

        mCurValue = val;
        mCurValueIsSpecial = false;

        UpdateCurrentInputDisplay();

        if(mCurValue != prevVal && signalValueUpdate)
            signalValueUpdate.Invoke((float)mCurValue);
    }

    private void UpdateCurrentValueFromInput() {
        //var prevVal = mCurValue;

        int startInd = 0;
        int len = mCurInput.Length;

        if(len > 0) {
            double parseVal;
            if(double.TryParse(mCurInput.ToString(startInd, len), out parseVal))
                mCurValue = parseVal;
        }
        else
            mCurValue = 0;

        mCurValueIsSpecial = false;

        //update display
        UpdateCurrentInputDisplay();

        //if(mCurValue != prevVal && signalValueUpdate)
        if(signalValueUpdate)
            signalValueUpdate.Invoke((float)mCurValue);
    }

    private void UpdateCurrentInputDisplay() {
        if(!numericLabel)
            return;

        if(mCurInput.Length > 0) {
            numericLabel.text = mCurInput.ToString();
        }
        else {
            if(System.Math.Abs(mCurValue) <= Mathf.Epsilon)
                numericLabel.text = "0";
            else
                numericLabel.text = mCurValue.ToString();
        }
    }
}
