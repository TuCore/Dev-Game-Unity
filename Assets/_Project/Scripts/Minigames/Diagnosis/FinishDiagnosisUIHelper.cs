using UnityEngine;
using UnityEngine.UI;

namespace Minigames.Diagnosis
{
    [RequireComponent(typeof(Button))]
    public class FinishDiagnosisUIHelper : MonoBehaviour
    {
        private void Start()
        {
            Button btn = GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                DiagnosisMinigame minigame = GetComponentInParent<DiagnosisMinigame>(true);
                if (minigame == null) minigame = Object.FindObjectOfType<DiagnosisMinigame>();
                if (minigame != null)
                {
                    minigame.FinishDiagnosis();
                }
            });
        }
    }
}
