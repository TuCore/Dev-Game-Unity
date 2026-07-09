using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public MeshRenderer[] danhSachCua;

    void OnMouseDown()
    {
        foreach (MeshRenderer cua in danhSachCua)
        {
            if (cua != null)
            {
                cua.enabled = !cua.enabled;
            }
        }
    }
}