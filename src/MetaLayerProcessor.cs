using System.Collections;
using System.Collections.Generic;
using Love;

namespace MetaSprite {

public abstract class MetaLayerProcessor {

    public abstract string actionName { get; }

    // The order of execution when importing an ase file. Higher order gets executed later.
    public virtual int executionOrder { get { return 0; } }

    public abstract void Process(ImportContext ctx, Layer layer);


}

}