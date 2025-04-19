using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class Utility
{
    public static Vector3 AddFlat(this Vector3 vector3, Vector2 vector2)
    {
        return new Vector3(vector3.x + vector2.x, vector3.y, vector3.z + vector2.y);
    }

    public static Color ToColor(this Vector3 vector3)
    {
        return new Color(vector3.x, vector3.y, vector3.z);
    }

    public static Vector3 ToVector3(this Color color)
    {
        return new Vector3(color.r, color.g, color.b);
    }

    public static Mesh CombineMesh(GameObject objectWithMultipleSkinnedMeshRenderers)
    {
        var skinnedMeshRenderers = objectWithMultipleSkinnedMeshRenderers
            .GetComponentsInChildren<SkinnedMeshRenderer>();
        
        var combineInstances = new CombineInstance[skinnedMeshRenderers.Length];
        for (int i = 0; i < skinnedMeshRenderers.Length; i++)
        {
            var mesh = new Mesh();
            skinnedMeshRenderers[i].BakeMesh(mesh);

            combineInstances[i].mesh = mesh;
            combineInstances[i].transform = skinnedMeshRenderers[i].localToWorldMatrix;
        }

        var combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineInstances);
        return combinedMesh;
    }
    
    public static IEnumerable<Vector3> ReadVectors()
    {
        var path = "Assets/VertexAnimationTextures/VAT.csv";
        if (!File.Exists(path))
        {
            Debug.LogError("CSV file not found");
            yield break;
        }

        using StreamReader reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line == null) yield break;
            string[] entries = line.Split(',');
            entries[0] = entries[0].Substring(1);
            entries[2] = entries[2].Substring(0, entries[2].Length - 1);

            if (entries.Length == 3 &&
                float.TryParse(entries[0], out float x) &&
                float.TryParse(entries[1], out float y) &&
                float.TryParse(entries[2], out float z))
            {
                yield return new Vector3(x, y, z);
            }
        }
    }
}