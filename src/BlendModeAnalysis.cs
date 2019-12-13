// adapt from https://github.com/Seanba/Aseprite2Unity
using System;
using color_t = System.UInt32;
using uint8_t = System.Byte;
using uint16_t = System.UInt16;
using uint32_t = System.UInt32;
using dc = MetaSprite.BlendeModeAnalysis.DocColor;
using pc = MetaSprite.BlendeModeAnalysis.PixmanCombine;
using Love;

namespace MetaSprite
{
    public static class BlendeModeAnalysis
    {
        public static color_t ConvertTo(Color c)
        {
            return dc.rgba(c.r, c.g, c.b, c.a);
        }

        public static Color ConvertTo(color_t c)
        {
            return new Color(dc.rgba_getr(c), dc.rgba_getg(c), dc.rgba_getb(c), dc.rgba_geta(c));
        }

        public delegate color_t BlendModeDelegate(color_t backdrop, color_t src, int opacity);
        public static BlendModeDelegate GetBlendFunc(BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Normal:
                    return rgba_blender_normal;

                case BlendMode.Darken:
                    return rgba_blender_darken;

                case BlendMode.Multiply:
                    return rgba_blender_multiply;

                case BlendMode.ColorBurn:
                    return rgba_blender_color_burn;

                case BlendMode.Lighten:
                    return rgba_blender_lighten;

                case BlendMode.Screen:
                    return rgba_blender_screen;

                case BlendMode.ColorDodge:
                    return rgba_blender_color_dodge;

                case BlendMode.Addition:
                    return rgba_blender_addition;

                case BlendMode.Overlay:
                    return rgba_blender_overlay;

                case BlendMode.SoftLight:
                    return rgba_blender_soft_light;

                case BlendMode.HardLight:
                    return rgba_blender_hard_light;

                case BlendMode.Difference:
                    return rgba_blender_difference;

                case BlendMode.Exclusion:
                    return rgba_blender_exclusion;

                case BlendMode.Subtract:
                    return rgba_blender_subtract;

                case BlendMode.Divide:
                    return rgba_blender_divide;

                case BlendMode.Hue:
                    return rgba_blender_hsl_hue;

                case BlendMode.Saturation:
                    return rgba_blender_hsl_saturation;

                case BlendMode.Color:
                    return rgba_blender_hsl_color;

                case BlendMode.Luminosity:
                    return rgba_blender_hsl_luminosity;

                default:
                    Log.Warnning($"Unsupported blend mode: {blendMode}");
                    return rgba_blender_normal;
            }
        }

        public static uint8_t blend_multiply(uint8_t b, uint8_t s) => pc.MUL_UN8(b, s);
        public static uint8_t blend_screen(uint8_t b, uint8_t s) => (uint8_t)(b + s - pc.MUL_UN8(b, s));
        public static uint8_t blend_overlay(uint8_t b, uint8_t s) => blend_hard_light(s, b);
        public static uint8_t blend_darken(uint8_t b, uint8_t s) => Math.Min(b, s);
        public static uint8_t blend_lighten(uint8_t b, uint8_t s) => Math.Max(b, s);

        public static uint8_t blend_hard_light(uint8_t b, uint8_t s)
        {
            return s < 128 ? blend_multiply(b, (uint8_t)(s << 1)) : blend_screen(b, (uint8_t)((s << 1) - 255));
        }

        public static uint8_t blend_difference(uint8_t b, uint8_t s) => (uint8_t)Math.Abs(b - s);

        public static uint8_t blend_exclusion(uint8_t b, uint8_t s)
        {
            int t = pc.MUL_UN8(b, s);
            return (uint8_t)(b + s - 2 * t);
        }

        public static uint8_t blend_divide(uint8_t b, uint8_t s)
        {
            if (b == 0)
                return 0;
            else if (b >= s)
                return 255;
            else
                return pc.DIV_UN8(b, s); // return b / s
        }

        public static uint8_t blend_color_dodge(uint8_t b, uint8_t s)
        {
            if (b == 0)
                return 0;

            s = (uint8_t)(255 - s);
            if (b >= s)
                return 255;
            else
                return pc.DIV_UN8(b, s); // return b / (1-s)
        }

        public static uint8_t blend_color_burn(uint32_t b, uint32_t s)
        {
            if (b == 255)
                return 255;

            b = (255 - b);
            if (b >= s)
                return 0;
            else
                return (uint8_t)(255 - pc.DIV_UN8((uint8_t)b, (uint8_t)s)); // return 1 - ((1-b)/s)
        }

        public static uint8_t blend_soft_light(uint32_t _b, uint32_t _s)
        {
            double b = _b / 255.0;
            double s = _s / 255.0;
            double r, d;

            if (b <= 0.25)
                d = ((16 * b - 12) * b + 4) * b;
            else
                d = Math.Sqrt(b);

            if (s <= 0.5)
                r = b - (1.0 - 2.0 * s) * b * (1.0 - b);
            else
                r = b + (2.0 * s - 1.0) * (d - b);

            return (uint8_t)(r * 255 + 0.5);
        }

        // RGB blenders

        public static color_t rgba_blender_normal(color_t backdrop, color_t src, int opacity)
        {
            if ((backdrop & dc.rgba_a_mask) == 0)
            {
                uint32_t a = dc.rgba_geta(src);
                a = pc.MUL_UN8((uint8_t)a, (uint8_t)opacity);
                a <<= (int)dc.rgba_a_shift;
                return (src & dc.rgba_rgb_mask) | a;
            }
            else if ((src & dc.rgba_a_mask) == 0)
            {
                return backdrop;
            }

            int Br = dc.rgba_getr(backdrop);
            int Bg = dc.rgba_getg(backdrop);
            int Bb = dc.rgba_getb(backdrop);
            int Ba = dc.rgba_geta(backdrop);

            int Sr = dc.rgba_getr(src);
            int Sg = dc.rgba_getg(src);
            int Sb = dc.rgba_getb(src);
            int Sa = dc.rgba_geta(src);
            Sa = pc.MUL_UN8((byte)Sa, (byte)opacity);

            // Ra = Sa + Ba*(1-Sa)
            //    = Sa + Ba - Ba*Sa
            int Ra = Sa + Ba - pc.MUL_UN8((byte)Ba, (byte)Sa);

            // Ra = Sa + Ba*(1-Sa)
            // Ba = (Ra-Sa) / (1-Sa)
            // Rc = (Sc*Sa + Bc*Ba*(1-Sa)) / Ra                Replacing Ba with (Ra-Sa) / (1-Sa)...
            //    = (Sc*Sa + Bc*(Ra-Sa)/(1-Sa)*(1-Sa)) / Ra
            //    = (Sc*Sa + Bc*(Ra-Sa)) / Ra
            //    = Sc*Sa/Ra + Bc*Ra/Ra - Bc*Sa/Ra
            //    = Sc*Sa/Ra + Bc - Bc*Sa/Ra
            //    = Bc + (Sc-Bc)*Sa/Ra
            int Rr = Br + (Sr - Br) * Sa / Ra;
            int Rg = Bg + (Sg - Bg) * Sa / Ra;
            int Rb = Bb + (Sb - Bb) * Sa / Ra;

            return dc.rgba((uint32_t)Rr, (uint32_t)Rg, (uint32_t)Rb, (uint32_t)Ra);
        }

        public static color_t rgba_blender_multiply(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_multiply(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_multiply(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_multiply(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_screen(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_screen(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_screen(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_screen(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_overlay(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_overlay(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_overlay(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_overlay(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_darken(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_darken(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_darken(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_darken(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_lighten(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_lighten(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_lighten(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_lighten(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_color_dodge(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_color_dodge(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_color_dodge(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_color_dodge(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_color_burn(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_color_burn(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_color_burn(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_color_burn(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_hard_light(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_hard_light(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_hard_light(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_hard_light(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_soft_light(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_soft_light(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_soft_light(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_soft_light(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_difference(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_difference(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_difference(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_difference(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_exclusion(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_exclusion(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_exclusion(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_exclusion(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        // HSV blenders

        private static double lum(double r, double g, double b)
        {
            return (0.3 * r) + (0.59 * g) + (0.11 * b);
        }

        private static double sat(double r, double g, double b)
        {
            return Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b));
        }

        private static void clip_color(ref double r, ref double g, ref double b)
        {
            double l = lum(r, g, b);
            double n = Math.Min(r, Math.Min(g, b));
            double x = Math.Max(r, Math.Max(g, b));

            if (n < 0)
            {
                r = l + (((r - l) * l) / (l - n));
                g = l + (((g - l) * l) / (l - n));
                b = l + (((b - l) * l) / (l - n));
            }

            if (x > 1)
            {
                r = l + (((r - l) * (1 - l)) / (x - l));
                g = l + (((g - l) * (1 - l)) / (x - l));
                b = l + (((b - l) * (1 - l)) / (x - l));
            }
        }

        private static void set_lum(ref double r, ref double g, ref double b, double l)
        {
            double d = l - lum(r, g, b);
            r = r + d;
            g = g + d;
            b = b + d;
            clip_color(ref r, ref g, ref b);
        }

        // This stuff is such a dirty hack for the set_sat function
        private class DoubleRef
        {
            public double Value { get; set; }
        }

        private static DoubleRef REFMIN(DoubleRef x, DoubleRef y)
        {
            return x.Value < y.Value ? x : y;
        }

        private static DoubleRef REFMAX(DoubleRef x, DoubleRef y)
        {
            return x.Value > y.Value ? x : y;
        }

        private static DoubleRef REFMID(DoubleRef x, DoubleRef y, DoubleRef z)
        {
            return REFMAX(x, REFMIN(y, z));
        }

        private static void set_sat(ref double _r, ref double _g, ref double _b, double s)
        {
            DoubleRef r = new DoubleRef { Value = _r };
            DoubleRef g = new DoubleRef { Value = _g };
            DoubleRef b = new DoubleRef { Value = _b };

            DoubleRef min = REFMIN(r, REFMIN(g, b));
            DoubleRef mid = REFMID(r, g, b);
            DoubleRef max = REFMAX(r, REFMAX(g, b));

            if (max.Value > min.Value)
            {
                mid.Value = ((mid.Value - min.Value) * s) / (max.Value - min.Value);
                max.Value = s;
            }
            else
            {
                mid.Value = 0;
                max.Value = 0;
            }

            min.Value = 0;

            _r = r.Value;
            _g = g.Value;
            _b = b.Value;
        }

        public static color_t rgba_blender_hsl_hue(color_t backdrop, color_t src, int opacity)
        {
            double r = dc.rgba_getr(backdrop) / 255.0;
            double g = dc.rgba_getg(backdrop) / 255.0;
            double b = dc.rgba_getb(backdrop) / 255.0;
            double s = sat(r, g, b);
            double l = lum(r, g, b);

            r = dc.rgba_getr(src) / 255.0;
            g = dc.rgba_getg(src) / 255.0;
            b = dc.rgba_getb(src) / 255.0;

            set_sat(ref r, ref g, ref b, s);
            set_lum(ref r, ref g, ref b, l);

            src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_hsl_saturation(color_t backdrop, color_t src, int opacity)
        {
            double r = dc.rgba_getr(src) / 255.0;
            double g = dc.rgba_getg(src) / 255.0;
            double b = dc.rgba_getb(src) / 255.0;
            double s = sat(r, g, b);

            r = dc.rgba_getr(backdrop) / 255.0;
            g = dc.rgba_getg(backdrop) / 255.0;
            b = dc.rgba_getb(backdrop) / 255.0;
            double l = lum(r, g, b);

            set_sat(ref r, ref g, ref b, s);
            set_lum(ref r, ref g, ref b, l);

            src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_hsl_color(color_t backdrop, color_t src, int opacity)
        {
            double r = dc.rgba_getr(backdrop) / 255.0;
            double g = dc.rgba_getg(backdrop) / 255.0;
            double b = dc.rgba_getb(backdrop) / 255.0;
            double l = lum(r, g, b);

            r = dc.rgba_getr(src) / 255.0;
            g = dc.rgba_getg(src) / 255.0;
            b = dc.rgba_getb(src) / 255.0;

            set_lum(ref r, ref g, ref b, l);

            src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_hsl_luminosity(color_t backdrop, color_t src, int opacity)
        {
            double r = dc.rgba_getr(src) / 255.0;
            double g = dc.rgba_getg(src) / 255.0;
            double b = dc.rgba_getb(src) / 255.0;
            double l = lum(r, g, b);

            r = dc.rgba_getr(backdrop) / 255.0;
            g = dc.rgba_getg(backdrop) / 255.0;
            b = dc.rgba_getb(backdrop) / 255.0;

            set_lum(ref r, ref g, ref b, l);

            src = dc.rgba((uint32_t)(255.0 * r), (uint32_t)(255.0 * g), (uint32_t)(255.0 * b), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_addition(color_t backdrop, color_t src, int opacity)
        {
            int r = dc.rgba_getr(backdrop) + dc.rgba_getr(src);
            int g = dc.rgba_getg(backdrop) + dc.rgba_getg(src);
            int b = dc.rgba_getb(backdrop) + dc.rgba_getb(src);
            src = dc.rgba((uint8_t)Math.Min(r, 255), (uint8_t)Math.Min(g, 255), (uint8_t)Math.Min(b, 255), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_subtract(color_t backdrop, color_t src, int opacity)
        {
            int r = dc.rgba_getr(backdrop) - dc.rgba_getr(src);
            int g = dc.rgba_getg(backdrop) - dc.rgba_getg(src);
            int b = dc.rgba_getb(backdrop) - dc.rgba_getb(src);
            src = dc.rgba((uint8_t)Math.Max(r, 0), (uint8_t)Math.Max(g, 0), (uint8_t)Math.Max(b, 0), 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static color_t rgba_blender_divide(color_t backdrop, color_t src, int opacity)
        {
            uint8_t r = blend_divide(dc.rgba_getr(backdrop), dc.rgba_getr(src));
            uint8_t g = blend_divide(dc.rgba_getg(backdrop), dc.rgba_getg(src));
            uint8_t b = blend_divide(dc.rgba_getb(backdrop), dc.rgba_getb(src));
            src = dc.rgba(r, g, b, 0) | (src & dc.rgba_a_mask);
            return rgba_blender_normal(backdrop, src, opacity);
        }

        public static class DocColor
        {
            public const uint32_t rgba_r_shift = 0;
            public const uint32_t rgba_g_shift = 8;
            public const uint32_t rgba_b_shift = 16;
            public const uint32_t rgba_a_shift = 24;

            public const uint32_t rgba_r_mask = 0x000000ff;
            public const uint32_t rgba_g_mask = 0x0000ff00;
            public const uint32_t rgba_b_mask = 0x00ff0000;
            public const uint32_t rgba_rgb_mask = 0x00ffffff;
            public const uint32_t rgba_a_mask = 0xff000000;

            public static uint8_t rgba_getr(uint32_t c)
            {
                return (uint8_t)((c >> (int)(rgba_r_shift)) & 0xff);
            }

            public static uint8_t rgba_getg(uint32_t c)
            {
                return (uint8_t)((c >> (int)rgba_g_shift) & 0xff);
            }

            public static uint8_t rgba_getb(uint32_t c)
            {
                return (uint8_t)((c >> (int)rgba_b_shift) & 0xff);
            }

            public static uint8_t rgba_geta(uint32_t c)
            {
                return (uint8_t)((c >> (int)rgba_a_shift) & 0xff);
            }

            public static void rgba_setr(ref uint32_t c, uint8_t r)
            {
                c &= (~rgba_r_mask); // clean
                c |= ((uint)r << (int)rgba_r_shift); // set
            }

            public static void rgba_setg(ref uint32_t c, uint8_t g)
            {
                c &= (~rgba_g_mask); // clean
                c |= ((uint)g << (int)rgba_g_shift); // set
            }

            public static void rgba_setb(ref uint32_t c, uint8_t b)
            {
                c &= (~rgba_b_mask); // clean
                c |= ((uint)b << (int)rgba_b_shift); // set
            }

            public static void rgba_seta(ref uint32_t c, uint8_t a)
            {
                c &= (~rgba_a_mask); // clean
                c |= ((uint)a << (int)rgba_a_shift); // set
            }

            public static uint32_t rgba(uint32_t r, uint32_t g, uint32_t b, uint32_t a)
            {
                return ((r << (int)rgba_r_shift) |
                        (g << (int)rgba_g_shift) |
                        (b << (int)rgba_b_shift) |
                        (a << (int)rgba_a_shift));
            }

            public static int rgb_luma(int r, int g, int b)
            {
                return (r * 2126 + g * 7152 + b * 722) / 10000;
            }

            public static uint8_t rgba_luma(uint32_t c)
            {
                return (uint8_t)rgb_luma(rgba_getr(c), rgba_getg(c), rgba_getb(c));
            }

            //////////////////////////////////////////////////////////////////////
            // Grayscale

            const uint16_t graya_v_shift = 0;
            const uint16_t graya_a_shift = 8;

            const uint16_t graya_v_mask = 0x00ff;
            const uint16_t graya_a_mask = 0xff00;

            public static uint8_t graya_getv(uint16_t c)
            {
                return (uint8_t)((c >> graya_v_shift) & 0xff);
            }

            public static uint8_t graya_geta(uint16_t c)
            {
                return (uint8_t)((c >> graya_a_shift) & 0xff);
            }

            public static uint16_t graya(uint8_t v, uint8_t a)
            {
                return (uint16_t)((v << graya_v_shift) | (a << graya_a_shift));
            }

            public static uint16_t gray(uint8_t v)
            {
                return graya(v, 255);
            }
        }

        public static class PixmanCombine
        {
            public const uint COMPONENT_SIZE = 8;
            public const byte MASK = 0xff;
            public const byte ONE_HALF = 0x80;

            public const byte A_SHIFT = 8 * 3;
            public const byte R_SHIFT = 8 * 2;
            public const byte G_SHIFT = 8;
            public const uint A_MASK = 0xff000000;
            public const uint R_MASK = 0xff0000;
            public const uint G_MASK = 0xff00;

            public const uint RB_MASK = 0xff00ff;
            public const uint AG_MASK = 0xff00ff00;
            public const uint RB_ONE_HALF = 0x800080;
            public const uint RB_MASK_PLUS_ONE = 0x10000100;

            static uint ALPHA_8(uint x) => ((x) >> A_SHIFT);
            static uint RED_8(uint x) => (((x) >> R_SHIFT) & MASK);
            static uint GREEN_8(uint x) => (((x) >> G_SHIFT) & MASK);
            static uint BLUE_8(uint x) => ((x) & MASK);

            // Helper "macros"
            public static byte MUL_UN8(uint8_t a, uint8_t b)
            {
                int t = a * b + ONE_HALF;
                return (uint8_t)(((t >> G_SHIFT) + (t)) >> G_SHIFT);
            }

            public static uint8_t DIV_UN8(uint8_t a, uint8_t b)
            {
                return (uint8_t)(((uint16_t)(a) * MASK + ((b) / 2)) / (b));
            }
        }


    }
}