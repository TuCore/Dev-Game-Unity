using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace AnhThoDien.UI.Menu
{
    public class MenuTabController : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public Button tabButton;
            public GameObject contentPanel;
        }

        public List<Tab> tabs = new List<Tab>();
        public Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        public Color activeColor = new Color(1f, 0.8f, 0f, 1f);
        public Color normalTextColor = Color.white;
        public Color activeTextColor = Color.black;

        private void Start()
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                int index = i; // capture for closure
                if (tabs[index].tabButton != null)
                {
                    tabs[index].tabButton.onClick.AddListener(() => SelectTab(index));
                }
            }

            // Mặc định chọn tab đầu tiên
            if (tabs.Count > 0)
            {
                SelectTab(0);
            }
        }

        public void SelectTab(int index)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                bool isActive = (i == index);
                if (tabs[i].contentPanel != null)
                {
                    tabs[i].contentPanel.SetActive(isActive);
                }

                if (tabs[i].tabButton != null)
                {
                    Image btnImage = tabs[i].tabButton.GetComponent<Image>();
                    if (btnImage != null)
                    {
                        btnImage.color = isActive ? activeColor : normalColor;
                    }

                    TMPro.TextMeshProUGUI tmp = tabs[i].tabButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.color = isActive ? activeTextColor : normalTextColor;
                    }
                }
            }
        }
    }
}
