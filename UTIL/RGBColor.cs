namespace Utils
{
    /// <summary>
    /// This struct represents a color consisting of three color components.
    /// They represent an additive color mixture (rgb) which is commonly used in
    /// LED products.
    /// </summary>
    public struct RGBColor
    {
        /// <summary>
        /// Red part of the color
        /// </summary>
        public byte R;
        /// <summary>
        /// Green part of the color
        /// </summary>
        public byte G;
        /// <summary>
        /// Blue part of the color
        /// </summary>
        public byte B;

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current 
        /// <see cref="T:LedControl.Common.RGBColor"/>.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current 
        /// <see cref="T:LedControl.Common.RGBColor"/>.</returns>
        public override string ToString()
        {
            return $"#{R:x2}{G:x2}{B:x2}".ToUpper();
        }
    }
}
