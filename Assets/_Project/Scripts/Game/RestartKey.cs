using UnityEngine;
using UnityEngine.SceneManagement;

namespace PiperToy
{
    /// <summary>
    /// Starts the toy again from the beginning when the player presses R: it loads the scene afresh, so the Piper,
    /// the frogs and every song are back as they were when the game started.
    /// </summary>
    public sealed class RestartKey : MonoBehaviour
    {
        [SerializeField, Tooltip("The key that starts the toy again.")]
        private KeyCode _key = KeyCode.R;

        public KeyCode Key => _key;

        private void Update()
        {
            if (Input.GetKeyDown(_key))
            {
                Restart();
            }
        }

        /// <summary>Loads the open scene again, which throws away everything that happened in it.</summary>
        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
