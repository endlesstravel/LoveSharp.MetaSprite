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
        List<string> tagNameList = new List<string>();
        int tagNameIndex = 0;
        public void ToNextTag()
        {
            tagNameIndex = ++tagNameIndex % tagNameList.Count;
            ani.SetTag(tagNameList[tagNameIndex], true);
        }

        public void Reset(string path)
        {
            ani = ASEImporter.Import(path, null);
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

        public override void Draw()
        {
            Graphics.Print(" fps: " + Love.FPSCounter.GetFPS(), 0, Graphics.GetHeight() - 20);
            if (ani != null)
            {
                var pos = new Vector2(Graphics.GetWidth() / 2, Graphics.GetHeight() / 2);
                Graphics.SetColor(Color.White);
                ani.Draw(pos.X, pos.Y);

                Graphics.SetColor(Color.Green);
                int r = 10;
                DrawCrossCircle(pos, r);

                foreach (var kv in ani.CurrentFrameRectDict) // rect
                {
                    Graphics.Rectangle(DrawMode.Line, ani.CurrentFrameRectToPos(pos, kv.Value));
                }

                foreach (var kv in ani.CurrentFrameTransDict) // trans
                {
                    var pps = ani.CurrentFrameTransToPos(pos, kv.Value);
                    DrawCrossCircle(pps, 5);
                    Graphics.Print(kv.Key, pps.X, pps.Y);
                }

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
                if (InputBoost.GetKeyboardDown().Length > 0 
                    && Keyboard.IsPressed(InputBoost.GetKeyboardDown()[0]))
                {
                    ToNextTag();
                }
            }
        }

        static void Main(string[] args)
        {
            Boot.Init();
            Boot.Run(new Program());
        }
    }
}
