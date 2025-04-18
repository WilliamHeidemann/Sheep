using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Windows;
using UtilityToolkit.Editor;

public class VertexAnimationTextureGenerator : MonoBehaviour
{
    [SerializeField] private GameObject _agent;
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private RenderTexture _texture;
    // field to store a png
    [SerializeField] private Texture2D _texture2D;
    
    [Button]
    public void Generate()
    {
        #region GuardClause

        if (_clip == null)
        {
            Debug.LogError("Animation clip is not assigned.");
            return;
        }

        if (_texture == null)
        {
            Debug.LogError("Render texture is not assigned.");
            return;
        }

        #endregion

        var texture = CreateTexture2D();

        for (int i = 0; i < _texture.height; i++)
        {
            for (int j = 0; j < _texture.width; j++)
            {
                var colorValue = (i + j) / 2f / 256f;
                texture.SetPixel(j, i, new Color(colorValue, colorValue, colorValue, 1));
            }
        }

        texture.Apply();

        SaveTexture(texture);
    }

    [Button]
    public void SampleAnimationClip()
    {
        var texture = CreateTexture2D();
        AnimationMode.StartAnimationMode();
        const int frames = 16;
        for (int i = 0; i < frames; i++)
        {
            float time = _clip.length * i / frames;
            AnimationMode.SampleAnimationClip(_agent, _clip, time);
            var mesh = Utility.CombineMesh(_agent);
            var vertices = mesh.vertices;
            var frameOffset = i * mesh.vertices.Length;
            for (int j = 0; j < vertices.Length; j++)
            {
                var flatIndex = frameOffset + j;
                var textureIndexX = flatIndex % texture.width;
                var textureIndexY = flatIndex / texture.width;
                texture.SetPixel(textureIndexX, textureIndexY, vertices[j].ToColor());
            }
        }
        AnimationMode.StopAnimationMode();
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
        var texture = new Texture2D(256, 256, TextureFormat.RGBAHalf, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    private void SaveTexture(Texture2D texture)
    {
        var bytes = texture.EncodeToPNG();
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var path = $"Assets/VertexAnimationTextures/VAT_{timestamp}.png";
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("VAT texture saved to: " + path);
    }
}