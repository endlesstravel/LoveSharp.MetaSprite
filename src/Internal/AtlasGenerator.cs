using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Love;

namespace MetaSprite.Internal {

    public static class AtlasGenerator {

        struct PackData {
            public int width,  height;
        }

        struct PackPos {
            public int x, y;
        }

        class PackResult {
            public int imageSize;
            public List<PackPos> positions;
        }

        public static List<Sprite> GenerateAtlas(ImportContext ctx, List<Layer> layers, bool densePacked = true, int border = 1) {
            var file = ctx.file;

            var images = file.frames    
                .Select(frame => {
                    var cels = frame.cels.Values.OrderBy(it => it.layerIndex).ToList();
                    var image = new FrameImage(file.width, file.height);

                    foreach (var cel in cels) {
                        var layer = file.FindLayer(cel.layerIndex);
                        if (!layers.Contains(layer)) continue;

                        for (int cy = 0; cy < cel.height; ++cy) {
                            for (int cx = 0; cx < cel.width; ++cx) {
                                var c = cel.GetPixelRaw(cx, cy);
                                if (c.Af != 0f) {
                                    var x = cx + cel.x;
                                    var y = cy + cel.y;
                                    if (0 <= x && x < file.width &&
                                        0 <= y && y < file.height) { // Aseprite allows some pixels out of bounds to be kept, ignore them
                                        var lastColor = image.GetPixel(x, y);
                                        // blending
                                        //var color = FunctionBoost.Color_Lerp(lastColor, c, c.Af);
                                        //color.Af = lastColor.Af + c.Af * (1 - lastColor.Af);
                                        //color.Rf /= color.Af;
                                        //color.Gf /= color.Af;
                                        //color.Bf /= color.Af;
                                        var color = BlendeModeAnalysis.ConvertTo(BlendeModeAnalysis.GetBlendFunc(layer.blendMode)(
                                            BlendeModeAnalysis.ConvertTo(lastColor),
                                            BlendeModeAnalysis.ConvertTo(c),
                                            c.a));

                                        image.SetPixel(x, y, color);

                                        // expand image area
                                        image.minx = Mathf.Min(image.minx, x);
                                        image.miny = Mathf.Min(image.miny, y);

                                        image.maxx = Mathf.Max(image.maxx, x);
                                        image.maxy = Mathf.Max(image.maxy, y);
                                    }
                                }
                            }
                        }
                    }

                    if (image.minx == int.MaxValue) {
                        image.minx = image.maxx = image.miny = image.maxy = 0;
                    }

                    if (!densePacked) { // override image border for sparsely packed atlas
                        image.minx = image.miny = 0;
                        image.maxx = file.width - 1;
                        image.maxy = file.height - 1;
                    }

                    return image;
                })
                .ToList();

            var packList = images.Select(image => new PackData { width = image.finalWidth, height = image.finalHeight }).ToList();
            var packResult = PackAtlas(packList, border);

            if (packResult.imageSize > 2048) {
                Log.Warnning("Generate atlas size is larger than 2048 !");
            }

            var textureData = new Color[packResult.imageSize * packResult.imageSize];
            // build image
            for (int i = 0; i < images.Count; ++i)
            {
                var pos = packResult.positions[i];
                var image = images[i];
                for (int y = image.miny; y <= image.maxy; ++y)
                {
                    for (int x = image.minx; x <= image.maxx; ++x)
                    {
                        //int texX = (x - image.minx) + pos.x;
                        //int texY = -(y - image.miny) + pos.y + image.finalHeight - 1;
                        //textureData[texX + texY * packResult.imageSize] = image.GetPixel(x, y);
                        int texX = (x - image.minx) + pos.x;
                        int texY = (y - image.miny) + pos.y;
                        textureData[texX + texY * packResult.imageSize] = image.GetPixel(x, y);
                    }
                }
            }
            var texture = Image.NewImageData(packResult.imageSize, packResult.imageSize);
            texture.SetPixels(textureData);
            var textureImage = Graphics.NewImage(texture);
            textureImage.SetFilter(FilterMode.Nearest, FilterMode.Nearest);
            // build image end

            Vector2 oldPivotNorm = Vector2.Zero;

            var metaList = new List<Sprite>(images.Count);

            for (int i = 0; i < images.Count; ++i) {
                var pos = packResult.positions[i];
                var image = images[i];
                float duration = file.frames[i].duration;

                var metadata = new Sprite();
                metadata.frame = file.frames[i];
                metadata.image = textureImage;
                metadata.duration = duration * 0.001f;
                metadata.name = ctx.fileNameNoExt + "_" + i;
                metadata.alignment = SpriteAlignment.Custom;
                metadata.rect = new RectangleF(pos.x, pos.y, image.finalWidth, image.finalHeight);
                metadata.quad = Graphics.NewQuad(pos.x, pos.y, image.finalWidth, image.finalHeight, packResult.imageSize, packResult.imageSize);

                // calculate relative pivot
                metadata.imgQuadOffset = new Vector2(image.minx, image.miny);

                //ctx.spriteCropPositions.Add(new Vector2(image.minx, file.height - image.maxy - 1));

                metaList.Add(metadata);
            }

            return metaList;
        }

        /// Pack the atlas
        static PackResult PackAtlas(List<PackData> list, int border)
        {
            int size = 128;
            while (true)
            {
                var result = DoPackAtlas(list, size, border);
                if (result != null)
                    return result;
                size *= 2;
            }
        }

        static PackResult DoPackAtlas(List<PackData> list, int size, int border) {
            // Pack using the most simple shelf algorithm
        
            List<PackPos> posList = new List<PackPos>();

            // x: the position after last rect; y: the baseline height of current shelf
            // axis: x left -> right, y bottom -> top
            int x = 0, y = 0; 
            int shelfHeight = 0;

            foreach (var data in list) {
                if (data.width > size)
                    return null;
                if (x + data.width + border > size) { // create a new shelf
                    y += shelfHeight;
                    x = 0;
                    shelfHeight = data.height + border;
                } else if (data.height + border > shelfHeight) { // increase shelf height
                    shelfHeight = data.height + border;
                }

                if (y + shelfHeight > size) { // can't place this anymore
                    return null;
                }

                posList.Add(new PackPos { x = x, y = y });

                x += data.width + border;
            }

            return new PackResult {
                imageSize = size,
                positions = posList
            };
        }

        class FrameImage {

            public int minx = int.MaxValue, miny = int.MaxValue, 
                       maxx = int.MinValue, maxy = int.MinValue;

            public int finalWidth { get { return maxx - minx + 1; } }

            public int finalHeight { get { return maxy - miny + 1; } }

            public readonly int width, height;

            readonly Color[] data;

            public FrameImage(int width, int height) {
                this.width = width;
                this.height = height;
                data = new Color[this.width * this.height];
                for (int i = 0; i < data.Length; ++i) {
                    data[i].a = 0;
                }
            }

            public Color GetPixel(int x, int y) {
                int idx = y * width + x;
                if (idx < 0 || idx >= data.Length) {
                    throw new Exception(string.Format("Pixel read of range! x: {0}, y: {1} where w: {2}, h: {3}", x, y, width, height));
                }
                return data[idx];
            }

            public void SetPixel(int x, int y, Color color) {
                data[y * width + x] = color;
            }

        }

    }
}

