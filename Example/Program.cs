using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Love;
using Love.Misc;
using MetaSprite;

namespace Example
{
    class Program : Scene
    {
        AsepriteAnimation ani;
        AsepriteAnimation aniCloned;
        List<string> tagNameList = new List<string>();
        int tagNameIndex = 0;
        public void ToNextTag()
        {
            tagNameIndex = ++tagNameIndex % tagNameList.Count;
            ani.SetTag(tagNameList[tagNameIndex], false);
            aniCloned.SetTag(tagNameList[tagNameIndex], true);
        }

        public void Reset(string path)
        {
            ani = AsepriteAnimation.New(path, null);
            aniCloned = ani.Clone();

            ani.FrameBegin += (index) =>
            {
                Console.WriteLine($"{path} - b - [{index}] / { ani.FrameCount - 1}");

                foreach (var evtName in ani.GetFrameEvent(index))
                {
                    Console.WriteLine($"event:   " + evtName);
                }
            };
            ani.FrameEnd += (index) =>
            {
                    Console.WriteLine($"{path} - e - [{index}] / { ani.FrameCount - 1}");
            };
            tagNameList.Clear();
            tagNameList.AddRange(ani.TagNameIter);
            tagNameIndex = -1;
            ToNextTag();
            Window.SetTitle(path);
        }

        public override void FileDropped(string fileFilePath)
        {
            Reset(fileFilePath);
        }

        public override void Load()
        {
            Reset("examples/spinner.aseprite");
        }

        public static void DrawCrossCircle(Vector2 pos, int r)
        {
            Graphics.Circle(DrawMode.Line, pos, r); // pivot
            Graphics.Line(pos.X - r, pos.Y, pos.X + r, pos.Y);
            Graphics.Line(pos.X, pos.Y - r, pos.X, pos.Y + r);
        }

        public static void Draw(AsepriteAnimation ani, Vector2 pos)
        {
            Graphics.SetColor(Color.White);
            ani.Draw(pos.X, pos.Y);

            Graphics.SetColor(Color.Green);
            int r = 10;
            DrawCrossCircle(pos, r);

            foreach (var kv in ani.CurrentFrameRectDict) // rect
            {
                var pp_rect = kv.Value;
                pp_rect.Location += pos;
                Graphics.Rectangle(DrawMode.Line, pp_rect);
            }

            foreach (var kv in ani.CurrentFrameTransDict) // trans
            {
                var pps = pos + kv.Value;// ani.CurrentFrameTransToPos(pos, kv.Value);
                DrawCrossCircle(pps, 5);
                Graphics.Print(kv.Key, pps.X, pps.Y);
            }
        }

        public override void Draw()
        {
            Graphics.Print(" fps: " + Love.FPSCounter.GetFPS(), 0, Graphics.GetHeight() - 20);
            if (ani != null)
            {
                var pos = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
                Draw(ani, pos);

                Draw(aniCloned, new Vector2(Graphics.GetWidth() / 4, Graphics.GetHeight() / 4));


                Graphics.SetColor(Color.White);
                int h = 0;
                float fh = Graphics.GetFont().GetHeight() + 2;
                foreach (var tname in ani.TagNameIter)
                {
                    Graphics.Print((tname == tagNameList[tagNameIndex] ? "*   " : "") + tname
                        , 0, h++ * fh);
                }
            }
        }

        public override void Update(float dt)
        {
            //dt /= 5f;
            if (ani != null)
            {
                ani?.Update(dt);
                aniCloned?.Update(dt);
                if (InputBoost.GetKeyboardDown().Length > 0 
                    && Keyboard.IsPressed(InputBoost.GetKeyboardDown()[0]))
                {
                    ToNextTag();
                }
            }
        }

        static void Main(string[] args)
        {
            Boot.Init(new BootConfig()
            {
                WindowWidth = 1000,
                WindowHeight = 800,
            });
            Boot.Run(new Program());
            Boot.Run(new BtnSubAreaTest());
        }
    }



    class AniMetaSpriteButton
    {
        readonly AsepriteAnimation ani;
        readonly RectangleF[] nineRectList;
        public AniMetaSpriteButton(string path)
        {
            ani = AsepriteAnimation.New(path, "hovered");
            ani.TryGetCurrentFrameRect("text", out var contentRect);
            var aniRect = new RectangleF(0, 0, ani.Width, ani.Height);

            if (SplitNine(aniRect, contentRect, out nineRectList) == false)
            {
                Log.Warnning("error nine button of " + path);
            }
        }

        public void SetStateTag(string str) => ani.SetTag(str);
        public void Update(float dt) => ani.Update(dt);
        public string CurrentName => ani.TagName;

        /// <summary>
        /// split rect by other center rect<para></para>
        /// [0] [1] [2]<para></para>
        /// [3] [4] [5]<para></para>
        /// [6] [7] [8]<para></para>
        /// </summary>
        public static bool SplitNine(RectangleF rect, RectangleF centerRect, out RectangleF[] result)
        {
            var offsetedCenterRect = centerRect.DefLocation(rect.Location + centerRect.Location);


            if (!rect.Contains(offsetedCenterRect.Location) || !rect.Contains(new Vector2(offsetedCenterRect.Right, offsetedCenterRect.Bottom)))
            {
                result = new RectangleF[9];
                return false;
            }


            float left_x = rect.Left, middle_x = offsetedCenterRect.Left, right_x = offsetedCenterRect.Right;
            float top_y = rect.Top, middle_y = offsetedCenterRect.Top, bottom_y = offsetedCenterRect.Bottom;
            float left_w = centerRect.Left, middle_w = offsetedCenterRect.Width, right_w = rect.Right - offsetedCenterRect.Right;
            float top_h = centerRect.Top, middle_h = offsetedCenterRect.Height, bottom_h = rect.Bottom - offsetedCenterRect.Bottom;

            result = new RectangleF[]
            {
                new RectangleF(left_x, top_y, left_w, top_h), // left - top
                new RectangleF(middle_x, top_y, middle_w, top_h), // middle - top
                new RectangleF(right_x, top_y, right_w, top_h), // right - top
                
                new RectangleF(left_x, middle_y, left_w, middle_h), // left - middle
                new RectangleF(middle_x, middle_y, middle_w, middle_h), // middle - middle
                new RectangleF(right_x, middle_y, right_w, middle_h), // right - middle
                
                new RectangleF(left_x, bottom_y, left_w, bottom_h), // left - bottom
                new RectangleF(middle_x, bottom_y, middle_w, bottom_h), // middle - bottom
                new RectangleF(right_x, bottom_y, right_w, bottom_h), // right - bottom
            };
            return true;
        }

        /// <summary>
        /// Vertical side repeat
        /// </summary>
        public bool VerticalSideRepeatMode = false;

        /// <summary>
        /// Horizontal side repeat
        /// </summary>
        public bool HorizontalSideRepeatMode = false;

        /// <summary>
        /// center use repeat
        /// </summary>
        public bool CenterRepeatMode = false;


        Shader reaptedShader = Graphics.NewShader(@"
   uniform vec4 texture_rect;
   uniform vec2 repeat_num;
   vec4 effect( vec4 color, Image tex, vec2 texture_coords, vec2 screen_coords )
    {
        float rw = texture_rect.z;
        float rh = texture_rect.w;
        float sub_w = texture_coords.x - texture_rect.x;
        float sub_h = texture_coords.y - texture_rect.y;
        vec4 texcolor = Texel(tex, vec2(
            texture_rect.x + mod(sub_w * repeat_num.x, rw), 
            texture_rect.y + mod(sub_h * repeat_num.y, rh)
        ));
        //texcolor = Texel(tex, texture_coords);
        return texcolor * color;
    }
");
        public void Draw(RectangleF drawRect)
        {
            void drawScaled(Quad quad, Image img, Vector2 pos, Vector2 pos_offset, Vector2 offset, Vector2 scale)
            {
                Graphics.Draw(quad, img,
                    pos.X + pos_offset.X * scale.X,
                    pos.Y + pos_offset.Y * scale.Y,
                    0, scale.X, scale.Y, offset.X, offset.Y);
            }
            void drawUnscaled(Quad quad, Image img, Vector2 pos, Vector2 pos_offset, Vector2 offset)
            {
                Graphics.Draw(quad, img, pos.X + pos_offset.X, pos.Y + pos_offset.Y, 0, 1, 1, offset.X, offset.Y);
            }
            Draw(drawRect, drawScaled, drawUnscaled);
        }


        public void Draw(RectangleF drawRect,
            Action<Quad, Image, Vector2, Vector2, Vector2, Vector2> sacledDraw,
            Action<Quad, Image, Vector2, Vector2, Vector2> unsacledDraw
            )
        {
            if (SplitNine(drawRect, new RectangleF(nineRectList[4].Location,
                 new SizeF(drawRect.Width - nineRectList[6].Width - nineRectList[8].Width,
                 drawRect.Height - nineRectList[2].Height - nineRectList[8].Height)), out var drawedNineRectList) == false)
            {
                Log.Warnning("too small draw area " + this);
                return;
            }

            void DrawSubArea(int regIndex, bool isScaled, bool repeated)
            {
                if (isScaled)
                {
                    if (repeated)
                    {
                        Shader oldShader = Graphics.GetShader();
                        Graphics.SetShader(reaptedShader);

                        var reg = nineRectList[regIndex];
                        var draw_rect = drawedNineRectList[regIndex];

                        ani.DrawSubRegion((quad, img, pos_offset, offset) =>
                        {
                            var scaleX = draw_rect.Width / reg.Width;
                            var scaleY = draw_rect.Height / reg.Height;
                            var vvp = quad.GetViewport();

                            reaptedShader.SendVector4("texture_rect", new Vector4(
                                vvp.X / img.GetWidth(), vvp.Y / img.GetHeight(),
                                (vvp.Width) / img.GetWidth(),
                                (vvp.Height) / img.GetHeight()));
                            reaptedShader.SendVector2("repeat_num", new Vector2(scaleX, scaleY));
                            sacledDraw?.Invoke(quad, img, draw_rect.Location, pos_offset, offset, new Vector2(scaleX, scaleY));

                            //Graphics.Draw(quad, img,
                            //    draw_rect.Location.X + pos_offset.X * scaleX,
                            //    draw_rect.Location.Y + pos_offset.Y * scaleY,
                            //    0, scaleX, scaleY, offset.X, offset.Y);

                        }, reg);
                        Graphics.SetShader(oldShader);
                    }
                    else
                    {
                        var reg = nineRectList[regIndex];
                        var draw_rect = drawedNineRectList[regIndex];
                        ani.DrawSubRegion((quad, img, pos_offset, offset) =>
                        {
                            var scaleX = draw_rect.Width / reg.Width;
                            var scaleY = draw_rect.Height / reg.Height;
                            sacledDraw?.Invoke(quad, img, draw_rect.Location, pos_offset, offset, new Vector2(scaleX, scaleY));
                        }, reg);
                    }
                }
                else
                {
                    var reg = nineRectList[regIndex];
                    var draw_rect = drawedNineRectList[regIndex];
                    ani.DrawSubRegion((quad, img, pos_offset, offset) =>
                    {
                        unsacledDraw?.Invoke(quad, img, draw_rect.Location, pos_offset, offset);
                        //Graphics.Draw(quad, img, pos.X + pos_offset.X, pos.Y + pos_offset.Y, 0, 1, 1, offset.X, offset.Y);
                    }, reg);
                }
            }


            // draw no changed l-t/r-t/l-b/rb
            foreach (var regIndex in new int[] { 0, 2, 6, 8 })
            {
                DrawSubArea(regIndex, false, false);
            }

            // draw scaled pic vertical
            foreach (var regIndex in new int[] { 1, 7 })
            {
                DrawSubArea(regIndex, true, VerticalSideRepeatMode);
            }
            // draw scaled pic horizontal
            foreach (var regIndex in new int[] { 3, 5 })
            {
                DrawSubArea(regIndex, true, HorizontalSideRepeatMode);
            }
            // draw center
            foreach (var regIndex in new int[] { 4 })
            {
                DrawSubArea(regIndex, true, CenterRepeatMode);
            }
        }

    }

    public class BtnSubAreaTest : Scene
    {
        readonly AniMetaSpriteButton ani = new AniMetaSpriteButton("examples/ui_menu_option.aseprite");

        public override void Update(float dt)
        {
            base.Update(dt);
            Window.SetTitle(ani.CurrentName);
            if (Keyboard.IsPressed(KeyConstant.Number1))
            {
                ani.SetStateTag("normal");
            }
            if (Keyboard.IsPressed(KeyConstant.Number2))
            {
                ani.SetStateTag("hovered");
            }
            if (Keyboard.IsPressed(KeyConstant.Number3))
            {
                ani.SetStateTag("selected");
            }
            ani.Update(dt);
        }

        public override void Draw()
        {

            Graphics.Clear(Color.IndianRed);
            float scaleF = 4;
            var drawRect = new RectangleF(20, 20, 140, 70);

            Graphics.Push();
            Graphics.Scale(scaleF);
            Graphics.SetColor(Color.Green);
            Graphics.SetColor(Color.White);
            ani.CenterRepeatMode = true;
            ani.VerticalSideRepeatMode = true;
            ani.HorizontalSideRepeatMode = true;
            ani.Draw(drawRect);
            Graphics.SetLineWidth(1 / scaleF);
            Graphics.Rectangle(DrawMode.Line, drawRect);
            Graphics.SetLineWidth(1);

            Graphics.Pop();
        }
    }
}
