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
}