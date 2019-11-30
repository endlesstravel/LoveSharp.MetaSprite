using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Love;
using MetaSprite;

namespace Example
{
    class Program : Scene
    {
        SpriteAnimation ani = ASEImporter.Import("examples/sound.aseprite", "Bounce");

        public override void Load()
        {
            base.Load();
        }

        public override void Draw()
        {
            ani.Draw(0, 0);
        }

        public override void Update(float dt)
        {
            ani.Update(dt);
        }

        static void Main(string[] args)
        {
            Boot.Init();
            Boot.Run(new Program());
        }
    }
}
