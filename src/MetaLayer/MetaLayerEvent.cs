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
            var eventFlags = new List<int>();
            var file = ctx.file;
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

            var name = layer.GetParamString(0);

            var evtlist = ctx.eventInfoList;
            if (evtlist.ContainsKey(name))
            {
                Love.Log.Error($"duplicate event       {name}");
            }
            else
            {
                evtlist[name] = eventFlags;
            }
        }
    }

}
