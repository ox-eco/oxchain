namespace OX
{
    /// <summary>
    /// Accurate to 10^-8 64-bit fixed-point numbers minimize rounding errors.
    /// By controlling the accuracy of the multiplier, rounding errors can be completely eliminated.
    /// </summary>
    public static class Fixed8Helper
    {
        public static long GetInternalValue(this Fixed8 amount)
        {
            return amount.value;
        }
    }
}
