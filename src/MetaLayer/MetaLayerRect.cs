using System.Collections.Generic;
using System.Linq;
using Love;

namespace MetaSprite {

    public class MetaLayerRect : MetaLayerProcessor {
        public override string actionName {
            get { return "rect"; }
        }

        struct RectFrame {
            public int frame;
            public RectangleF rect;
            public string name;

            public override string ToString()
            {
                return $"{frame}: {name}: {rect}";
            }
        }

        public static RectangleF RectOfCel(Cel cel)
        {
            if (cel != null)
            {
                return new RectangleF(cel.x, cel.y, cel.width, cel.height);
            }

            return RectangleF.Empty;
        }

        public override void Process(ImportContext ctx, Layer layer) {
            var pivots = new List<RectFrame>();

            var file = ctx.file;

            //var importer = AssetImporter.GetAtPath(ctx.atlasPath) as TextureImporter;
            var spriteSheet = ctx.generatedSprites;

            for (int i = 0; i < file.frames.Count; ++i) {
                Cel cel;
                file.frames[i].cels.TryGetValue(layer.index, out cel);

                if (cel != null) {
                    
                    pivots.Add(new RectFrame { frame = i, name = layer.GetParamString(0), rect = RectOfCel(cel) });
                }
            }

            if (pivots.Count == 0)
                return;

            for (int i = 0; i < spriteSheet.Count; ++i) {
                int j = 1;
                while (j < pivots.Count && pivots[j].frame <= i) ++j; // j = index after found item
            
                var data = pivots[j - 1];
                spriteSheet[i].rectDict[data.name] = data.rect;
            }
        }
    }

}