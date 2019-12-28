Inspired by
https://github.com/WeAthFoLD/MetaSprite
https://github.com/Seanba/Aseprite2Unity

MetaSprite is an library that lets you import Aseprite's .ase file into Unity, Love2DCS.


![](img/show-case-01.gif)
![](img/show-case-02.gif)

support meta:
* `pivot` : `@pivot()` location the pivot of the sprite
* `rect` : `@rect("rect_name")` generate rectangle region on each frame
* `trans` : `@trans("weapon pos")` generate  position on each frame
* `event` : `@event("event name")` generate event on each frame

use it by

```C#
using MetaSprite;
    class Program : Scene
    {
        AsepriteAnimation ani = AsepriteAnimation.New("example.ase", "idle");
        public override void Load()
        {
            ani.FrameBegin += (index) =>
            {
                Console.WriteLine($"{path} - b - [{index}] / { ani.FrameCount - 1}");

                foreach (var evtName in ani.GetFrameEvent(index))
                {
                    Console.WriteLine($"event:   " + evtName);
                }
            };
        }
        public override void Update(float dt)
        {
            // ani.SetTag("attack");
            // ani.SetTag("attack", true); // use reverse mode
            // ani.TagName // get tag name
            // ani......... // more power
            ani.Update(dt);
        }
        public override void Draw()
        {
            ani.Draw(pos.X, pos.Y);
        }
    }
```