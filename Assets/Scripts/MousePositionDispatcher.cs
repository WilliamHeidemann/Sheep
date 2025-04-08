using UnityEngine;
using UnityEngine.UI;

public class MousePositionDispatcher : MonoBehaviour
{
    [SerializeField] private ComputeShader _mousePositionShader;
    private RenderTexture _renderTexture;
    [SerializeField] private RawImage _image;
    private int _kernelHandle;
    private const int TextureSize = 64;
    private void Start()
    {
        _renderTexture = new RenderTexture(TextureSize, TextureSize, 0)
        {
            enableRandomWrite = true
        };
        _renderTexture.Create();
        
        _kernelHandle = _mousePositionShader.FindKernel("CSMain");
        _mousePositionShader.SetTexture(_kernelHandle, "Result", _renderTexture);
        _mousePositionShader.SetVector("MousePosition", new Vector2(0.5f, 0.5f));
        _mousePositionShader.Dispatch(_kernelHandle, TextureSize, TextureSize, 1);
        
        _image.texture = _renderTexture;
    }

    private void Update()
    {
        var mousePosition = Input.mousePosition;
        var normalizedMousePosition = new Vector2(mousePosition.x / Screen.width, mousePosition.y / Screen.height);
        _mousePositionShader.SetVector("MousePosition", normalizedMousePosition);
        _mousePositionShader.SetFloat("Time", Time.time);
        _mousePositionShader.Dispatch(_kernelHandle, TextureSize, TextureSize, 1);
    }

    private void OnDestroy()
    {
        _renderTexture?.Release();
    }
}