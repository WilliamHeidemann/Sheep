using UnityEngine;
using UtilityToolkit.Editor;

public class VertexAnimationTextureGenerator : MonoBehaviour
{
    [SerializeField] private AnimationClip _clip;
    [SerializeField] private RenderTexture _texture;

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

        // for (int i = 0; i < _clip.length; i++)
        // {
        //     var frame = Mathf.FloorToInt(i * _clip.frameRate);
        //     var time = i / _clip.frameRate;
        //
        // }

        for (int i = 0; i < _texture.height; i++)
        {
            for (int j = 0; j < _texture.width; j++)
            {
            }
        }
    }
    
}
