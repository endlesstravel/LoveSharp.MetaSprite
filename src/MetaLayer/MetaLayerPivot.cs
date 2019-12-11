using System.Collections.Generic;
using System.Linq;
using Love;

namespace MetaSprite {

    public class MetaLayerPivot : MetaLayerProcessor {
        public override string actionName {
            get { return "pivot"; }
        }

        struct PivotFrame {
            public int frame;
            public Vector2 pivot;
            public override string ToString()
            {
                return $"{frame}: {pivot}";
            }
        }

        public static Vector2 CenterOfCel(Cel cel)
        {
            if (cel != null)
            {
                return new RectangleF(cel.x, cel.y, cel.width, cel.height).Center;
            }

            return Vector2.Zero;
        }

        public override void Process(ImportContext ctx, Layer layer) {
            var pivots = new List<PivotFrame>();

            var file = ctx.file;

            //var importer = AssetImporter.GetAtPath(ctx.atlasPath) as TextureImporter;
            var spriteSheet = ctx.generatedSprites;

            for (int i = 0; i < file.frames.Count; ++i) {
                Cel cel;
                file.frames[i].cels.TryGetValue(layer.index, out cel);

                if (cel != null) {
                    //Vector2 center = Vector2.Zero;
                    //int pixelCount = 0;

                    //for (int y = 0; y < cel.height; ++y)
                    //    for (int x = 0; x < cel.width; ++x) {
                    //        // tex coords relative to full texture boundaries
                    //        int texX = cel.x + x;
                    //        int texY = -(cel.y + y) + file.height - 1;

                    //        var col = cel.GetPixelRaw(x, y);
                    //        if (col.Af > 0.1f) {
                    //            center += new Vector2(texX, texY);
                    //            ++pixelCount;
                    //        }
                    //    }

                    //if (pixelCount > 0) {
                    //    center /= pixelCount;
                    //    pivots.Add(new PivotFrame { frame = i, pivot = center });
                    //}


                    pivots.Add(new PivotFrame { frame = i, pivot = CenterOfCel(cel) });
                }
            }

            if (pivots.Count == 0)
                return;

            for (int i = 0; i < spriteSheet.Count; ++i) {
                int j = 1;
                while (j < pivots.Count && pivots[j].frame <= i) ++j; // j = index after found item
            
                //Vector2 pivot = pivots[j - 1].pivot;
                //pivot -= ctx.spriteCropPositions[i];
                //pivot = FunctionBoost.Vector2_Scale(pivot, new Vector2(1.0f / spriteSheet[i].rect.Width, 1.0f / spriteSheet[i].rect.Height));

                //spriteSheet[i].spritedPivot = new Vector2(pivot.X, pivot.Y);

                Vector2 pivot = pivots[j - 1].pivot;
                //pivot -= ctx.spriteCropPositions[i];
                //pivot = FunctionBoost.Vector2_Scale(pivot, new Vector2(1.0f / file.width, 1.0f / file.height));
                //spriteSheet[i].spritedPivot = new Vector2(pivot.X, pivot.Y);

                spriteSheet[i].spritedPivot = new Vector2(pivot.X, pivot.Y);
            }

            //importer.spritesheet = spriteSheet;
            //EditorUtility.SetDirty(importer);
            //importer.SaveAndReimport();
        }
    }

}