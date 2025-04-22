using TMPro;
using UnityEngine;
using static UnityEngine.SceneManagement.SceneManager;

namespace Components
{
    public class GameResetter : MonoBehaviour
    {
        [SerializeField] private AgentCounter _agentCounter;
        
        private TMP_InputField _inputField;
        
        private void Awake()
        {
            _inputField = GetComponent<TMP_InputField>();
            _inputField.onEndEdit.AddListener(ResetGame);
        }
        
        public void ResetGame(string input)
        {
            if (int.TryParse(input, out var number))
            {
                _agentCounter.AgentCount = number;
            }
            
            // reload scene
            var scene = GetActiveScene();
            if (scene.isLoaded)
            {
                LoadScene(scene.name);
            }
        }
    }
}
