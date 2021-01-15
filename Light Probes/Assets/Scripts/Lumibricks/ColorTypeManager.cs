using UnityEngine;

class ColorTypeManager
{
    public enum ColorSpaceType
    {
        RGB,
        Chrominance,
        Luminance
    }

    public abstract class ColorType
    {
        public abstract Color convertColor(Color rgbColor);
    }

    public class RGBType : ColorType
    {
        public override Color convertColor(Color rgbColor) {
            return rgbColor;
        }
    }
    public class YCoCgType : ColorType
    {
        public override Color convertColor(Color rgbColor) {
            return new Color(
             rgbColor.r * 0.25f + rgbColor.g * 0.5f + rgbColor.b * 0.25f,
             rgbColor.r * 0.5f - rgbColor.b * 0.5f,
            -rgbColor.r * 0.25f + rgbColor.g * 0.5f - rgbColor.b * 0.25f);
        }
    }
}
