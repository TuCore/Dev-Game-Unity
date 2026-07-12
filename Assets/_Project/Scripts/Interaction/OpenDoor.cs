using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public MeshRenderer[] danhSachCua;

    void OnMouseDown()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Tiếng mở cửa");
        }

        foreach (MeshRenderer cua in danhSachCua)
        {
            if (cua != null)
            {
                cua.enabled = !cua.enabled;
            }
        }
    }
}