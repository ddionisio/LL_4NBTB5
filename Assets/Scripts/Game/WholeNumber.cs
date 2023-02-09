using System.Collections;
using System.Collections.Generic;

public struct WholeNumber {
    /// <summary>
    /// Grab the digit from number, count from right to left with 0 being the first digit.
    /// </summary>
    public static void ExtractDigit(int number, int digitCount, out int newNumber, out int digitNumber) {
        digitNumber = number;

        if(digitCount > 0)
            digitNumber /= 10 * digitCount;

        digitNumber %= 10;

        if(digitCount > 0)
            digitNumber *= 10 * digitCount;

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
