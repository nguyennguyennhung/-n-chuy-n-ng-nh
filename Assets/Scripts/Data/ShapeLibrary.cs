using UnityEngine;

public static class ShapeLibrary
{
    public static ShapeDefinition CreateCube(float s = 0.5f)
    {
        ShapeDefinition def = new ShapeDefinition();

        def.vertices = new Vector3[]
        {
            new Vector3(-s,-s,-s),
            new Vector3(s,-s,-s),
            new Vector3(s,-s,s),
            new Vector3(-s,-s,s),

            new Vector3(-s,s,-s),
            new Vector3(s,s,-s),
            new Vector3(s,s,s),
            new Vector3(-s,s,s),
        };

        def.labels = new string[]
        {
            "A","B","C","D",
            "A'","B'","C'","D'"
        };

        def.edges = new int[,]
        {
            {0,1},{1,2},{2,3},{3,0},
            {4,5},{5,6},{6,7},{7,4},
            {0,4},{1,5},{2,6},{3,7}
        };

        return def;
    }
}