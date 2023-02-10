using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct WholeNumber {
    public static int TenExponent(int exp) {
        if(exp == 0)
            return 1;
        else if(exp > 0) {
            int ret = 10;
            for(int i = 0; i < exp; i++)
                ret *= 10;

            return ret;
        }

        //can't really do negative
        return 0;
    }

    /// <summary>
    /// Grab the digit from number, count from right to left with 0 being the first digit.
    /// </summary>
    public static void ExtractDigit(int number, int digitCount, out int newNumber, out int digitNumber) {
        digitNumber = number;

        if(digitCount > 0) {
            var digitShift = TenExponent(digitCount);

            digitNumber /= digitShift;

            digitNumber %= 10;

            digitNumber *= digitShift;
        }
        else
            digitNumber %= 10;

        newNumber = number - digitNumber;
    }

    public static int DigitCount(int number) {
        int count = 0;
        while(number > 0) {
            number /= 10;
            count++;
        }

        return count;
    }
}
