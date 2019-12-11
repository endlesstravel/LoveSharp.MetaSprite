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
        SpriteAnimation ani;
        SpriteAnimation aniCloned;
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
            ani = ASEImporter.Import(path, null);
            aniCloned = ani.Clone();

            ani.FrameBegin += (name, index) =>
            {
                if (index == ani.FrameCount - 1)
                {
                    ToNextTag();
                    Console.WriteLine($"{path} - [{name}]     end");
                }
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

        public static void Draw(SpriteAnimation ani, Vector2 pos)
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


    public class BtnSubAreaTest : Scene
    {
        readonly SpriteAnimation ani = SpriteAnimation.New("examples/ui_menu_option.aseprite", "selected");
        //readonly SpriteAnimation ani = SpriteAnimation.New("examples/ui_menu_option.aseprite", "normal");

        SpriteAnimationSubarea[] qlist;
        public override void Load()
        {
            base.Load();
            ani.TryGetCurrentFrameRect("text", out var contentRect);
            var aniRect = new RectangleF(0, 0, ani.Width, ani.Height);

            float left_x = 0, middle_x = contentRect.Left, right_x = contentRect.Right;
            float top_y = 0, middle_y = contentRect.Top, bottom_y = contentRect.Bottom;
            float left_w = contentRect.Left, middle_w = contentRect.Width, right_w = aniRect.Right - contentRect.Right;
            float top_h = contentRect.Top, middle_h = contentRect.Height, bottom_h = aniRect.Bottom - contentRect.Bottom;

            var rectList = new RectangleF[]
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
            qlist = rectList.Select(item => ani.GenSubRegionQuad(item)).ToArray();
        }

        public override void Draw()
        {
            Graphics.Clear(Color.IndianRed);
            var drawRect = new RectangleF(100, 100, 200, 200);

            Graphics.Push();
            Graphics.Scale(8);
            Graphics.SetColor(Color.Green);
            Graphics.SetLineWidth(1/8f);
            Graphics.SetColor(Color.White);

            //ani.Draw(0, 0);
            ani.Draw((quad, img, offset)=> {
                Graphics.Draw(quad, img, 0, 0, 0, 1, 1, offset.X, offset.Y);
            });

            foreach (var reg in qlist.Skip(0).Take(9))
            {
                //ani.DrawSubRegion(reg.Rect, reg.Rect.X, reg.Rect.Y);
                //ani.DrawSubRegion((quad, img, pos, offset) =>
                //{
                //    Graphics.Draw(quad, img, pos.X, pos.Y, 0, 1, 1, offset.X, offset.Y);
                //}, reg.Rect, reg.Rect.Location);
                Graphics.Rectangle(DrawMode.Line, reg.Rect);
            }
            Graphics.Pop();

            //Graphics.SetLineWidth(1);
            //foreach (var reg in rectList)
            //{
            //    Graphics.Rectangle(DrawMode.Line, new RectangleF(reg.Location * 8, new SizeF(reg.Width * 8, reg.Height * 8)));
            //}

        }
    }
}
