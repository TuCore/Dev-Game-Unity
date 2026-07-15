using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingTextAnim : MonoBehaviour
{
    private TextMeshProUGUI _text;
    private RectTransform _rect;
    public float moveSpeed = 50f;
    public float fadeSpeed = 1f;
    public float lifeTime = 2f;

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
        _rect = GetComponent<RectTransform>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        StartCoroutine(AnimateText());
    }

    private IEnumerator AnimateText()
    {
        Color startColor = _text.color;
        float timer = 0f;

        while (timer < lifeTime)
        {
            timer += Time.deltaTime;
            
            // Bay lên
            _rect.anchoredPosition += new Vector2(0, moveSpeed * Time.deltaTime);

            // Mờ dần
            float alpha = Mathf.Lerp(1f, 0f, timer / lifeTime);
            _text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            yield return null;
        }
    }
}
