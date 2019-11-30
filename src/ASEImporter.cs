using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using Love;

using MetaSprite.Internal;
using System.Linq;

namespace MetaSprite {

    public class ImportContext {

        public ASEFile file;

        public string fileDirectory;
        public string fileName;
        public string fileNameNoExt;
        
        public List<Sprite> generatedSprites = new List<Sprite>();

        // The local texture coordinate for bottom-left point of each frame's crop rect, in Unity texture space.
        public List<Vector2> spriteCropPositions = new List<Vector2>();

        public Dictionary<FrameTag, AnimationClip> generatedClips = new Dictionary<FrameTag, AnimationClip>();

        public Dictionary<string, List<Layer>> subImageLayers = new Dictionary<string, List<Layer>>();

    }

    public static class ASEImporter {

        static readonly Dictionary<string, MetaLayerProcessor> layerProcessors = new Dictionary<string, MetaLayerProcessor>();

        static Type[] FindAllTypes(Type interfaceType) {
            var types = System.Reflection.Assembly.GetExecutingAssembly()
                .GetTypes();
            return types.Where(type => type.IsClass && interfaceType.IsAssignableFrom(type))
                        .ToArray();
        }

        struct LayerAndProcessor {
            public Layer layer;
            public MetaLayerProcessor processor;
        }


        public static SpriteAnimation Import(string path, string initTagName) {

            var context = new ImportContext {
                // file = file,
                fileDirectory = Path.GetDirectoryName(path),
                fileName = Path.GetFileName(path),
                fileNameNoExt = Path.GetFileNameWithoutExtension(path)
            };

            // parse file
            context.file = ASEParser.Parse(System.IO.File.ReadAllBytes(path)); 

            // generate sprite
            context.generatedSprites = AtlasGenerator.GenerateAtlas(context, 
                context.file.layers.Where(it => it.type == LayerType.Content).ToList());

            // generate animation clips
            GenerateAnimClips(context);

            // process other ...
            context.file.layers
                .Where(layer => layer.type == LayerType.Meta)
                .Select(layer => {
                    MetaLayerProcessor processor;
                    layerProcessors.TryGetValue(layer.actionName, out processor);
                    return new LayerAndProcessor { layer = layer, processor = processor };                     
                })
                .OrderBy(it => it.processor != null ? it.processor.executionOrder : 0)
                .ToList()
                .ForEach(it => {
                    var layer = it.layer;
                    var processor = it.processor;
                    if (processor != null) {
                        processor.Process(context, layer);
                    } else {
                        Log.Warnning(string.Format("No processor for meta layer {0}", layer.layerName));                        
                    }
                });

            Dictionary<string, AnimationClip> dict = new Dictionary<string, AnimationClip>();
            foreach (var kv in context.generatedClips)
            {
                dict[kv.Key.name] = kv.Value;
            }

            return new SpriteAnimation(dict, initTagName);
        }

        static void GenerateAnimClips(ImportContext ctx)
        {
            // Generate one animation for each tag
            foreach (var tag in ctx.file.frameTags) {

                // Create clip
                AnimationClip clip = new AnimationClip(tag, ctx.generatedSprites, ctx.file.frames);
                ctx.generatedClips.Add(tag, clip);
            }
        }
    }
}