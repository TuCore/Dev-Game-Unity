using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Đại diện cho một đường dây nối giữa 2 cọc.
/// Hỗ trợ lưu trữ đường vẽ tự do đa điểm (Polyline PathPoints) và thuật toán kiểm tra giao cắt chuẩn xác tuyệt đối bằng Cross Product + khoảng cách.
/// </summary>
public class RewiringWire : MonoBehaviour
{
    public RewiringTerminal StartTerminal { get; private set; }
    public RewiringTerminal EndTerminal { get; private set; }
    public WireColor Color { get; private set; }

    /// <summary>
    /// Danh sách các điểm trên đường nét vẽ tự do của dây.
    /// </summary>
    public List<Vector2> PathPoints { get; private set; } = new List<Vector2>();

    /// <summary>
    /// Danh sách các ô lưới (Grid Cells) mà đường dây đi qua trên Bảng lưới ô vuông.
    /// </summary>
    public List<Vector2Int> CellPath { get; private set; } = new List<Vector2Int>();

    public void Initialize(RewiringTerminal start, RewiringTerminal end, WireColor color, List<Vector2> customPath = null)
    {
        StartTerminal = start;
        EndTerminal = end;
        Color = color;

        PathPoints.Clear();
        if (customPath != null && customPath.Count >= 2)
        {
            PathPoints.AddRange(customPath);
        }
        else
        {
            PathPoints.Add((Vector2)start.transform.position);
            PathPoints.Add((Vector2)end.transform.position);
        }

        StartTerminal.ConnectWire();
        EndTerminal.ConnectWire();
    }

    /// <summary>
    /// Khởi tạo dây trên Bảng lưới ô vuông (Grid Board).
    /// </summary>
    public void InitializeGrid(RewiringTerminal start, RewiringTerminal end, WireColor color, List<Vector2Int> cellPath, List<Vector2> worldPoints)
    {
        StartTerminal = start;
        EndTerminal = end;
        Color = color;

        CellPath.Clear();
        if (cellPath != null) CellPath.AddRange(cellPath);

        PathPoints.Clear();
        if (worldPoints != null) PathPoints.AddRange(worldPoints);

        StartTerminal.ConnectWire();
        EndTerminal.ConnectWire();
    }

    /// <summary>
    /// Kiểm tra xem dây có nối thông đầy đủ từ cọc nguồn đến cọc đích hay không.
    /// </summary>
    public bool IsFullyConnected
    {
        get
        {
            if (StartTerminal == null || EndTerminal == null) return false;
            if (CellPath.Count > 0)
            {
                return CellPath[0] == StartTerminal.GridCell && CellPath[CellPath.Count - 1] == EndTerminal.GridCell;
            }
            if (PathPoints.Count >= 2)
            {
                return Vector2.Distance(PathPoints[0], StartTerminal.Position) < 0.3f &&
                       Vector2.Distance(PathPoints[PathPoints.Count - 1], EndTerminal.Position) < 0.3f;
            }
            return false;
        }
    }

    /// <summary>
    /// Cập nhật toàn bộ điểm trên đường dây.
    /// </summary>
    public void SetPathPoints(List<Vector2> newPoints)
    {
        if (newPoints != null && newPoints.Count >= 2)
        {
            PathPoints.Clear();
            PathPoints.AddRange(newPoints);
        }
    }

    /// <summary>
    /// Cắt đứt dây từ ô có chỉ số cutIndex trở đi (từ chỗ bị đâm trở xuống sẽ bị mất hoàn toàn).
    /// Trả về true nếu dây bị xóa hoàn toàn (do số lượng điểm còn lại < 2 hoặc cắt ngay đầu).
    /// </summary>
    public bool TrimAtCellIndex(int cutIndex)
    {
        if (cutIndex <= 0)
        {
            StartTerminal?.DisconnectWire();
            EndTerminal?.DisconnectWire();
            CellPath.Clear();
            PathPoints.Clear();
            return true;
        }

        if (cutIndex < CellPath.Count)
        {
            // Vì dây không còn chạm tới EndTerminal nữa nên ngắt kết nối đích
            EndTerminal?.DisconnectWire();

            // Xóa từ cutIndex đến hết
            int removeCount = CellPath.Count - cutIndex;
            CellPath.RemoveRange(cutIndex, removeCount);

            if (cutIndex < PathPoints.Count)
            {
                int removePointsCount = PathPoints.Count - cutIndex;
                PathPoints.RemoveRange(cutIndex, removePointsCount);
            }
        }

        if (CellPath.Count < 2 || PathPoints.Count < 2)
        {
            StartTerminal?.DisconnectWire();
            CellPath.Clear();
            PathPoints.Clear();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra xem đường nét vẽ đa điểm của dây này có cắt qua hoặc đè lên đường nét vẽ của dây khác hay không.
    /// </summary>
    public bool IntersectsWith(RewiringWire otherWire)
    {
        return CountIntersectionsWith(otherWire) > 0;
    }

    /// <summary>
    /// Đếm chính xác số điểm giao cắt đâm chéo riêng biệt (distinct crossing points) hoặc ô lưới chồng chéo giữa 2 đường dây.
    /// </summary>
    public int CountIntersectionsWith(RewiringWire otherWire)
    {
        if (otherWire == null || otherWire == this) return 0;

        RewiringBridge[] bridges = UnityEngine.Object.FindObjectsOfType<RewiringBridge>();

        // Nếu cả 2 dây đều đi theo ô lưới (Grid Board) -> kiểm tra trùng/đè ô lưới chính xác tuyệt đối
        if (CellPath.Count > 0 && otherWire.CellPath.Count > 0)
        {
            int count = 0;
            foreach (var cell in CellPath)
            {
                if (otherWire.CellPath.Contains(cell))
                {
                    bool isBridge = false;
                    foreach (var br in bridges)
                    {
                        if (br != null && br.GridCell == cell)
                        {
                            isBridge = true;
                            break;
                        }
                    }
                    if (!isBridge)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        if (PathPoints.Count < 2 || otherWire.PathPoints.Count < 2) return 0;

        List<Vector2> distinctCrossings = new List<Vector2>();

        for (int i = 0; i < PathPoints.Count - 1; i++)
        {
            Vector2 p1 = PathPoints[i];
            Vector2 p2 = PathPoints[i + 1];

            for (int j = 0; j < otherWire.PathPoints.Count - 1; j++)
            {
                Vector2 p3 = otherWire.PathPoints[j];
                Vector2 p4 = otherWire.PathPoints[j + 1];

                if (DoLineSegmentsIntersect(p1, p2, p3, p4))
                {
                    Vector2 crossPoint = GetClosestIntersectionPoint(p1, p2, p3, p4);

                    // Kiểm tra xem điểm giao cắt này có nằm ngay trên Cầu Vượt hay không (nếu có thì hợp lệ, không tính lỗi)
                    bool isAtBridge = false;
                    foreach (var br in bridges)
                    {
                        if (br != null && Vector2.Distance(crossPoint, (Vector2)br.transform.position) < 0.65f)
                        {
                            isAtBridge = true;
                            break;
                        }
                    }
                    if (isAtBridge) continue;

                    // Kiểm tra xem điểm giao cắt này đã thuộc một cụm giao cắt (crossing point) vừa tìm thấy trước đó chưa
                    // (khoảng cách < 0.45f tức là cùng thuộc 1 điểm cắt chéo trên màn hình)
                    bool alreadyCounted = false;
                    foreach (var cp in distinctCrossings)
                    {
                        if (Vector2.Distance(cp, crossPoint) < 0.45f)
                        {
                            alreadyCounted = true;
                            break;
                        }
                    }

                    if (!alreadyCounted)
                    {
                        distinctCrossings.Add(crossPoint);
                    }
                }
            }
        }
        return distinctCrossings.Count;
    }

    private static Vector2 GetClosestIntersectionPoint(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float A1 = p2.y - p1.y;
        float B1 = p1.x - p2.x;
        float C1 = A1 * p1.x + B1 * p1.y;

        float A2 = p4.y - p3.y;
        float B2 = p3.x - p4.x;
        float C2 = A2 * p3.x + B2 * p3.y;

        float det = A1 * B2 - A2 * B1;
        if (Mathf.Abs(det) > 0.0001f)
        {
            float x = (B2 * C1 - B1 * C2) / det;
            float y = (A1 * C2 - A2 * C1) / det;
            return new Vector2(x, y);
        }

        return (p1 + p2 + p3 + p4) * 0.25f;
    }

    /// <summary>
    /// Thuật toán kiểm tra 2 đoạn thẳng (p1-p2) và (p3-p4) có đâm chéo cắt ngang qua nhau hay không.
    /// </summary>
    public static bool DoLineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        // 1. Sử dụng Cross Product (CCW orientation test) chuẩn xác 100% để phát hiện cắt chéo nhau
        float d1 = CrossProduct(p3 - p1, p2 - p1);
        float d2 = CrossProduct(p4 - p1, p2 - p1);
        float d3 = CrossProduct(p1 - p3, p4 - p3);
        float d4 = CrossProduct(p2 - p3, p4 - p3);

        if (((d1 > 0.0001f && d2 < -0.0001f) || (d1 < -0.0001f && d2 > 0.0001f)) &&
            ((d3 > 0.0001f && d4 < -0.0001f) || (d3 < -0.0001f && d4 > 0.0001f)))
        {
            return true;
        }

        // 2. Kiểm tra khoảng cách giữa 2 đoạn thẳng: nếu nét vẽ đi cắt sát đè lên nhau (< 0.16f) -> Cắt đè chéo
        if (GetSegmentToSegmentDistance(p1, p2, p3, p4) < 0.16f)
        {
            return true;
        }

        return false;
    }

    private static float CrossProduct(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private static float GetSegmentToSegmentDistance(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        return Mathf.Min(
            DistancePointToSegment(p1, p3, p4),
            DistancePointToSegment(p2, p3, p4),
            DistancePointToSegment(p3, p1, p2),
            DistancePointToSegment(p4, p1, p2)
        );
    }

    private static float DistancePointToSegment(Vector2 pt, Vector2 p1, Vector2 p2)
    {
        float l2 = Vector2.SqrMagnitude(p2 - p1);
        if (l2 < 0.0001f) return Vector2.Distance(pt, p1);

        float t = Mathf.Clamp01(Vector2.Dot(pt - p1, p2 - p1) / l2);
        Vector2 projection = p1 + t * (p2 - p1);
        return Vector2.Distance(pt, projection);
    }
}
