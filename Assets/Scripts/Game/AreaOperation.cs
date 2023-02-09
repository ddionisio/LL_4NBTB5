using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOperation {
    public struct Cell {
        public Operation op;
        public bool isSolved;

        public Cell(Operation val) {
            op = val;
            isSolved = false;
        }
    }

    public Operation initialOperation { get; private set; }

    public int areaRowCount {
        get {
            if(mAreaOperations == null)
                return 0;

            return mAreaOperations.Count;            
        }
    }

    public int areaColCount {
        get {
            if(mAreaOperations == null || mAreaOperations.Count == 0)
                return 0;

            return mAreaOperations[0].Count;
        }
    }

    private List<List<Cell>> mAreaOperations; //[row, col]

    public void Init(int factorLeft, int factorRight) {
        initialOperation = new Operation { operand1 = factorLeft, operand2 = factorRight, op = OperatorType.Multiply };

        mAreaOperations = new List<List<Cell>>(GameData.instance.areaRowCapacity);
        mAreaOperations.Add(new List<Cell>(GameData.instance.areaColCapacity));

        mAreaOperations[0].Add(new Cell(initialOperation));
    }

    /// <summary>
    /// Return area operation, if operation type is None, then it is invalid.
    /// </summary>
    public Cell GetAreaOperation(int row, int col) {
        if(row >= areaRowCount || col >= areaColCount)
            return new Cell { op = new Operation { op = OperatorType.None }, isSolved = false };

        return mAreaOperations[row][col];
    }

    public void SetAreaOperationSolved(int row, int col, bool isSolved) {
        if(row < areaRowCount && col < areaColCount) {
            var areaOp = mAreaOperations[row][col];
            areaOp.isSolved = isSolved;
            mAreaOperations[row][col] = areaOp;
        }
    }

    public bool SplitAreaCol(int col, int digitCount) {
        if(areaRowCount == 0 || col >= areaColCount)
            return false;

        var areaRow = mAreaOperations[0];
        var areaOp = areaRow[col];

        //grab modified number and new number
        int newNum, digitNum;

        WholeNumber.ExtractDigit(areaOp.op.operand1, digitCount, out newNum, out digitNum);

        //new number is zero, can't split
        if(digitNum == 0)
            return false;

        //modify current col
        for(int row = 0; row < mAreaOperations.Count; row++) {
            areaRow = mAreaOperations[row];

            areaOp = areaRow[col];
            areaOp.op.operand1 = newNum;

            areaRow[col] = areaOp;
        }

        //find column to insert to
        int colInsert;
        for(colInsert = 0; colInsert < areaRow.Count; colInsert++) {
            if(digitNum > areaRow[colInsert].op.operand1)
                break;
        }

        //add new col
        for(int row = 0; row < mAreaOperations.Count; row++) {
            areaRow = mAreaOperations[row];

            areaOp = areaRow[col];

            areaRow.Insert(colInsert, new Cell(new Operation { operand1 = digitNum, operand2 = areaOp.op.operand2, op = areaOp.op.op }));
        }

        return true;
    }

    public bool SplitAreaRow(int row, int digitCount) {
        if(areaColCount == 0 || row >= areaRowCount)
            return false;

        var areaRow = mAreaOperations[row];
        var areaOp = areaRow[0];

        //grab modified number and new number
        int newNum, digitNum;

        WholeNumber.ExtractDigit(areaOp.op.operand2, digitCount, out newNum, out digitNum);

        //new number is zero, can't split
        if(digitNum == 0)
            return false;

        //modify current row
        for(int col = 0; col < areaRow.Count; col++) {
            areaOp = areaRow[col];
            areaOp.op.operand2 = newNum;

            areaRow[col] = areaOp;
        }

        //find row to insert to
        int rowInsert;
        for(rowInsert = 0; rowInsert < mAreaOperations.Count; rowInsert++) {
            if(digitNum > mAreaOperations[rowInsert][0].op.operand2)
                break;
        }

        //add new row
        var newAreaRow = new List<Cell>(GameData.instance.areaColCapacity);

        for(int col = 0; col < areaRow.Count; col++) {
            areaOp = areaRow[col];

            newAreaRow.Add(new Cell(new Operation { operand1 = areaOp.op.operand1, operand2 = digitNum, op = areaOp.op.op }));
        }

        mAreaOperations.Insert(rowInsert, newAreaRow);

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

        var areaSrcOp = mAreaOperations[0][colSrc];
        var areaDestOp = mAreaOperations[0][colDest];

        var newDestNum = areaDestOp.op.operand1 + areaSrcOp.op.operand1;

        //modify col dest, then delete source col
        for(int row = 0; row < mAreaOperations.Count; row++) {
            var areaRow = mAreaOperations[row];

            var areaOp = areaRow[colDest];
            areaOp.op.operand1 = newDestNum;
            areaRow[colDest] = areaOp;

            mAreaOperations[row].RemoveAt(colSrc);
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

        var areaSrcOp = mAreaOperations[rowSrc][0];
        var areaDestOp = mAreaOperations[rowDest][0];

        var newDestNum = areaDestOp.op.operand2 + areaSrcOp.op.operand2;

        //modify row dest
        var areaDestRow = mAreaOperations[rowDest];
        for(int col = 0; col < areaDestRow.Count; col++) {
            var areaOp = areaDestRow[col];
            areaOp.op.operand2 = newDestNum;
            areaDestRow[col] = areaOp;
        }

        //delete row src
        mAreaOperations.RemoveAt(rowSrc);

        return true;
    }
}
