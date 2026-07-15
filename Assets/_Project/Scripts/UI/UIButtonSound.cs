using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace AnhThoDien.UI
{
    [RequireComponent(typeof(Selectable))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, ISelectHandler, ISubmitHandler
    {
        [Header("Sound Settings")]
        [SerializeField] private string hoverSoundName = "musicholder-hover-button-287656";
        [SerializeField] private string clickSoundName = "666herohero-click-21156";
        private Selectable _selectable;
        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverSound();
        }
        public void OnSelect(BaseEventData eventData)
        {
            PlayHoverSound();
        }
        public void OnPointerClick(PointerEventData eventData)
        {
            PlayClickSound();
        }
        public void OnSubmit(BaseEventData eventData)
        {
            PlayClickSound();
        }
        private void PlayHoverSound()
        {
            if (_selectable != null && !_selectable.interactable) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(hoverSoundName);
            }
        }
        private void PlayClickSound()
        {
            if (_selectable != null && !_selectable.interactable) return;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clickSoundName);
            }
        }

        public void SetSoundNames(string hover, string click)
        {
            hoverSoundName = hover;
            clickSoundName = click;
        }
    }
}