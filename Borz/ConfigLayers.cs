using AkoSharp;

namespace Borz;

public class ConfigLayers
{
    public enum LayerType : int
    {
        Defaults = 0,    //Defaults in borz
        UserGobal = 1,   // Users global config e.g ~/.borz/config 
        Workspace = 2,    // Workspace specific config e.g. .borz/config
        UserWorkspace = 3, // User specific project config e.g. .borz/config.ako
        Last = 4
    }
    
    public Dictionary<LayerType, AkoVar> Layers = new();

    public ConfigLayers()
    {
        for (int i = 0; i < (int)LayerType.Last; i++)
        {
            var layer = (LayerType) i;
            if (!Layers.ContainsKey(layer))
            {
                Layers.Add(layer, new AkoVar(AkoVar.VarType.TABLE));
            }
        }
    }

    public AkoVar GetLayer(LayerType layer)
    {
        return Layers[layer];
    }
    
    public void SetLayer(LayerType layer, AkoVar value)
    {
        Layers[layer] = value;
    }

    public AkoVar? Get(params string[] path)
    {
        //Go from Last to Defaults
        //We need to find what layer the value is in accounting for tables.
        //Recursively go through the layers and find the value.
        
        for (int i = (int) LayerType.Last - 1; i >= (int) LayerType.Defaults; i--)
        {
            var table = Layers[(LayerType) i];
            if(table.Count == 0)
               continue;
            
            var currentVar = table;
            
            bool found = false;
            
            //Now traverse this layers table and see if it has the value
            //we need to make sure the last part in the path is used, we cannot ignore it.
            for (int j = 0; j < path.Length; j++)
            {
                var key = path[j];
                if (currentVar.Type == AkoVar.VarType.TABLE)
                {
                    if (currentVar.ContainsKey(key))
                    {
                        currentVar = currentVar[key];
                        found = true;
                    }
                    else
                    {
                        found = false;
                        break;
                    }
                }
                //if we are at the last part of the path then we can return the value
                else if (j == path.Length - 1)
                {
                    return currentVar;
                }
                else
                {
                    found = false;
                    break;
                }
            }
            
            if (found)
            {
                return currentVar;
            }
        }
        
        return null;
    }
}