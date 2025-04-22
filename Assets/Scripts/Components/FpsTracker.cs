using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Components
{
    public class FpsTracker : MonoBehaviour
    {
        private TextMeshProUGUI _fpsText;

        private void Awake()
        {
            _fpsText = GetComponent<TextMeshProUGUI>();
        }

        private async void Start()
        {
            while (true)
            {
                var fps = await GetFpsForOneSecond();
                _fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            }
        }

        private async Task<float> GetFpsForOneSecond()
        {
            float fps = 0;
            for (int i = 0; i < 60; i++)
            {
                fps += 1 / Time.deltaTime;
                await Awaitable.EndOfFrameAsync();
            }

            return fps / 60;
        }
    }
}
