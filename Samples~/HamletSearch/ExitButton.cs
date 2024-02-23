using UnityEngine;

namespace RAGSearchUnitySamples
{
    public class ExitButton : MonoBehaviour
    {
        public void ExitGame()
        {
            // This method will be called when the button is clicked
            Debug.Log("Exit button clicked");
            Application.Quit();
        }
    }
}
