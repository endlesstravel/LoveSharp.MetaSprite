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
        //public List<Vector2> spriteCropPositions = new List<Vector2>();

        public Dictionary<FrameTag, AnimationClip> generatedClips = new Dictionary<FrameTag, AnimationClip>();
        public Dictionary<FrameTag, AnimationClip> generatedReversedClips = new Dictionary<FrameTag, AnimationClip>();

        public Dictionary<string, List<Layer>> subImageLayers = new Dictionary<string, List<Layer>>();

    }

    public static class ASEImporter {

        static readonly Dictionary<string, MetaLayerProcessor> layerProcessors = new MetaLayerProcessor[]
        {
            new MetaLayerPivot(),
            new MetaLayerRect(),
            new MetaLayerTransform(),
            new MetaLayerEvent(),
        }.ToDictionary(item => item.actionName);


        //public static void RefresProcessor()
        //{
        //    layerProcessors.Clear();
        //    var processorTypes = FindAllTypes(typeof(MetaLayerProcessor));
        //    // Debug.Log("Found " + processorTypes.Length + " layer processor(s).");
        //    foreach (var type in processorTypes)
        //    {
        //        if (type.IsAbstract) continue;
        //        try
        //        {
        //            var instance = (MetaLayerProcessor)type.GetConstructor(new Type[0]).Invoke(new object[0]);
        //            if (layerProcessors.ContainsKey(instance.actionName))
        //            {
        //                Log.Error(string.Format("Duplicate processor with name {0}: {1}", instance.actionName, instance));
        //            }
        //            else
        //            {
        //                layerProcessors.Add(instance.actionName, instance);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Log.Error("Can't instantiate meta processor " + type);
        //            Log.Error(ex);
        //        }
        //    }
        //}


        //static Type[] FindAllTypes(Type interfaceType) {
        //    var types = System.Reflection.Assembly.GetExecutingAssembly()
        //        .GetTypes();
        //    return types.Where(type => type.IsClass && interfaceType.IsAssignableFrom(type))
        //                .ToArray();
        //}

        struct LayerAndProcessor {
            public Layer layer;
            public MetaLayerProcessor processor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="initTagName">null for random tag</param>
        /// <returns></returns>
        public static AsepriteAnimation Import(string path, string initTagName) {

            var context = new ImportContext {
                // file = file,
                fileDirectory = Path.GetDirectoryName(path),
                fileName = Path.GetFileName(path),
                fileNameNoExt = Path.GetFileNameWithoutExtension(path)
            };

            // parse file
            context.file = ASEParser.Parse(System.IO.File.ReadAllBytes(path));


            // gen default tag for no tag file
            GenerateFrameTagForNoTagFile(context);

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
                        Log.Warnning($"No processor for meta layer {layer.layerName}           \t {path}");                        
                    }
                });

            Dictionary<string, AnimationClip> dict = new Dictionary<string, AnimationClip>();
            foreach (var kv in context.generatedClips)
            {
                dict[kv.Key.name] = kv.Value;
            }
            Dictionary<string, AnimationClip> revsrse_dict = new Dictionary<string, AnimationClip>();
            foreach (var kv in context.generatedReversedClips)
            {
                revsrse_dict[kv.Key.name] = kv.Value;
            }

            if (initTagName == null)
                initTagName = dict.Keys.FirstOrDefault();

            return new AsepriteAnimation(dict, revsrse_dict, context.file.width, context.file.height, initTagName);
        }

        public const string DefaultTagName = "_____default_______tag_______name_____";
        static void GenerateFrameTagForNoTagFile(ImportContext ctx)
        {
            if (ctx.file.frameTags.Count == 0)
            {
                var defaultTag = new FrameTag();
                defaultTag.from = 0;
                defaultTag.to = ctx.file.frames.Count - 1;
                defaultTag.name = DefaultTagName;
                ctx.file.frameTags.Add(defaultTag);
            }
        }

        static void GenerateAnimClips(ImportContext ctx)
        {
            // Generate one animation for each tag
            foreach (var tag in ctx.file.frameTags) {

                // Create clip
                ctx.generatedClips.Add(tag, new AnimationClip(tag, ctx.generatedSprites, ctx.file.frames, false));
                ctx.generatedReversedClips.Add(tag, new AnimationClip(tag, ctx.generatedSprites, ctx.file.frames, true));
            }
        }
    }
}
