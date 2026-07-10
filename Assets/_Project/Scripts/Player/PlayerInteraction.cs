using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;

    private Camera _cam;

    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void Update()
    {
        Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                // Trong thực tế sẽ cập nhật UI text hiển thị: interactable.GetInteractPrompt()
                if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
                {
                    interactable.Interact();
                }
            }
        }
    }
}
