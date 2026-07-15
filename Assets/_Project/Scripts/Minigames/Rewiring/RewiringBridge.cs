using UnityEngine;

/// <summary>
/// Linh kiện Cầu vượt dây (Wire Bridge Cell) dành cho độ khó Khổ Hạnh (Ascetic / Extreme).
/// Cho phép 2 sợi dây khác màu đi qua cùng 1 ô lưới mà không bị cắt đứt hay chạm chập:
/// - 1 dây chạy ngầm theo hướng Ngang (Horizontal - rãnh dưới).
/// - 1 dây chạy nổi theo hướng Dọc (Vertical - rãnh trên).
/// </summary>
public class RewiringBridge : MonoBehaviour
{
    [Header("Grid Info")]
    public Vector2Int GridCell;

    [Header("Bridge State")]
    public bool IsOccupiedHorizontal = false;
    public bool IsOccupiedVertical = false;
    public WireColor HorizontalWireColor;
    public WireColor VerticalWireColor;

    private GameObject _visualHolder;

    /// <summary>
    /// Khởi tạo dữ liệu cầu vượt tại ô lưới xác định.
    /// </summary>
    public void Initialize(Vector2Int gridCell)
    {
        GridCell = gridCell;
        IsOccupiedHorizontal = false;
        IsOccupiedVertical = false;
    }

    /// <summary>
    /// Kiểm tra hướng di chuyển từ ô trước vào ô cầu vượt này có hợp lệ không.
    /// Cầu vượt bắt buộc phải đi thẳng góc (Ngang ra Ngang, Dọc ra Dọc).
    /// </summary>
    public bool CanWirePassThrough(Vector2Int fromCell, Vector2Int toCell, WireColor wireColor)
    {
        // Nếu đi theo hướng Ngang (cùng y, khác x)
        if (fromCell.y == GridCell.y && toCell.y == GridCell.y)
        {
            return !IsOccupiedHorizontal || HorizontalWireColor == wireColor;
        }
        // Nếu đi theo hướng Dọc (cùng x, khác y)
        if (fromCell.x == GridCell.x && toCell.x == GridCell.x)
        {
            return !IsOccupiedVertical || VerticalWireColor == wireColor;
        }
        // Không cho phép rẽ cua ngay tại ô cầu vượt
        return false;
    }

    /// <summary>
    /// Đăng ký dây đi qua cầu vượt.
    /// </summary>
    public void RegisterWirePass(bool isHorizontal, WireColor color)
    {
        if (isHorizontal)
        {
            IsOccupiedHorizontal = true;
            HorizontalWireColor = color;
        }
        else
        {
            IsOccupiedVertical = true;
            VerticalWireColor = color;
        }
    }

    /// <summary>
    /// Tạo mô hình 3D trực quan cho Cầu vượt trong Scene View.
    /// Mang phong cách cơ khí công nghiệp (Industrial Ceramic & Gunmetal Bridge).
    /// </summary>
    public void CreateVisualModel(float cellWidth, float cellHeight)
    {
        if (_visualHolder != null)
        {
            if (Application.isPlaying) Destroy(_visualHolder);
            else DestroyImmediate(_visualHolder);
        }

        _visualHolder = new GameObject("Bridge_Visual");
        _visualHolder.transform.SetParent(this.transform, false);
        _visualHolder.transform.localPosition = Vector3.zero;

        float minDim = Mathf.Min(cellWidth, cellHeight);

        // 1. Đế bo mạch kim loại xám súng (Base Plate)
        GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        basePlate.name = "Base_Plate";
        basePlate.transform.SetParent(_visualHolder.transform, false);
        basePlate.transform.localPosition = new Vector3(0, 0, 0.05f);
        basePlate.transform.localScale = new Vector3(cellWidth * 0.88f, cellHeight * 0.88f, 0.08f);
        SetColorAndShader(basePlate, new Color(0.18f, 0.20f, 0.24f)); // Gunmetal gray

        // 2. 4 chốt ốc vít cơ khí ở 4 góc (Corner Bolts)
        float offsetX = cellWidth * 0.35f;
        float offsetY = cellHeight * 0.35f;
        Vector2[] boltPositions = new Vector2[]
        {
            new Vector2(-offsetX, -offsetY), new Vector2(offsetX, -offsetY),
            new Vector2(-offsetX, offsetY), new Vector2(offsetX, offsetY)
        };
        foreach (var pos in boltPositions)
        {
            GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bolt.name = "Corner_Bolt";
            bolt.transform.SetParent(_visualHolder.transform, false);
            bolt.transform.localPosition = new Vector3(pos.x, pos.y, 0.01f);
            bolt.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            bolt.transform.localScale = new Vector3(minDim * 0.1f, 0.04f, minDim * 0.1f);
            SetColorAndShader(bolt, new Color(0.75f, 0.78f, 0.82f)); // Silver metallic
        }

        // 3. Khối cách điện sứ cách nhiệt ở giữa (Ceramic Insulator Block)
        GameObject insulator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        insulator.name = "Ceramic_Insulator";
        insulator.transform.SetParent(_visualHolder.transform, false);
        insulator.transform.localPosition = new Vector3(0, 0, -0.02f);
        insulator.transform.localScale = new Vector3(minDim * 0.68f, minDim * 0.68f, 0.12f);
        SetColorAndShader(insulator, new Color(0.86f, 0.84f, 0.78f)); // Ceramic Ivory

        // 4. Rãnh ngầm ngang cho dây dưới (Horizontal Underground Groove)
        GameObject hGroove = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hGroove.name = "Horizontal_Groove";
        hGroove.transform.SetParent(_visualHolder.transform, false);
        hGroove.transform.localPosition = new Vector3(0, 0, -0.05f);
        hGroove.transform.localScale = new Vector3(cellWidth * 0.92f, minDim * 0.26f, 0.06f);
        SetColorAndShader(hGroove, new Color(0.12f, 0.13f, 0.15f)); // Dark trench

        // 5. Cầu vòm nổi dọc cho dây trên (Vertical Overpass Arch)
        GameObject vArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vArch.name = "Vertical_Overpass_Arch";
        vArch.transform.SetParent(_visualHolder.transform, false);
        vArch.transform.localPosition = new Vector3(0, 0, -0.12f);
        vArch.transform.localScale = new Vector3(minDim * 0.32f, cellHeight * 0.92f, 0.07f);
        SetColorAndShader(vArch, new Color(0.92f, 0.55f, 0.15f)); // Industrial Orange Arch

        // Remove Colliders để không ảnh hưởng raycast của chuột khi kéo dây
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
