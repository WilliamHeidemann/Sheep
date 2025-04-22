using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
// using UtilityToolkit.Editor;

public class VertexAnimationTextureGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _agent;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private Texture2D _vertexAnimationTexture;
    [SerializeField] private GameObject _tempGameObject;

    // [Button("Convert Clip To Vertex Animation Texture")]
    public void AnimationToVat()
    {
        if (_agent == null || _clip == null)
        {
            Debug.LogWarning("Assign agent and animation clip");
            return;
        }

        var texture = CreateTexture2D();

        // create csv file
        var path = "Assets/VertexAnimationTextures/VAT.csv";
        using (StreamWriter writer = new StreamWriter(path))
        {
            // AnimationMode.StartAnimationMode();
            const int frames = 16;
            for (int i = 0; i < frames; i++)
            {
                float time = _clip.length * i / frames;
                // AnimationMode.SampleAnimationClip(_agent, _clip, time);
                var mesh = Utility.CombineMesh(_agent);
                var vertices = mesh.vertices;
                var frameOffset = i * mesh.vertices.Length;
                for (int j = 0; j < vertices.Length; j++)
                {
                    // var flatIndex = frameOffset + j;
                    // var textureIndexX = flatIndex % texture.width;
                    // var textureIndexY = flatIndex / texture.width;
                    // texture.SetPixel(textureIndexX, textureIndexY, vertices[j].ToColor());
                    writer.WriteLine(vertices[j]);
                }
            }
        }


        // AnimationMode.StopAnimationMode();
        texture.Apply();
        SaveTexture(texture);
    }

    /*
     * For each frame of the animation:
     * Sample the animation clip AnimationMode.SampleAnimationClip(_agent, _clip, time);
     * Create a mesh for each renderer.
     * Each renderer bakes their own mesh.
     * Optionally combines the meshes into one.
     * Write all vertex positions and normals to a texture
     */

    private Texture2D CreateTexture2D()
    {
        var texture = new Texture2D(256, 256, TextureFormat.RGBAHalf, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        return texture;
    }

    private void SaveTexture(Texture2D texture)
    {
        var bytes = texture.EncodeToPNG();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var path = $"Assets/VertexAnimationTextures/VAT_{timestamp}.png";
        File.WriteAllBytes(path, bytes);
        // AssetDatabase.ImportAsset(path);
        Debug.Log("VAT texture saved to: " + path);
    }

    // [Button]
    public void GetTextureContentLength()
    {
        if (!_vertexAnimationTexture.isReadable) return;
        var readable = 0;
        var unreadable = 0;
        var empty = new Color(0, 0, 0, 0);
        var set = new HashSet<Color>();
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                var color = _vertexAnimationTexture.GetPixel(j, i);
                if (color == empty)
                {
                    unreadable++;
                }
                else
                {
                    readable++;
                }

                set.Add(color);
            }
        }

        print($"Different vertex positions: {set.Count}");
        print($"Readable: {readable}. Unreadable: {unreadable}");
    } // 16568 readable

    // [Button]
    public void LogInfo()
    {
        print($"Clip length: {_clip.length}");
        var mesh = Utility.CombineMesh(_agent);
        print($"Vertices count: {mesh.vertices.Length}");
        print($"CSV length: {Utility.ReadVectors().Count()}");
    }

    // [Button]
    public void GetVertexCount()
    {
        var mesh = Utility.CombineMesh(_agent);
        var vertices = mesh.vertices;
        print(vertices.Length);
    }

    // [Button]
    public void SetMesh()
    {
        Mesh mesh = Utility.CombineMesh(_agent);
        const int length = 16544;
        var vertexAnimationBuffer = new Vector3[length];
        var count = 0;
        for (int i = 0; i < 256; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                if (count == length) break;
                var pixel = _vertexAnimationTexture.GetPixel(j, i);
                vertexAnimationBuffer[count] = pixel.ToVector3();
                count++;
            }
        }

        mesh.vertices = vertexAnimationBuffer;

        _tempGameObject.SetActive(true);
        _tempGameObject.GetComponent<MeshFilter>().mesh = mesh;
    }

    // [Button]
    public void SetMeshFromCsv()
    {
        var vertices = Utility.ReadVectors().ToArray();
        var mesh = Utility.CombineMesh(_agent);
        var neededVertices = mesh.vertices.Length;
        Vector3[] vertexAnimationBuffer = new Vector3[neededVertices]; 
        for (int i = 0; i < neededVertices; i++)
        {
            vertexAnimationBuffer[i] = vertices[i];
        }
        mesh.vertices = vertexAnimationBuffer;
        _tempGameObject.SetActive(true);
        _tempGameObject.GetComponent<MeshFilter>().mesh = mesh;
    }
}
// 16 * 1034 = 16544