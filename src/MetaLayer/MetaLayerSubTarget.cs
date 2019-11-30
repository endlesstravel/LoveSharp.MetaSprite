using System.Collections.Generic;
using System.Linq;
using Love;
using Love;
using MetaSprite.Internal;
using System.IO;

namespace MetaSprite {

public class MetaLayerSubTarget : MetaLayerProcessor {

    // syntax: @subTarget(string subImageName, string targetChildObject)

    public override string actionName {
        get { return "subTarget"; }
    }

    public override int executionOrder {
        get { 
            return 1; // After specificaiton of all @sub layers.
        }
    }

    public override void Process(ImportContext context, Layer layer) {
        string subImageName = layer.GetParamString(0);
        string targetChildObject = layer.GetParamString(1);

        List<Layer> layers; 
        context.subImageLayers.TryGetValue(subImageName, out layers);

        var sprites = AtlasGenerator.GenerateAtlas(context, layers);
        ASEImporter.GenerateClipImageLayer(context, targetChildObject, sprites);
    }

}

}