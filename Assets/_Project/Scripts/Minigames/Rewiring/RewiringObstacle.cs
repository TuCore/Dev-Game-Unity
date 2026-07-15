using UnityEngine;

/// <summary>
/// Linh kiện chết / Chướng ngại vật (Obstacle / Burnt Component Cell) dành cho độ khó Khổ Hạnh.
/// Ngăn chặn không cho dây điện đi xuyên qua, buộc người chơi phải vòng qua hoặc tìm đường qua cầu vượt.
/// </summary>
public class RewiringObstacle : MonoBehaviour
{
    public enum ObstacleType
    {
        BurntCapacitor, // Tụ điện bị cháy xém phồng rộp
        BrokenHeatsink  // Khối tản nhiệt nhô cao cản đường
    }

    [Header("Grid Info")]
    public Vector2Int GridCell;

    [Header("Obstacle Info")]
    public ObstacleType Type = ObstacleType.BurntCapacitor;

    private GameObject _visualHolder;

    public void Initialize(Vector2Int gridCell, ObstacleType type = ObstacleType.BurntCapacitor)
    {
        GridCell = gridCell;
        Type = type;
    }

    /// <summary>
    /// Tạo mô hình 3D trực quan cho Chướng ngại vật trong Scene View.
    /// Mang phong cách linh kiện công nghiệp bị hỏng nặng / cháy khét (Industrial Damaged Component).
    /// </summary>
    public void CreateVisualModel(float cellWidth, float cellHeight)
    {
        if (_visualHolder != null)
        {
            if (Application.isPlaying) Destroy(_visualHolder);
            else DestroyImmediate(_visualHolder);
        }

        _visualHolder = new GameObject("Obstacle_Visual_" + Type);
        _visualHolder.transform.SetParent(this.transform, false);
        _visualHolder.transform.localPosition = Vector3.zero;

        float minDim = Mathf.Min(cellWidth, cellHeight);

        // 1. Đế bo mạch hỏng sờn đen (Damaged Base Plate)
        GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlate.name = "Damaged_Base_Plate";
        basePlate.transform.SetParent(_visualHolder.transform, false);
        basePlate.transform.localPosition = new Vector3(0, 0, 0.05f);
        basePlate.transform.localScale = new Vector3(cellWidth * 0.88f, cellHeight * 0.88f, 0.08f);
        SetColorAndShader(basePlate, new Color(0.12f, 0.12f, 0.14f)); // Charred black

        if (Type == ObstacleType.BurntCapacitor)
        {
            // 2. Thân tụ điện hóa học cháy đen rộp (Charred Electrolytic Capacitor Body)
            GameObject capBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            capBody.name = "Capacitor_Body";
            capBody.transform.SetParent(_visualHolder.transform, false);
            capBody.transform.localPosition = new Vector3(0, 0, -0.12f);
            capBody.transform.localRotation = Quaternion.Euler(90f, 15f, 0); // Hơi nghiêng vì bị nổ
            capBody.transform.localScale = new Vector3(minDim * 0.62f, 0.16f, minDim * 0.62f);
            SetColorAndShader(capBody, new Color(0.16f, 0.18f, 0.22f)); // Dark matte blue/gray

            // 3. Vết khía chữ K trên đỉnh tụ bị bung (Metallic Vent Cross)
            GameObject vent1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vent1.name = "Vent_Cross_1";
            vent1.transform.SetParent(_visualHolder.transform, false);
            vent1.transform.localPosition = new Vector3(0, 0, -0.29f);
            vent1.transform.localScale = new Vector3(minDim * 0.5f, minDim * 0.1f, 0.02f);
            SetColorAndShader(vent1, new Color(0.6f, 0.6f, 0.65f));

            GameObject vent2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vent2.name = "Vent_Cross_2";
            vent2.transform.SetParent(_visualHolder.transform, false);
            vent2.transform.localPosition = new Vector3(0, 0, -0.29f);
            vent2.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            vent2.transform.localScale = new Vector3(minDim * 0.5f, minDim * 0.1f, 0.02f);
            SetColorAndShader(vent2, new Color(0.6f, 0.6f, 0.65f));

            // 4. Đốm tia lửa chập chắp đỏ rực cảnh báo (Short-circuit Hazard Spark)
            GameObject hazardDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hazardDot.name = "Hazard_Spark";
            hazardDot.transform.SetParent(_visualHolder.transform, false);
            hazardDot.transform.localPosition = new Vector3(minDim * 0.25f, minDim * 0.25f, -0.15f);
            hazardDot.transform.localScale = new Vector3(minDim * 0.18f, minDim * 0.18f, minDim * 0.18f);
            SetColorAndShader(hazardDot, new Color(0.96f, 0.15f, 0.20f)); // Bright Danger Red
        }

        // Remove Colliders để không cản trở raycast chuột
        foreach (var col in _visualHolder.GetComponentsInChildren<Collider>())
        {
            if (Application.isPlaying) Destroy(col);
            else DestroyImmediate(col);
        }
    }

    private void SetColorAndShader(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            r.sharedMaterial = mat;
        }
    }
}
