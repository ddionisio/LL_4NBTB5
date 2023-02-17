using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOperation {
    public struct Cell {
        public Operation op;
        public bool isSolved;

        public bool isValid { get { return op.op != OperatorType.None; } }

        public static Cell Invalid { get { return new Cell(new Operation { op = OperatorType.None }); } }

        public Cell(Operation val) {
            op = val;
            isSolved = false;
        }
    }

    public Operation operation { get; private set; }

    public int areaRowCount { get { return mAreaOperations.GetLength(0); } }

    public int areaColCount { get { return mAreaOperations.GetLength(1); } }

    public Cell mainCell {
        get {
            if(areaRowCount == 0 || areaColCount == 0)
                return Cell.Invalid;

            return mAreaOperations[areaRowCount - 1, areaColCount - 1];
        }
    }

    private Cell[,] mAreaOperations; //[row, col]

    public void Setup(int factorLeft, int factorRight) {
        operation = new Operation { operand1 = factorLeft, operand2 = factorRight, op = OperatorType.Multiply };

        var factorLeftDigitCount = WholeNumber.DigitCount(factorLeft);
        var factorRightDigitCount = WholeNumber.DigitCount(factorRight);

        mAreaOperations = new Cell[factorRightDigitCount, factorLeftDigitCount];

        for(int r = 0; r < factorRightDigitCount - 1; r++) {
            for(int c = 0; c < factorLeftDigitCount - 1; c++) {
                mAreaOperations[r, c] = Cell.Invalid;
            }
        }

        mAreaOperations[factorRightDigitCount - 1, factorLeftDigitCount - 1] = new Cell(operation);
    }

    /// <summary>
    /// Return area operation, if operation type is None, then it is invalid.
    /// </summary>
    public Cell GetAreaOperation(int row, int col) {
        if(row >= areaRowCount || col >= areaColCount)
            return Cell.Invalid;

        return mAreaOperations[row, col];
    }

    public void SetAreaOperationSolved(int row, int col, bool isSolved) {
        if(row < areaRowCount && col < areaColCount)
            mAreaOperations[row, col].isSolved = isSolved;
    }

    public bool SplitAreaCol(int col, int digitCount) {
        if(areaRowCount == 0 || col >= areaColCount)
            return false;

        var areaOp = mAreaOperations[areaRowCount - 1, col];

        //grab modified number and new number
        int newNum, digitNum;

        WholeNumber.ExtractDigit(areaOp.op.operand1, digitCount, out newNum, out digitNum);

        //new number is zero, can't split
        if(digitNum == 0)
            return false;

        //modify current col, and set new col
        int insertCol = digitCount;

        for(int row = 0; row < areaRowCount; row++) {
            areaOp = mAreaOperations[row, col];
            areaOp.op.operand1 = newNum;

            mAreaOperations[row, col] = areaOp;

            int prevInsertNum = mAreaOperations[row, insertCol].op.operand1;

            mAreaOperations[row, insertCol] = new Cell(new Operation { operand1 = prevInsertNum + digitNum, operand2 = areaOp.op.operand2, op = areaOp.op.op });
        }

        return true;
    }

    public bool SplitAreaRow(int row, int digitCount) {
        if(areaColCount == 0 || row >= areaRowCount)
            return false;

        var areaOp = mAreaOperations[row, areaColCount - 1];

        //grab modified number and new number
        int newNum, digitNum;

        WholeNumber.ExtractDigit(areaOp.op.operand2, digitCount, out newNum, out digitNum);

        //new number is zero, can't split
        if(digitNum == 0)
            return false;

        //modify current row, and set new row
        int insertRow = digitCount;

        for(int col = 0; col < areaColCount; col++) {
            areaOp = mAreaOperations[row, col];
            areaOp.op.operand2 = newNum;

            mAreaOperations[row, col] = areaOp;

            int prevInsertNum = mAreaOperations[insertRow, col].op.operand2;

            mAreaOperations[insertRow, col] = new Cell(new Operation { operand1 = areaOp.op.operand1, operand2 = prevInsertNum + digitNum, op = areaOp.op.op });
        }

        return true;
    }

    public bool MergeCol(int colSrc, int colDest) {
        if(colSrc == colDest)
            return false;

        if(areaRowCount == 0)
            return false;

        var colCount = areaColCount;
        if(colSrc >= colCount || colDest >= colCount)
            return false;

        var areaSrcOp = mAreaOperations[0, colSrc];
        var areaDestOp = mAreaOperations[0, colDest];

        var newDestNum = areaDestOp.op.operand1 + areaSrcOp.op.operand1;

        //modify col dest, invalidate source col
        for(int row = 0; row < areaRowCount; row++) {
            mAreaOperations[row, colDest].op.operand1 = newDestNum;

            mAreaOperations[row, colSrc] = Cell.Invalid;
        }

        return true;
    }

    public bool MergeRow(int rowSrc, int rowDest) {
        if(rowSrc == rowDest)
            return false;

        if(areaColCount == 0)
            return false;

        var rowCount = areaRowCount;
        if(rowSrc >= rowCount || rowDest >= rowCount)
            return false;

        var areaSrcOp = mAreaOperations[rowSrc, 0];
        var areaDestOp = mAreaOperations[rowDest, 0];

        var newDestNum = areaDestOp.op.operand2 + areaSrcOp.op.operand2;

        //modify row dest, invalidate source row
        for(int col = 0; col < areaColCount; col++) {
            mAreaOperations[rowDest, col].op.operand2 = newDestNum;

            mAreaOperations[rowSrc, col] = Cell.Invalid;
        }

        return true;
    }
}
