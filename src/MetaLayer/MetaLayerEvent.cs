using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace MetaSprite
{

    public class MetaLayerEvent : MetaLayerProcessor
    {

        public override string actionName
        {
            get { return "event"; }
        }

        public override void Process(ImportContext ctx, Layer layer)
        {
            var file = ctx.file;
            var eventFlags = new List<int>(file.frames.Count);
            for (int i = 0; i < file.frames.Count; ++i)
            {
                file.frames[i].cels.TryGetValue(layer.index, out Cel cel);
                if (cel != null)
                {
                    eventFlags.Add(i);
                }
            }

            if (eventFlags.Count == 0)
                return;

            var eventName = layer.GetParamString(0);

            foreach (var freame in eventFlags)
            {
                var sprites = ctx.generatedSprites[freame];
                if (sprites.eventSet.Add(eventName) == false)
                {
                    Love.Log.Error($"duplicate event       {eventName}");
                }
            }
        }
    }

}
