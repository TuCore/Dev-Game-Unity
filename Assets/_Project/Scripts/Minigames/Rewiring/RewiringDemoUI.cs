using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Giao diện chơi thử (Interactive Playable Demo) cho Minigame Nối dây (Rewiring).
/// Tự động dựng bo mạch, cọc nối màu, hỗ trợ đi dây tự do (Freehand Polyline / Vẽ vòng vèo)
/// để người chơi trải nghiệm đúng chuẩn nối dây: Tránh chéo dây -> Có tâm | Đâm chéo dây -> Nối ẩu.
[ExecuteAlways]
public class RewiringDemoUI : MonoBehaviour
{
    [Header("Âm thanh Minigame")]
    [SerializeField] private AudioClip zapAudioClip;
    [SerializeField] private AudioClip successAudioClip;

    private RewiringController _controller;
    private Camera _mainCamera;

    private List<DemoTerminalElement> _terminals = new List<DemoTerminalElement>();
    private List<DemoWireElement> _wires = new List<DemoWireElement>();
    private List<RewiringBridge> _activeBridges = new List<RewiringBridge>();
    private List<RewiringObstacle> _activeObstacles = new List<RewiringObstacle>();

    private DemoTerminalElement _dragStartTerminal = null;
    private LineRenderer _previewLine = null;
    private LineRenderer _previewCoreStart = null;
    private LineRenderer _previewBootStart = null;
    private Dictionary<WireColor, Texture2D> _cached3DWireTextures = new Dictionary<WireColor, Texture2D>();
    private Texture2D _cachedCopperTexture = null;
    private Texture2D _guideStep1Texture = null;
    private List<Vector2> _currentDrawPoints = new List<Vector2>();
    private List<Vector2Int> _currentCellPath = new List<Vector2Int>();

    private int _gridRows = 4;
    private int _gridCols = 6;
    private float _cellWidth = 1.5f;
    private float _cellHeight = 1.35f;
    private GameObject _gridVisualHolder = null;

    private string _resultText = "";
    private bool _showGuide = false;
    private int _guidePageIndex = 0;
    private Vector2 _guideScrollPosition = Vector2.zero;
    private GUIStyle _titleStyle;
    private GUIStyle _guideStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _resultStyle;
    private GUIStyle _popupWindowStyle;
    private GUIStyle _guideTitleStyle;
    private GUIStyle _guideBodyStyle;
    private GUIStyle _guideCloseBtnStyle;
    private GUIStyle _guideIllustrationBoxStyle;
    private GUIStyle _guideArrowBtnStyle;
    private GUIStyle _guidePageIndicatorStyle;

    public class DemoTerminalElement : MonoBehaviour
    {
        public RewiringTerminal data;
        public RewiringDemoUI manager;
        public MeshRenderer outline;

        private void OnMouseDown()
        {
            if (data == null) return;
            manager.OnTerminalBeginDrag(this);
        }

        private void OnMouseUp()
        {
            manager.OnTerminalEndDrag(this);
        }

        private void OnMouseEnter()
        {
            if (outline != null) outline.enabled = true;
        }

        private void OnMouseExit()
        {
            if (outline != null) outline.enabled = false;
        }
    }

    public class DemoWireElement
    {
        public RewiringWire data;
        public GameObject holderObj;
        public LineRenderer line;
        public DemoTerminalElement startElem;
        public DemoTerminalElement endElem;
    }

    private void Start()
    {
        SetupCamera();
        SetupController();
        SetupPreviewLine();
        PreloadAudioClips();
        if (Application.isPlaying || transform.childCount < 5)
        {
            InitializeDemoBoard(3);
        }
    }

    private void PreloadAudioClips()
    {
        if (zapAudioClip != null) zapAudioClip.LoadAudioData();
        if (successAudioClip != null) successAudioClip.LoadAudioData();
    }

    private void SetupCamera()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            GameObject camObj = new GameObject("Demo_MainCamera");
            _mainCamera = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
        }
        _mainCamera.clearFlags = CameraClearFlags.SolidColor;
        _mainCamera.orthographic = true;
        _mainCamera.orthographicSize = 4.8f;
        _mainCamera.transform.position = new Vector3(0, 0, -10f);
        _mainCamera.backgroundColor = new Color(0.12f, 0.13f, 0.15f, 1f); // Nền xám kim loại cơ khí
    }

    private void SetupController()
    {
        Transform existing = transform.Find("RewiringLogic_Controller");
        if (existing != null)
        {
            _controller = existing.GetComponent<RewiringController>();
            if (_controller != null) return;
            DestroyHelper(existing.gameObject);
        }

        GameObject controllerObj = new GameObject("RewiringLogic_Controller");
        controllerObj.transform.SetParent(this.transform);
        _controller = controllerObj.AddComponent<RewiringController>();
        _controller.Initialize(null, 1);
        _controller.StartMinigame();
    }

    private void SetupPreviewLine()
    {
        Transform existing = transform.Find("Preview_Line");
        if (existing != null)
        {
            _previewLine = existing.GetComponent<LineRenderer>();
            _previewCoreStart = transform.Find("Preview_Core_Start")?.GetComponent<LineRenderer>();
            _previewBootStart = transform.Find("Preview_Boot_Start")?.GetComponent<LineRenderer>();
            if (_previewLine != null && _previewCoreStart != null && _previewBootStart != null) return;
            if (existing != null) DestroyHelper(existing.gameObject);
            var c = transform.Find("Preview_Core_Start"); if (c != null) DestroyHelper(c.gameObject);
            var b = transform.Find("Preview_Boot_Start"); if (b != null) DestroyHelper(b.gameObject);
        }

        GameObject previewObj = new GameObject("Preview_Line");
        previewObj.transform.SetParent(this.transform);
        _previewLine = previewObj.AddComponent<LineRenderer>();
        _previewLine.positionCount = 1;
        _previewLine.startWidth = 0.33f;
        _previewLine.endWidth = 0.33f;
        _previewLine.numCornerVertices = 16;
        _previewLine.numCapVertices = 16;
        _previewLine.material = new Material(Shader.Find("Sprites/Default"));
        _previewLine.enabled = false;

        GameObject previewCoreObj = new GameObject("Preview_Core_Start");
        previewCoreObj.transform.SetParent(this.transform);
        _previewCoreStart = previewCoreObj.AddComponent<LineRenderer>();
        _previewCoreStart.positionCount = 2;
        _previewCoreStart.startWidth = 0.17f;
        _previewCoreStart.endWidth = 0.17f;
        _previewCoreStart.numCapVertices = 12;
        _previewCoreStart.material = new Material(Shader.Find("Unlit/Texture"));
        _previewCoreStart.material.mainTexture = GetOrCreateCopperTexture();
        _previewCoreStart.enabled = false;

        GameObject previewBootObj = new GameObject("Preview_Boot_Start");
        previewBootObj.transform.SetParent(this.transform);
        _previewBootStart = previewBootObj.AddComponent<LineRenderer>();
        _previewBootStart.positionCount = 2;
        _previewBootStart.startWidth = 0.38f;
        _previewBootStart.endWidth = 0.38f;
        _previewBootStart.numCapVertices = 12;
        _previewBootStart.material = new Material(Shader.Find("Sprites/Default"));
        _previewBootStart.material.color = new Color(0.18f, 0.20f, 0.24f);
        _previewBootStart.enabled = false;
    }

    private void SetupGridBoardParameters(int difficulty)
    {
        if (difficulty == 0) // Dễ (30 ô - 3 Cặp màu)
        {
            _gridRows = 5;
            _gridCols = 6;
            _cellWidth = 1.4f;
            _cellHeight = 1.3f;
        }
        else if (difficulty == 1) // Bình thường (48 ô - 4 Cặp màu)
        {
            _gridRows = 6;
            _gridCols = 8;
            _cellWidth = 1.2f;
            _cellHeight = 1.1f;
        }
        else if (difficulty == 2) // Khó (70 ô - 6 Cặp màu)
        {
            _gridRows = 7;
            _gridCols = 10;
            _cellWidth = 1.0f;
            _cellHeight = 0.95f;
        }
        else // Khổ Hạnh (Extreme - 80 ô - 7 Cặp màu + Cầu Vượt + Chướng Ngại Vật)
        {
            _gridRows = 8;
            _gridCols = 10;
            _cellWidth = 0.95f;
            _cellHeight = 0.90f;
        }

        if (_previewLine != null)
        {
            _previewLine.startWidth = Mathf.Min(_cellWidth, _cellHeight) * 0.33f;
            _previewLine.endWidth = _previewLine.startWidth;
        }
        if (_previewCoreStart != null)
        {
            _previewCoreStart.startWidth = Mathf.Min(_cellWidth, _cellHeight) * 0.17f;
            _previewCoreStart.endWidth = _previewCoreStart.startWidth;
        }
        if (_previewBootStart != null)
        {
            _previewBootStart.startWidth = Mathf.Min(_cellWidth, _cellHeight) * 0.38f;
            _previewBootStart.endWidth = _previewBootStart.startWidth;
        }
    }

    private Vector2 GetCellWorldPosition(int r, int c)
    {
        float startX = -(_gridCols - 1) * _cellWidth * 0.5f;
        float startY = (_gridRows - 1) * _cellHeight * 0.5f + 0.5f;
        return new Vector2(startX + c * _cellWidth, startY - r * _cellHeight);
    }

    private Vector2Int GetCellFromWorldPosition(Vector3 worldPos)
    {
        float startX = -(_gridCols - 1) * _cellWidth * 0.5f;
        float startY = (_gridRows - 1) * _cellHeight * 0.5f + 0.5f;

        int col = Mathf.RoundToInt((worldPos.x - startX) / _cellWidth);
        int row = Mathf.RoundToInt((startY - worldPos.y) / _cellHeight);

        return new Vector2Int(row, col);
    }

    private bool IsCellValid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _gridRows && cell.y >= 0 && cell.y < _gridCols;
    }

    private DemoTerminalElement GetTerminalAtCell(Vector2Int cell)
    {
        foreach (var t in _terminals)
        {
            if (t != null && t.data != null && t.data.GridCell == cell)
            {
                return t;
            }
        }
        return null;
    }

    private RewiringObstacle GetObstacleAtCell(Vector2Int cell)
    {
        foreach (var obs in _activeObstacles)
        {
            if (obs != null && obs.GridCell == cell) return obs;
        }
        return null;
    }

    private RewiringBridge GetBridgeAtCell(Vector2Int cell)
    {
        foreach (var br in _activeBridges)
        {
            if (br != null && br.GridCell == cell) return br;
        }
        return null;
    }

    private Texture2D GetWireTexture(WireColor c)
    {
#if UNITY_EDITOR
        string fileName = "";
        switch (c)
        {
            case WireColor.Red: fileName = "red-wire-solid.jpg"; break;
            case WireColor.Green: fileName = "green-wire.jpg"; break;
            case WireColor.Blue: fileName = "blue-wire.jpg"; break;
            case WireColor.Yellow: fileName = "yellow-wire.jpg"; break;
            case WireColor.Orange: fileName = "orange-wire.jpg"; break;
            case WireColor.Purple: fileName = "purple-wire.jpg"; break;
            case WireColor.Brown: fileName = "brown-wire.jpg"; break;
            default: return null;
        }
        string fullPath = "Assets/_Project/Art/Models/rewiring-asset/" + fileName;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
#else
        return null;
#endif
    }

    private Texture2D GetConnectorTexture(bool isRightSide)
    {
#if UNITY_EDITOR
        string fileName = isRightSide ? "dau-noi-1.png" : "dau-noi-2.png";
        string fullPath = "Assets/_Project/Art/Models/rewiring-asset/" + fileName;
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
#else
        return null;
#endif
    }

    private Texture2D ColorizeConnectorTexture(Texture2D sourceTex, bool isRightSide, WireColor wireColor)
    {
        if (sourceTex == null) return null;
        Color tint = GetColorValue(wireColor);

        RenderTexture tmp = RenderTexture.GetTemporary(sourceTex.width, sourceTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(sourceTex, tmp);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D result = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        result.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        Color[] pixels = result.GetPixels();
        int w = result.width;
        int h = result.height;

        // Vùng kim loại thừa cần tô màu:
        // isRightSide = true (dau-noi-noi-o-ben-phai.png): phần kim loại thừa nằm ở rìa trái (x từ 0 đến ~28% width)
        // isRightSide = false (dau-noi-noi-o-ben-trai.png): phần kim loại thừa nằm ở rìa phải (x từ ~72% đến 100% width)
        int xMin = isRightSide ? 0 : Mathf.RoundToInt(w * 0.72f);
        int xMax = isRightSide ? Mathf.RoundToInt(w * 0.28f) : w;

        for (int y = 0; y < h; y++)
        {
            for (int x = xMin; x < xMax; x++)
            {
                int idx = y * w + x;
                Color p = pixels[idx];
                if (p.a > 0.1f)
                {
                    float lum = p.r * 0.3f + p.g * 0.59f + p.b * 0.11f;
                    Color colored = new Color(
                        Mathf.Clamp01(tint.r * (lum + 0.35f)),
                        Mathf.Clamp01(tint.g * (lum + 0.35f)),
                        Mathf.Clamp01(tint.b * (lum + 0.35f)),
                        p.a
                    );
                    pixels[idx] = Color.Lerp(p, colored, 0.9f);
                }
            }
        }

        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    private void SetupGridVisuals()
    {
        Transform existingGrid = transform.Find("Grid_Visual_Board");
        if (existingGrid != null) DestroyHelper(existingGrid.gameObject);
        if (_gridVisualHolder != null) DestroyHelper(_gridVisualHolder);
        _gridVisualHolder = new GameObject("Grid_Visual_Board");
        _gridVisualHolder.transform.SetParent(this.transform);

        float startX = -(_gridCols - 1) * _cellWidth * 0.5f;
        float startY = (_gridRows - 1) * _cellHeight * 0.5f + 0.5f;
        float halfW = _cellWidth * 0.5f;
        float halfH = _cellHeight * 0.5f;

        float minX = startX - halfW;
        float maxX = startX + (_gridCols - 1) * _cellWidth + halfW;
        float maxY = startY + halfH;
        float minY = startY - (_gridRows - 1) * _cellHeight - halfH;

        // 1. Tạo nền kim loại (Metallic background plate) bên dưới lưới (z = 0.8f)
        GameObject bgObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bgObj.name = "MetallicBackground";
        bgObj.transform.SetParent(_gridVisualHolder.transform);
        bgObj.transform.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0.8f);
        bgObj.transform.localScale = new Vector3(maxX - minX, maxY - minY, 1f);

        Collider bgCol = bgObj.GetComponent<Collider>();
        if (bgCol != null) DestroyImmediate(bgCol);

        MeshRenderer bgMr = bgObj.GetComponent<MeshRenderer>();
#if UNITY_EDITOR
        Texture2D metalTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Models/rewiring-asset/tamkimloai.jpg");
        if (metalTex != null)
        {
            bgMr.material = new Material(Shader.Find("Unlit/Texture"));
            bgMr.material.mainTexture = metalTex;
        }
        else
        {
            bgMr.material = new Material(Shader.Find("Sprites/Default"));
            bgMr.material.color = new Color(0.28f, 0.3f, 0.34f, 1f);
        }
#else
        bgMr.material = new Material(Shader.Find("Sprites/Default"));
        bgMr.material.color = new Color(0.28f, 0.3f, 0.34f, 1f);
#endif

        // 2. Kẻ đường rãnh chia ô kim loại (Dark Metallic Grooves z = 0.5f)
        for (int r = 0; r <= _gridRows; r++)
        {
            float y = maxY - r * _cellHeight;
            CreateGridLine(new Vector3(minX, y, 0.5f), new Vector3(maxX, y, 0.5f), 0.045f, new Color(0.08f, 0.1f, 0.12f, 0.95f));
        }
        for (int c = 0; c <= _gridCols; c++)
        {
            float x = minX + c * _cellWidth;
            CreateGridLine(new Vector3(x, maxY, 0.5f), new Vector3(x, minY, 0.5f), 0.045f, new Color(0.08f, 0.1f, 0.12f, 0.95f));
        }

        // 3. Khung viền kim loại ngoài cùng (Gunmetal Frame z = 0.4f)
        CreateGridLine(new Vector3(minX, maxY, 0.4f), new Vector3(maxX, maxY, 0.4f), 0.18f, new Color(0.22f, 0.25f, 0.3f, 1f));
        CreateGridLine(new Vector3(maxX, maxY, 0.4f), new Vector3(maxX, minY, 0.4f), 0.18f, new Color(0.22f, 0.25f, 0.3f, 1f));
        CreateGridLine(new Vector3(maxX, minY, 0.4f), new Vector3(minX, minY, 0.4f), 0.18f, new Color(0.22f, 0.25f, 0.3f, 1f));
        CreateGridLine(new Vector3(minX, minY, 0.4f), new Vector3(minX, maxY, 0.4f), 0.18f, new Color(0.22f, 0.25f, 0.3f, 1f));
    }

    private void CreateGridLine(Vector3 start, Vector3 end, float width, Color color)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(_gridVisualHolder.transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = color;
    }

    private void Update()
    {
        DemoTerminalElement hoveredElem = null;
        if (_mainCamera != null)
        {
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero);
            hoveredElem = hit.collider != null ? hit.collider.GetComponent<DemoTerminalElement>() : null;

            foreach (var t in _terminals)
            {
                if (t != null && t.outline != null)
                {
                    bool isHovered = (t == hoveredElem) || (t == _dragStartTerminal);
                    t.outline.enabled = isHovered;
                }
            }
        }

        if (_dragStartTerminal != null && _previewLine != null && _previewLine.enabled)
        {
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            Vector2Int mouseCell = GetCellFromWorldPosition(mousePos);
            if (_currentCellPath.Count == 0)
            {
                _currentCellPath.Add(_dragStartTerminal.data.GridCell);
                _currentDrawPoints.Add(GetCellWorldPosition(_dragStartTerminal.data.GridCell.x, _dragStartTerminal.data.GridCell.y));
            }

            Vector2Int lastCell = _currentCellPath[_currentCellPath.Count - 1];

            if (mouseCell != lastCell && IsCellValid(mouseCell))
            {
                // Kiểm tra xem có phải người chơi lùi lại (backtrack) đúng ô ngay trước đó để xóa nét không
                if (_currentCellPath.Count >= 2 && mouseCell == _currentCellPath[_currentCellPath.Count - 2])
                {
                    _currentCellPath.RemoveAt(_currentCellPath.Count - 1);
                    _currentDrawPoints.RemoveAt(_currentDrawPoints.Count - 1);
                    UpdatePreviewPositions();
                }
                else
                {
                    // Di chuyển theo ô vuông góc (TUYỆT ĐỐI KHÔNG ĐI XIÊN):
                    int stepsToTake = Mathf.Abs(mouseCell.x - lastCell.x) + Mathf.Abs(mouseCell.y - lastCell.y);
                    if (stepsToTake <= 4)
                    {
                        while (lastCell != mouseCell)
                        {
                            Vector2Int nextStep = lastCell;
                            if (Mathf.Abs(mouseCell.y - lastCell.y) >= Mathf.Abs(mouseCell.x - lastCell.x) && nextStep.y != mouseCell.y)
                            {
                                nextStep.y += (mouseCell.y > lastCell.y) ? 1 : -1;
                            }
                            else if (nextStep.x != mouseCell.x)
                            {
                                nextStep.x += (mouseCell.x > lastCell.x) ? 1 : -1;
                            }
                            else if (nextStep.y != mouseCell.y)
                            {
                                nextStep.y += (mouseCell.y > lastCell.y) ? 1 : -1;
                            }

                            if (!_currentCellPath.Contains(nextStep) && IsCellValid(nextStep))
                            {
                                if (_currentCellPath.Count == 1 && !IsStepAllowedFromStartTerminal(lastCell, nextStep))
                                {
                                    break;
                                }

                                // Kiểm tra nếu ô nextStep là cọc của màu khác -> không được phép đi vào
                                DemoTerminalElement targetElem = GetTerminalAtCell(nextStep);
                                if (targetElem != null && targetElem != _dragStartTerminal && targetElem.data != null && targetElem.data.Color != _dragStartTerminal.data.Color)
                                {
                                    break;
                                }

                                // Kiểm tra nếu ô nextStep là Chướng Ngại Vật -> chặn hoàn toàn không cho đi qua
                                if (GetObstacleAtCell(nextStep) != null)
                                {
                                    break;
                                }

                                // Kiểm tra nếu ô nextStep là Cầu Vượt -> cho phép 2 dây khác màu cắt ngang và chéo nhau an toàn
                                RewiringBridge bridge = GetBridgeAtCell(nextStep);
                                if (bridge != null)
                                {
                                    bool isHorizontalStep = (nextStep.y != lastCell.y);
                                    if (isHorizontalStep)
                                    {
                                        if (bridge.IsOccupiedHorizontal && bridge.HorizontalWireColor != _dragStartTerminal.data.Color)
                                        {
                                            CutWiresIntersectingCell(nextStep, _dragStartTerminal.data.Color);
                                        }
                                        bridge.IsOccupiedHorizontal = true;
                                        bridge.HorizontalWireColor = _dragStartTerminal.data.Color;
                                    }
                                    else
                                    {
                                        if (bridge.IsOccupiedVertical && bridge.VerticalWireColor != _dragStartTerminal.data.Color)
                                        {
                                            CutWiresIntersectingCell(nextStep, _dragStartTerminal.data.Color);
                                        }
                                        bridge.IsOccupiedVertical = true;
                                        bridge.VerticalWireColor = _dragStartTerminal.data.Color;
                                    }
                                }
                                else
                                {
                                    // Ô bình thường: Cắt đứt dây khác nếu đang chiếm giữ ô nextStep này (từ chỗ bị đâm trở xuống mất hoàn toàn)
                                    CutWiresIntersectingCell(nextStep, _dragStartTerminal.data.Color);
                                }

                                _currentCellPath.Add(nextStep);
                                _currentDrawPoints.Add(GetCellWorldPosition(nextStep.x, nextStep.y));
                                UpdatePreviewPositions();
                                lastCell = nextStep;

                                // Kiểm tra nếu ô nextStep chính là ô cọc đích cùng màu -> LẬP TỨC CHỐT NỐI VÀ DỪNG!
                                if (targetElem != null && targetElem != _dragStartTerminal && targetElem.data != null && targetElem.data.Color == _dragStartTerminal.data.Color)
                                {
                                    if (IsConnectionAllowedIntoTargetTerminal(_currentCellPath[_currentCellPath.Count - 2], targetElem))
                                    {
                                        ConnectWires(_dragStartTerminal, targetElem, new List<Vector2Int>(_currentCellPath), new List<Vector2>(_currentDrawPoints));
                                        _previewLine.enabled = false;
                                        if (_previewCoreStart != null) _previewCoreStart.enabled = false;
                                        if (_previewBootStart != null) _previewBootStart.enabled = false;
                                        _dragStartTerminal = null;
                                        _currentCellPath.Clear();
                                        _currentDrawPoints.Clear();
                                        return;
                                    }
                                    else
                                    {
                                        _currentCellPath.RemoveAt(_currentCellPath.Count - 1);
                                        _currentDrawPoints.RemoveAt(_currentDrawPoints.Count - 1);
                                        UpdatePreviewPositions();
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    private void UpdatePreviewPositions()
    {
        if (_previewLine == null) return;

        if (_currentDrawPoints.Count >= 2)
        {
            float minDim = Mathf.Min(_cellWidth, _cellHeight);
            float stripDistStart = Vector3.Distance(_currentDrawPoints[0], _currentDrawPoints[1]) * 0.5f; // Ranh giới ô có đầu nối nguồn

            Vector3 dirStart = (Vector3)(_currentDrawPoints[1] - _currentDrawPoints[0]).normalized;
            Vector3 pCoreStart0 = (Vector3)_currentDrawPoints[0] + dirStart * (minDim * 0.31f);
            Vector3 pCoreStart1 = (Vector3)_currentDrawPoints[0] + dirStart * stripDistStart;

            if (_previewCoreStart != null)
            {
                _previewCoreStart.enabled = true;
                _previewCoreStart.SetPosition(0, new Vector3(pCoreStart0.x, pCoreStart0.y, -0.22f));
                _previewCoreStart.SetPosition(1, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.22f));
            }

            if (_previewBootStart != null)
            {
                _previewBootStart.enabled = true;
                Vector3 pBoot0 = pCoreStart1 - dirStart * (minDim * 0.05f);
                Vector3 pBoot1 = pCoreStart1 + dirStart * (minDim * 0.05f);
                _previewBootStart.SetPosition(0, new Vector3(pBoot0.x, pBoot0.y, -0.19f));
                _previewBootStart.SetPosition(1, new Vector3(pBoot1.x, pBoot1.y, -0.19f));
            }

            _previewLine.positionCount = _currentDrawPoints.Count;
            _previewLine.SetPosition(0, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.16f));
            for (int i = 1; i < _currentDrawPoints.Count; i++)
            {
                _previewLine.SetPosition(i, new Vector3(_currentDrawPoints[i].x, _currentDrawPoints[i].y, -0.16f));
            }
        }
        else if (_currentDrawPoints.Count == 1)
        {
            if (_previewCoreStart != null) _previewCoreStart.enabled = false;
            if (_previewBootStart != null) _previewBootStart.enabled = false;
            _previewLine.positionCount = 1;
            _previewLine.SetPosition(0, new Vector3(_currentDrawPoints[0].x, _currentDrawPoints[0].y, -0.16f));
        }
    }

    private bool IsStepAllowedFromStartTerminal(Vector2Int fromCell, Vector2Int toCell)
    {
        if (_dragStartTerminal == null || fromCell != _dragStartTerminal.data.GridCell) return true;
        if (_dragStartTerminal.name.StartsWith("Nguồn"))
        {
            // Cọc Nguồn: mặt có màu nằm ở bên Trái -> Cấm bước đầu tiên đi sang hướng Trái (y < fromCell.y)
            if (toCell.y < fromCell.y) return false;
        }
        else if (_dragStartTerminal.name.StartsWith("Đích"))
        {
            // Cọc Đích: mặt có màu nằm ở bên Phải -> Cấm bước đầu tiên đi sang hướng Phải (y > fromCell.y)
            if (toCell.y > fromCell.y) return false;
        }
        return true;
    }

    private bool IsConnectionAllowedIntoTargetTerminal(Vector2Int fromCell, DemoTerminalElement targetElem)
    {
        if (targetElem == null || targetElem.data == null) return false;
        Vector2Int targetCell = targetElem.data.GridCell;
        if (targetElem.name.StartsWith("Nguồn"))
        {
            // Cọc Nguồn: mặt có màu bên Trái -> Cấm dây tiến vào từ hướng Trái (fromCell.y < targetCell.y)
            if (fromCell.y < targetCell.y) return false;
        }
        else if (targetElem.name.StartsWith("Đích"))
        {
            // Cọc Đích: mặt có màu bên Phải -> Cấm dây tiến vào từ hướng Phải (fromCell.y > targetCell.y)
            if (fromCell.y > targetCell.y) return false;
        }
        return true;
    }

    public void OnTerminalBeginDrag(DemoTerminalElement startElem)
    {
        PlayZapSound();
        _dragStartTerminal = startElem;
        RemoveWireOfColor(startElem.data.Color);
        _currentCellPath.Clear();
        _currentCellPath.Add(startElem.data.GridCell);
        _currentDrawPoints.Clear();
        _currentDrawPoints.Add(GetCellWorldPosition(startElem.data.GridCell.x, startElem.data.GridCell.y));

        Texture2D wireTex = GetOrCreate3DWireTexture(startElem.data.Color);
        if (wireTex != null)
        {
            _previewLine.material = new Material(Shader.Find("Unlit/Texture"));
            _previewLine.material.mainTexture = wireTex;
            _previewLine.textureMode = LineTextureMode.Tile;
        }
        else
        {
            _previewLine.material = new Material(Shader.Find("Sprites/Default"));
            _previewLine.material.color = GetColorValue(startElem.data.Color);
        }
        _previewLine.positionCount = 1;
        _previewLine.SetPosition(0, _currentDrawPoints[0]);
        _previewLine.enabled = true;
        if (_previewCoreStart != null)
        {
            _previewCoreStart.material = new Material(Shader.Find("Unlit/Texture"));
            _previewCoreStart.material.mainTexture = GetOrCreateCopperTexture();
            _previewCoreStart.enabled = false;
        }
        if (_previewBootStart != null) _previewBootStart.enabled = false;
    }

    public void OnTerminalEndDrag(DemoTerminalElement endElem)
    {
        _previewLine.enabled = false;
        if (_previewCoreStart != null) _previewCoreStart.enabled = false;
        if (_previewBootStart != null) _previewBootStart.enabled = false;
        if (_dragStartTerminal == null) return;

        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int cell = GetCellFromWorldPosition(mousePos);
        DemoTerminalElement targetElem = GetTerminalAtCell(cell);
        if (targetElem == null) targetElem = endElem;

        if (targetElem != null && targetElem != _dragStartTerminal && targetElem.data != null && targetElem.data.Color == _dragStartTerminal.data.Color)
        {
            if (_currentCellPath.Count == 1 && !IsStepAllowedFromStartTerminal(_currentCellPath[0], targetElem.data.GridCell))
            {
                _dragStartTerminal = null;
                _currentCellPath.Clear();
                _currentDrawPoints.Clear();
                return;
            }

            Vector2Int prevCell = _currentCellPath.Count >= 1 ? _currentCellPath[_currentCellPath.Count - 1] : targetElem.data.GridCell;
            if (prevCell == targetElem.data.GridCell && _currentCellPath.Count >= 2)
            {
                prevCell = _currentCellPath[_currentCellPath.Count - 2];
            }

            if (!IsConnectionAllowedIntoTargetTerminal(prevCell, targetElem))
            {
                _dragStartTerminal = null;
                _currentCellPath.Clear();
                _currentDrawPoints.Clear();
                return;
            }

            if (_currentCellPath.Count == 0 || _currentCellPath[_currentCellPath.Count - 1] != targetElem.data.GridCell)
            {
                _currentCellPath.Add(targetElem.data.GridCell);
                _currentDrawPoints.Add(GetCellWorldPosition(targetElem.data.GridCell.x, targetElem.data.GridCell.y));
            }
            ConnectWires(_dragStartTerminal, targetElem, new List<Vector2Int>(_currentCellPath), new List<Vector2>(_currentDrawPoints));
        }
        _dragStartTerminal = null;
        _currentCellPath.Clear();
        _currentDrawPoints.Clear();
    }

    private void RemoveWireOfColor(WireColor color)
    {
        for (int i = _wires.Count - 1; i >= 0; i--)
        {
            if (_wires[i] != null && _wires[i].data != null && _wires[i].data.Color == color)
            {
                _wires[i].data.StartTerminal?.DisconnectWire();
                _wires[i].data.EndTerminal?.DisconnectWire();
                _controller.RemoveWire(_wires[i].data);
                if (_wires[i].holderObj != null) DestroyHelper(_wires[i].holderObj);
                else if (_wires[i].line != null) DestroyHelper(_wires[i].line.gameObject);
                if (_wires[i].data != null) DestroyHelper(_wires[i].data.gameObject);
                _wires.RemoveAt(i);
            }
        }
        RefreshBridgeStatesFromWires();
    }

    private void CutWiresIntersectingCell(Vector2Int cell, WireColor ignoreColor)
    {
        for (int i = _wires.Count - 1; i >= 0; i--)
        {
            var wireElem = _wires[i];
            if (wireElem != null && wireElem.data != null && wireElem.data.Color != ignoreColor)
            {
                int cutIndex = wireElem.data.CellPath.IndexOf(cell);
                if (cutIndex != -1)
                {
                    bool isDestroyed = wireElem.data.TrimAtCellIndex(cutIndex);
                    if (isDestroyed)
                    {
                        _controller.RemoveWire(wireElem.data);
                        if (wireElem.holderObj != null) DestroyHelper(wireElem.holderObj);
                        else if (wireElem.line != null) DestroyHelper(wireElem.line.gameObject);
                        if (wireElem.data != null) DestroyHelper(wireElem.data.gameObject);
                        _wires.RemoveAt(i);
                    }
                    else
                    {
                        RebuildWireVisual(wireElem);
                    }
                }
            }
        }
        RefreshBridgeStatesFromWires();
    }

    private void RefreshBridgeStatesFromWires()
    {
        foreach (var b in _activeBridges)
        {
            if (b != null)
            {
                b.IsOccupiedHorizontal = false;
                b.IsOccupiedVertical = false;
            }
        }
        foreach (var w in _wires)
        {
            if (w == null || w.data == null || w.data.CellPath == null) continue;
            List<Vector2Int> path = w.data.CellPath;
            for (int i = 0; i < path.Count; i++)
            {
                RewiringBridge br = GetBridgeAtCell(path[i]);
                if (br != null)
                {
                    if (i > 0 && path[i].y != path[i - 1].y) { br.IsOccupiedHorizontal = true; br.HorizontalWireColor = w.data.Color; }
                    else if (i < path.Count - 1 && path[i + 1].y != path[i].y) { br.IsOccupiedHorizontal = true; br.HorizontalWireColor = w.data.Color; }
                    if (i > 0 && path[i].x != path[i - 1].x) { br.IsOccupiedVertical = true; br.VerticalWireColor = w.data.Color; }
                    else if (i < path.Count - 1 && path[i + 1].x != path[i].x) { br.IsOccupiedVertical = true; br.VerticalWireColor = w.data.Color; }
                }
            }
        }
    }

    private void RebuildWireVisual(DemoWireElement wireElem)
    {
        if (wireElem == null || wireElem.data == null) return;
        BuildWireHolder(wireElem, wireElem.data.CellPath, wireElem.data.PathPoints);
    }

    private void ConnectWires(DemoTerminalElement startElem, DemoTerminalElement endElem, List<Vector2Int> cellPath, List<Vector2> worldPoints)
    {
        RemoveWireOfColor(startElem.data.Color);

        GameObject wireObj = new GameObject("Wire_Logic_" + startElem.data.Color);
        wireObj.transform.SetParent(_controller.transform);
        RewiringWire logicWire = wireObj.AddComponent<RewiringWire>();
        logicWire.InitializeGrid(startElem.data, endElem.data, startElem.data.Color, cellPath, worldPoints);
        _controller.AddWire(logicWire);

        DemoWireElement wireElem = new DemoWireElement
        {
            data = logicWire,
            holderObj = null,
            line = null,
            startElem = startElem,
            endElem = endElem
        };

        BuildWireHolder(wireElem, cellPath, worldPoints);
        _wires.Add(wireElem);
        PlayZapSound();
    }

    private void BuildWireHolder(DemoWireElement wireElem, List<Vector2Int> cellPath, List<Vector2> worldPoints)
    {
        if (wireElem.holderObj != null) DestroyHelper(wireElem.holderObj);
        else if (wireElem.line != null) DestroyHelper(wireElem.line.gameObject);

        GameObject holderObj = new GameObject("Wire_Holder_" + wireElem.startElem.data.Color);
        holderObj.transform.SetParent(this.transform);
        wireElem.holderObj = holderObj;

        float minDim = Mathf.Min(_cellWidth, _cellHeight);

        if (worldPoints.Count == 0) return;

        Vector3 pCoreStart0 = worldPoints[0];
        Vector3 pCoreStart1 = worldPoints[0];
        Vector3 pCoreEnd0 = worldPoints[worldPoints.Count - 1];
        Vector3 pCoreEnd1 = worldPoints[worldPoints.Count - 1];

        bool isReachingEnd = (cellPath.Count > 0 && wireElem.endElem != null && wireElem.endElem.data != null && cellPath[cellPath.Count - 1] == wireElem.endElem.data.GridCell);

        if (worldPoints.Count >= 2)
        {
            float stripDistStart = Vector3.Distance(worldPoints[0], worldPoints[1]) * 0.5f;
            Vector3 dirStart = (Vector3)(worldPoints[1] - worldPoints[0]).normalized;
            pCoreStart0 = (Vector3)worldPoints[0] + dirStart * (minDim * 0.31f);
            pCoreStart1 = (Vector3)worldPoints[0] + dirStart * stripDistStart;

            Vector3 dirEnd = (Vector3)(worldPoints[worldPoints.Count - 2] - worldPoints[worldPoints.Count - 1]).normalized;
            if (isReachingEnd)
            {
                float stripDistEnd = Vector3.Distance(worldPoints[worldPoints.Count - 2], worldPoints[worldPoints.Count - 1]) * 0.5f;
                pCoreEnd0 = (Vector3)worldPoints[worldPoints.Count - 1] + dirEnd * (minDim * 0.31f);
                pCoreEnd1 = (Vector3)worldPoints[worldPoints.Count - 1] + dirEnd * stripDistEnd;
            }
            else
            {
                // Nếu bị cắt ngang ở giữa bảng mạch -> lộ một đoạn lõi đồng ngắn tại điểm bị cắt
                float stripDistEnd = minDim * 0.18f;
                pCoreEnd0 = (Vector3)worldPoints[worldPoints.Count - 1];
                pCoreEnd1 = (Vector3)worldPoints[worldPoints.Count - 1] + dirEnd * stripDistEnd;
            }
        }

        // 1. Lõi dây đồng ở cọc Nguồn (Start Copper Core)
        if (worldPoints.Count >= 2)
        {
            GameObject coreStartObj = new GameObject("Core_Start");
            coreStartObj.transform.SetParent(holderObj.transform);
            LineRenderer lrStart = coreStartObj.AddComponent<LineRenderer>();
            lrStart.positionCount = 2;
            lrStart.SetPosition(0, new Vector3(pCoreStart0.x, pCoreStart0.y, -0.22f));
            lrStart.SetPosition(1, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.22f));
            lrStart.startWidth = minDim * 0.17f;
            lrStart.endWidth = lrStart.startWidth;
            lrStart.numCapVertices = 12;
            lrStart.material = new Material(Shader.Find("Unlit/Texture"));
            lrStart.material.mainTexture = GetOrCreateCopperTexture();
        }

        // 2. Lõi dây đồng ở đầu ra/điểm cắt (End/Cut Copper Core)
        if (worldPoints.Count >= 2)
        {
            GameObject coreEndObj = new GameObject("Core_End");
            coreEndObj.transform.SetParent(holderObj.transform);
            LineRenderer lrEnd = coreEndObj.AddComponent<LineRenderer>();
            lrEnd.positionCount = 2;
            lrEnd.SetPosition(0, new Vector3(pCoreEnd1.x, pCoreEnd1.y, -0.22f));
            lrEnd.SetPosition(1, new Vector3(pCoreEnd0.x, pCoreEnd0.y, -0.22f));
            lrEnd.startWidth = minDim * 0.17f;
            lrEnd.endWidth = lrEnd.startWidth;
            lrEnd.numCapVertices = 12;
            lrEnd.material = new Material(Shader.Find("Unlit/Texture"));
            lrEnd.material.mainTexture = GetOrCreateCopperTexture();
        }

        // Khớp bọc cao su (Start Rubber Boot & End Rubber Boot)
        if (worldPoints.Count >= 2)
        {
            Vector3 dirStart = (Vector3)(worldPoints[1] - worldPoints[0]).normalized;
            GameObject bootStartObj = new GameObject("Boot_Start");
            bootStartObj.transform.SetParent(holderObj.transform);
            LineRenderer lrBootStart = bootStartObj.AddComponent<LineRenderer>();
            lrBootStart.positionCount = 2;
            lrBootStart.SetPosition(0, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.19f) - dirStart * (minDim * 0.05f));
            lrBootStart.SetPosition(1, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.19f) + dirStart * (minDim * 0.05f));
            lrBootStart.startWidth = minDim * 0.38f;
            lrBootStart.endWidth = lrBootStart.startWidth;
            lrBootStart.numCapVertices = 12;
            lrBootStart.material = new Material(Shader.Find("Sprites/Default"));
            lrBootStart.material.color = new Color(0.18f, 0.20f, 0.24f);

            if (isReachingEnd)
            {
                Vector3 dirEnd = (Vector3)(worldPoints[worldPoints.Count - 2] - worldPoints[worldPoints.Count - 1]).normalized;
                GameObject bootEndObj = new GameObject("Boot_End");
                bootEndObj.transform.SetParent(holderObj.transform);
                LineRenderer lrBootEnd = bootEndObj.AddComponent<LineRenderer>();
                lrBootEnd.positionCount = 2;
                lrBootEnd.SetPosition(0, new Vector3(pCoreEnd1.x, pCoreEnd1.y, -0.19f) - dirEnd * (minDim * 0.05f));
                lrBootEnd.SetPosition(1, new Vector3(pCoreEnd1.x, pCoreEnd1.y, -0.19f) + dirEnd * (minDim * 0.05f));
                lrBootEnd.startWidth = minDim * 0.38f;
                lrBootEnd.endWidth = lrBootEnd.startWidth;
                lrBootEnd.numCapVertices = 12;
                lrBootEnd.material = new Material(Shader.Find("Sprites/Default"));
                lrBootEnd.material.color = new Color(0.18f, 0.20f, 0.24f);
            }
        }

        // 3. Dây vỏ cách điện chính ở giữa (Insulated Jacket Line)
        GameObject lineObj = new GameObject("Insulated_Line_" + wireElem.startElem.data.Color);
        lineObj.transform.SetParent(holderObj.transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = worldPoints.Count;
        lr.startWidth = minDim * 0.33f;
        lr.endWidth = lr.startWidth;
        lr.numCornerVertices = 16;
        lr.numCapVertices = 16;

        Texture2D wireTex = GetOrCreate3DWireTexture(wireElem.startElem.data.Color);
        if (wireTex != null)
        {
            lr.material = new Material(Shader.Find("Unlit/Texture"));
            lr.material.mainTexture = wireTex;
            lr.textureMode = LineTextureMode.Tile;
        }
        else
        {
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.material.color = GetColorValue(wireElem.startElem.data.Color);
        }

        if (worldPoints.Count >= 2)
        {
            lr.SetPosition(0, new Vector3(pCoreStart1.x, pCoreStart1.y, -0.16f));
            for (int i = 1; i < worldPoints.Count - 1; i++)
            {
                lr.SetPosition(i, new Vector3(worldPoints[i].x, worldPoints[i].y, -0.16f));
            }
            lr.SetPosition(worldPoints.Count - 1, new Vector3(pCoreEnd1.x, pCoreEnd1.y, -0.16f));
        }
        else
        {
            lr.SetPosition(0, new Vector3(worldPoints[0].x, worldPoints[0].y, -0.16f));
        }

        wireElem.line = lr;
    }

    private Texture2D GetOrCreate3DWireTexture(WireColor c)
    {
        if (_cached3DWireTextures.TryGetValue(c, out Texture2D tex) && tex != null)
        {
            return tex;
        }

        int size = 128;
        Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
        result.wrapMode = TextureWrapMode.Repeat;
        result.filterMode = FilterMode.Bilinear;

        Color baseColor = GetColorValue(c);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (float)y / (size - 1);
            float distFromCenter = Mathf.Abs(v - 0.5f) * 2f;
            float cylZ = Mathf.Sqrt(Mathf.Max(0f, 1f - distFromCenter * distFromCenter));

            // Viền tối bo tròn 2 bên mép dây (Edge darkening & Ambient Occlusion)
            float edgeDarkening = Mathf.Clamp01((1f - distFromCenter) * 2.2f);
            float shading = Mathf.Lerp(0.35f, 1.08f, cylZ) * edgeDarkening;

            // Dải highlight rực sáng dọc sống lưng dây cáp (Specular Highlight 3D)
            float specFactor = Mathf.Max(0f, 1f - Mathf.Abs(v - 0.53f) * 8f);
            float specular = Mathf.Pow(specFactor, 3f) * 0.45f;

            for (int x = 0; x < size; x++)
            {
                float u = (float)x / (size - 1);
                // Tạo gờ rãnh nhẹ chạy dọc dây co giãn chống xoắn (Subtle Flexible Ribbing)
                float rib = 1f - 0.05f * (Mathf.Sin(u * Mathf.PI * 24f) * 0.5f + 0.5f);

                float r = Mathf.Clamp01(baseColor.r * shading * rib + specular);
                float g = Mathf.Clamp01(baseColor.g * shading * rib + specular);
                float b = Mathf.Clamp01(baseColor.b * shading * rib + specular);

                pixels[y * size + x] = new Color(r, g, b, 1f);
            }
        }

        result.SetPixels(pixels);
        result.Apply();
        _cached3DWireTextures[c] = result;
        return result;
    }

    private Texture2D GetOrCreateCopperTexture()
    {
        if (_cachedCopperTexture != null) return _cachedCopperTexture;

        int size = 64;
        Texture2D result = new Texture2D(size, size, TextureFormat.RGBA32, false);
        result.wrapMode = TextureWrapMode.Repeat;
        result.filterMode = FilterMode.Bilinear;

        Color centerCopper = new Color(0.96f, 0.65f, 0.38f);
        Color edgeCopper = new Color(0.52f, 0.28f, 0.12f);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            float v = (float)y / (size - 1);
            float dist = Mathf.Abs(v - 0.5f) * 2f;
            float cylZ = Mathf.Sqrt(Mathf.Max(0f, 1f - dist * dist));
            Color col = Color.Lerp(edgeCopper, centerCopper, cylZ);

            for (int x = 0; x < size; x++)
            {
                pixels[y * size + x] = col;
            }
        }

        result.SetPixels(pixels);
        result.Apply();
        _cachedCopperTexture = result;
        return result;
    }

    private void DestroyHelper(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    /// <summary>
    /// difficulty = 0: Dễ (24 ô - 3 Cặp)
    /// difficulty = 1: Bình thường (56 ô - 4 Cặp)
    /// difficulty = 2: Khó (84 ô - 6 Cặp)
    /// </summary>
    private void InitializeDemoBoard(int difficulty)
    {
        // 1. Dọn sạch toàn bộ các vật thể con cũ trên scene (Lưới cũ, cọc nối, cầu vượt, chướng ngại vật, dây nối)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform ch = transform.GetChild(i);
            if (ch == null) continue;
            string n = ch.name;
            if (n.StartsWith("Grid_Visual_") || n.StartsWith("Nguồn_") || n.StartsWith("Đích_") ||
                n.StartsWith("Bridge_") || n.StartsWith("Obstacle_") || n.StartsWith("Wire_") ||
                n.StartsWith("Terminal_") || n == "GridVisuals" || n.StartsWith("Preview_Bridge_") || n.StartsWith("Preview_Obstacle_"))
            {
                DestroyHelper(ch.gameObject);
            }
        }

        if (_controller == null) SetupController();
        if (_mainCamera == null) SetupCamera();
        if (_previewLine == null) SetupPreviewLine();

        if (_controller != null)
        {
            _controller.ClearAllWires();
            _controller.ClearAllTerminals();
        }
        foreach (var w in _wires)
        {
            if (w != null && w.holderObj != null) DestroyHelper(w.holderObj);
            else if (w != null && w.line != null) DestroyHelper(w.line.gameObject);
        }
        _wires.Clear();

        foreach (var br in _activeBridges)
        {
            if (br != null && br.gameObject != null) DestroyHelper(br.gameObject);
        }
        _activeBridges.Clear();

        foreach (var obs in _activeObstacles)
        {
            if (obs != null && obs.gameObject != null) DestroyHelper(obs.gameObject);
        }
        _activeObstacles.Clear();

        foreach (var t in _terminals)
        {
            if (t != null && t.gameObject != null) DestroyHelper(t.gameObject);
        }
        _terminals.Clear();
        _resultText = "";

        SetupGridBoardParameters(difficulty);
        SetupGridVisuals();

        WireColor[] allColors = { WireColor.Red, WireColor.Green, WireColor.Blue, WireColor.Yellow, WireColor.Orange, WireColor.Purple, WireColor.Brown };
        int pairs = (difficulty == 0) ? 3 : ((difficulty == 1) ? 4 : ((difficulty == 2) ? 6 : 7));

        RewiringBoardGenerator.BoardLevelData levelData = RewiringBoardGenerator.GenerateLevelData(_gridRows, _gridCols, pairs, difficulty);
        if (levelData.Pairs != null)
        {
            for (int i = 0; i < levelData.Pairs.Count && i < allColors.Length; i++)
            {
                CreateTerminalGrid(levelData.Pairs[i].StartCell, allColors[i], $"Nguồn_{allColors[i]}");
                CreateTerminalGrid(levelData.Pairs[i].EndCell, allColors[i], $"Đích_{allColors[i]}");
            }
        }

        if (levelData.Bridges != null)
        {
            foreach (var bCell in levelData.Bridges)
            {
                GameObject bridgeObj = new GameObject($"Bridge_{bCell.x}_{bCell.y}");
                bridgeObj.transform.SetParent(this.transform);
                bridgeObj.transform.position = GetCellWorldPosition(bCell.x, bCell.y);
                RewiringBridge bridgeComp = bridgeObj.AddComponent<RewiringBridge>();
                bridgeComp.Initialize(bCell);
                bridgeComp.CreateVisualModel(_cellWidth, _cellHeight);
                _activeBridges.Add(bridgeComp);
            }
        }

        if (levelData.Obstacles != null)
        {
            foreach (var oCell in levelData.Obstacles)
            {
                GameObject obsObj = new GameObject($"Obstacle_{oCell.x}_{oCell.y}");
                obsObj.transform.SetParent(this.transform);
                obsObj.transform.position = GetCellWorldPosition(oCell.x, oCell.y);
                RewiringObstacle obsComp = obsObj.AddComponent<RewiringObstacle>();
                obsComp.Initialize(oCell, RewiringObstacle.ObstacleType.BurntCapacitor);
                obsComp.CreateVisualModel(_cellWidth, _cellHeight);
                _activeObstacles.Add(obsComp);
            }
        }
    }

    private Vector2Int GetRandomFreeCell(List<Vector2Int> used)
    {
        for (int attempt = 0; attempt < 250; attempt++)
        {
            int r = Random.Range(0, _gridRows);
            int c = Random.Range(0, _gridCols);
            Vector2Int cell = new Vector2Int(r, c);
            if (!used.Contains(cell))
            {
                used.Add(cell);
                return cell;
            }
        }
        return new Vector2Int(0, 0);
    }

    private DemoTerminalElement CreateTerminalGrid(Vector2Int gridCell, WireColor color, string name)
    {
        Vector2 worldPos = GetCellWorldPosition(gridCell.x, gridCell.y);
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(this.transform);
        obj.transform.position = worldPos;

        float cylScale = Mathf.Min(_cellWidth, _cellHeight) * 0.62f;

        CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
        col.radius = cylScale * 0.55f;

        GameObject outlineObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        outlineObj.name = "Outline";
        outlineObj.transform.SetParent(obj.transform, false);
        outlineObj.transform.localPosition = new Vector3(0, 0, 0.05f);
        outlineObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        outlineObj.transform.localScale = new Vector3(cylScale * 1.18f, 0.14f, cylScale * 1.18f);

        Collider outlineCol = outlineObj.GetComponent<Collider>();
        if (outlineCol != null) DestroyImmediate(outlineCol);

        MeshRenderer outlineMr = outlineObj.GetComponent<MeshRenderer>();
        if (outlineMr != null)
        {
            outlineMr.material = new Material(Shader.Find("Sprites/Default"));
            outlineMr.material.color = Color.white;
            outlineMr.enabled = false;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "Visual";
        visual.transform.SetParent(obj.transform, false);
        visual.transform.localPosition = new Vector3(0, 0, -0.15f);
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(cylScale * 1.05f, cylScale * 1.05f, 1f);

        Collider col3d = visual.GetComponent<Collider>();
        if (col3d != null) DestroyImmediate(col3d);

        MeshRenderer mr = visual.GetComponent<MeshRenderer>();
        bool isRightSide = name.StartsWith("Nguồn");
        Texture2D connTex = GetConnectorTexture(isRightSide);
        Texture2D coloredTex = ColorizeConnectorTexture(connTex, isRightSide, color);
        if (coloredTex != null)
        {
            mr.material = new Material(Shader.Find("Unlit/Transparent"));
            mr.material.mainTexture = coloredTex;
        }
        else if (connTex != null)
        {
            mr.material = new Material(Shader.Find("Unlit/Transparent"));
            mr.material.mainTexture = connTex;
        }
        else
        {
            mr.material = new Material(Shader.Find("Sprites/Default"));
            mr.material.color = Color.white;
        }

        RewiringTerminal data = obj.AddComponent<RewiringTerminal>();
        data.InitializeGrid(color, gridCell);
        _controller.RegisterTerminal(data);

        DemoTerminalElement elem = obj.AddComponent<DemoTerminalElement>();
        elem.data = data;
        elem.manager = this;
        elem.outline = outlineMr;

        _terminals.Add(elem);
        return elem;
    }

    private void OnGUI()
    {
        InitGUIStyles();

        // Top Banner (Ngắn gọn và súc tích)
        GUI.Box(new Rect(0, 0, Screen.width, 56), "", _titleStyle);
        GUI.Label(new Rect(0, 8, Screen.width, 40), "Minigame nối dây", _titleStyle);

        // Nút Hướng dẫn chơi ở góc phải Top Banner
        if (GUI.Button(new Rect(Screen.width - 235, 8, 220, 40), "❓ HƯỚNG DẪN CHƠI", _buttonStyle))
        {
            _showGuide = !_showGuide;
        }

        // Bottom Buttons
        float btnWidth = 270;
        float btnHeight = 54;
        float bottomY = Screen.height - 72f;
        float centerX = Screen.width / 2f;

        if (GUI.Button(new Rect(centerX - 350, bottomY - 68, 180, btnHeight), "↩ UNDO (Lùi)", _buttonStyle))
        {
            OnUndoClicked();
        }

        if (GUI.Button(new Rect(centerX - 155, bottomY - 68, 310, btnHeight), "★ KIỂM TRA CHẤT LƯỢNG ★", _buttonStyle))
        {
            OnCheckQualityClicked();
        }

        if (GUI.Button(new Rect(centerX + 170, bottomY - 68, 180, btnHeight), "🗑 RESET (Xóa)", _buttonStyle))
        {
            OnResetClicked();
        }

        float w4 = Mathf.Min(220f, (Screen.width - 60f) / 4f);
        float startX = centerX - w4 * 2f - 15f;
        if (GUI.Button(new Rect(startX, bottomY, w4, 48), "1. Dễ (30 Ô - 3 Cặp)", _buttonStyle))
        {
            InitializeDemoBoard(0);
        }

        if (GUI.Button(new Rect(startX + w4 + 10f, bottomY, w4, 48), "2. Vừa (48 Ô - 4 Cặp)", _buttonStyle))
        {
            InitializeDemoBoard(1);
        }

        if (GUI.Button(new Rect(startX + (w4 + 10f) * 2f, bottomY, w4, 48), "3. Khó (70 Ô - 6 Cặp)", _buttonStyle))
        {
            InitializeDemoBoard(2);
        }

        if (GUI.Button(new Rect(startX + (w4 + 10f) * 3f, bottomY, w4, 48), "4. Khổ Hạnh (Extreme)", _buttonStyle))
        {
            InitializeDemoBoard(3);
        }

        // Result Popup (đặt phía trên cụm nút bấm bottomY - 68, nằm giữa bảng mạch và 3 nút action)
        if (!string.IsNullOrEmpty(_resultText))
        {
            GUI.Box(new Rect(Screen.width * 0.12f, bottomY - 144, Screen.width * 0.76f, 66), _resultText, _resultStyle);
        }

        // Hiển thị Cửa sổ Hướng dẫn chơi (Tutorial Modal) trên cùng nếu đang bật
        if (_showGuide)
        {
            DrawGuideModal();
        }
    }

    private void OnUndoClicked()
    {
        if (_wires.Count > 0)
        {
            DemoWireElement lastElement = _wires[_wires.Count - 1];
            if (lastElement != null)
            {
                if (lastElement.data != null && lastElement.data.gameObject != null)
                {
                    _controller.RemoveWire(lastElement.data);
                    DestroyHelper(lastElement.data.gameObject);
                }
                if (lastElement.holderObj != null)
                {
                    DestroyHelper(lastElement.holderObj);
                }
                else if (lastElement.line != null)
                {
                    DestroyHelper(lastElement.line.gameObject);
                }
                if (lastElement.startElem != null && lastElement.startElem.data != null)
                {
                    lastElement.startElem.data.DisconnectWire();
                }
                if (lastElement.endElem != null && lastElement.endElem.data != null)
                {
                    lastElement.endElem.data.DisconnectWire();
                }
            }
            _wires.RemoveAt(_wires.Count - 1);
            _resultText = "";
        }
    }

    private void OnResetClicked()
    {
        _controller.ClearAllWires();
        foreach (var w in _wires)
        {
            if (w != null && w.holderObj != null) DestroyHelper(w.holderObj);
            else if (w != null && w.line != null) DestroyHelper(w.line.gameObject);
        }
        _wires.Clear();
        _resultText = "";
    }

    private void OnCheckQualityClicked()
    {
        RepairQuality quality = _controller.EvaluateRewiringQuality();
        int intersections = _controller.CountTotalIntersections();

        if (quality == RepairQuality.Broken)
        {
            int connectedCount = _wires.Count;
            int totalPairs = _terminals.Count / 2;
            _resultStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
            _resultText = $"<color=#FF5555>⚠ CHƯA NỐI ĐỦ DÂY ({connectedCount}/{totalPairs}) - HÃY NỐI HẾT CÁC CẶP MÀU! ⚠</color>";
        }
        else if (quality == RepairQuality.Passable)
        {
            _resultStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            _resultText = $"<color=#FFC800>⚠ NỐI ẨU (PASSABLE) - RỦI RO CHẬP MẠCH! ⚠</color>\n" +
                          $"Đã nối đủ dây nhưng có {intersections} điểm chồng chập. Vui lòng sửa lại hoặc sẽ bị trừ tiền.";
        }
        else if (quality == RepairQuality.Perfect)
        {
            _resultStyle.normal.textColor = new Color(0.2f, 1f, 0.4f);
            _resultText = "<color=#00FF88>★ HOÀN HẢO (PERFECT) - THỢ CÓ TÂM! ★</color>\n" +
                          "Đã nối thông toàn bộ mạch điện vuông vức theo từng ô, 0 ô bị đè hay chéo dây!";
            PlaySuccessSound();
        }
    }

    private AudioSource _localZapSource;

    private void PlayZapSound()
    {
        AudioClip clipToPlay = zapAudioClip;
#if UNITY_EDITOR
        if (clipToPlay == null)
        {
            clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Tiếng vô điện.wav");
        }
#endif
        if (AudioManager.Instance != null && Application.isPlaying)
        {
            if (clipToPlay != null)
                AudioManager.Instance.PlaySFX(clipToPlay, 0.45f, 1.2f, true);
            else
                AudioManager.Instance.PlaySFX("Tiếng vô điện", 0.45f, 1.2f, true);
            return;
        }

        if (clipToPlay != null)
        {
            if (Application.isPlaying)
            {
                if (_localZapSource == null)
                {
                    _localZapSource = gameObject.GetComponent<AudioSource>();
                    if (_localZapSource == null) _localZapSource = gameObject.AddComponent<AudioSource>();
                    _localZapSource.playOnAwake = false;
                    _localZapSource.loop = false;
                }
                if (_localZapSource.isPlaying) _localZapSource.Stop();
                _localZapSource.pitch = 1.2f;
                _localZapSource.PlayOneShot(clipToPlay, 0.45f);
            }
#if UNITY_EDITOR
            else
            {
                PlayClipInEditor(clipToPlay);
            }
#endif
        }
    }

    private void PlaySuccessSound()
    {
        AudioClip clipToPlay = successAudioClip;
#if UNITY_EDITOR
        if (clipToPlay == null)
        {
            clipToPlay = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Tiếng báo hiệu-chính xác.mp3");
        }
#endif
        if (AudioManager.Instance != null && Application.isPlaying)
        {
            if (clipToPlay != null)
                AudioManager.Instance.PlaySFX(clipToPlay, 0.9f, 1.0f, false, 0.32f);
            else
                AudioManager.Instance.PlaySFX("Tiếng báo hiệu-chính xác", 0.9f, 1.0f, false, 0.32f);
            return;
        }

        if (clipToPlay != null)
        {
            if (Application.isPlaying)
            {
                if (_localZapSource == null)
                {
                    _localZapSource = gameObject.GetComponent<AudioSource>();
                    if (_localZapSource == null) _localZapSource = gameObject.AddComponent<AudioSource>();
                    _localZapSource.playOnAwake = false;
                    _localZapSource.loop = false;
                }
                if (_localZapSource.isPlaying) _localZapSource.Stop();
                _localZapSource.pitch = 1.0f;
                _localZapSource.clip = clipToPlay;
                _localZapSource.volume = 0.9f;
                _localZapSource.time = 0.32f;
                _localZapSource.Play();
            }
#if UNITY_EDITOR
            else
            {
                PlayClipInEditor(clipToPlay, 14112);
            }
#endif
        }
    }

#if UNITY_EDITOR
    private static void PlayClipInEditor(AudioClip clip, int startSample = 0)
    {
        try
        {
            System.Reflection.Assembly unityEditorAssembly = typeof(UnityEditor.AudioImporter).Assembly;
            System.Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
            System.Reflection.MethodInfo method = audioUtilClass.GetMethod("PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null);
            if (method != null)
            {
                method.Invoke(null, new object[] { clip, startSample, false });
            }
        }
        catch { }
    }
#endif

    private void InitGUIStyles()
    {
        if (_titleStyle != null) return;

        _titleStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _titleStyle.normal.textColor = new Color(0.2f, 1f, 0.6f, 1f);

        _guideStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _guideStyle.normal.textColor = new Color(1f, 0.92f, 0.5f, 1f);

        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };

        _resultStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            wordWrap = true
        };

        _popupWindowStyle = new GUIStyle(GUI.skin.box);
        _popupWindowStyle.normal.background = MakeSolidTexture(2, 2, new Color(0.06f, 0.09f, 0.12f, 0.97f));
        _popupWindowStyle.border = new RectOffset(2, 2, 2, 2);

        _guideTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 35,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            wordWrap = true
        };
        _guideTitleStyle.normal.textColor = new Color(0.3f, 1f, 0.7f, 1f);

        _guideBodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft,
            richText = true,
            wordWrap = true
        };
        _guideBodyStyle.normal.textColor = new Color(0.95f, 0.98f, 1f, 1f);

        _guideCloseBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _guideCloseBtnStyle.normal.textColor = new Color(1f, 0.95f, 0.3f, 1f);

        _guideIllustrationBoxStyle = new GUIStyle(GUI.skin.box);
        _guideIllustrationBoxStyle.normal.background = MakeSolidTexture(2, 2, new Color(0.12f, 0.16f, 0.22f, 0.95f));
        _guideIllustrationBoxStyle.border = new RectOffset(2, 2, 2, 2);

        _guideArrowBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 46,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        _guideArrowBtnStyle.normal.textColor = new Color(0.3f, 1f, 0.7f, 1f);

        _guidePageIndicatorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
    }

    private Texture2D MakeSolidTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawGuideModal()
    {
        // Nền tối mờ bao phủ toàn màn hình
        GUI.color = new Color(0, 0, 0, 0.85f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float popupW = Mathf.Min(1650f, Screen.width * 0.90f);
        float popupH = Mathf.Min(980f, Screen.height * 0.92f);
        float popupX = (Screen.width - popupW) / 2f;
        float popupY = (Screen.height - popupH) / 2f;

        GUI.Box(new Rect(popupX, popupY, popupW, popupH), "", _popupWindowStyle);

        // Nút mũi tên chuyển trang (Side Arrow navigation like sketch)
        float arrowW = 90f;
        float arrowH = 180f;
        float arrowY = popupY + (popupH - arrowH) / 2f - 40f;

        if (_guidePageIndex > 0)
        {
            if (GUI.Button(new Rect(popupX - arrowW - 15f, arrowY, arrowW, arrowH), "◀", _guideArrowBtnStyle))
            {
                _guidePageIndex--;
            }
        }

        if (_guidePageIndex < 3)
        {
            if (GUI.Button(new Rect(popupX + popupW + 15f, arrowY, arrowW, arrowH), "▶", _guideArrowBtnStyle))
            {
                _guidePageIndex++;
            }
        }

        // Tiêu đề Trang (Header)
        string pageTitle = "";
        if (_guidePageIndex == 0) pageTitle = "Mục tiêu & thao tác cơ bản";
        else if (_guidePageIndex == 1) pageTitle = "BƯỚC 2/4 : ĐÁNH GIÁ CHẤT LƯỢNG (HOÀN HẢO VS NỐI ẨU)";
        else if (_guidePageIndex == 2) pageTitle = "BƯỚC 3/4 : CẦU VƯỢT GỐM (BRIDGE) - KHUNG CHỮ THẬP";
        else if (_guidePageIndex == 3) pageTitle = "BƯỚC 4/4 : CHƯỚNG NGẠI VẬT & CÔNG CỤ HỖ TRỢ";

        GUI.Label(new Rect(popupX + 25f, popupY + 18f, popupW - 50f, 95f), $"📖 HƯỚNG DẪN CHƠI - {pageTitle}", _guideTitleStyle);

        // Khung "Ảnh minh họa" (Top Illustration Box like sketch)
        float illusX = popupX + 50f;
        float illusY = popupY + 118f;
        float illusW = popupW - 100f;
        float illusH = popupH * 0.42f;
        DrawSlideIllustration(_guidePageIndex, new Rect(illusX, illusY, illusW, illusH));

        // Nội dung mô tả ngắn gọn bên dưới ảnh minh họa (Bottom Text lines)
        float descY = illusY + illusH + 20f;
        float descH = popupH - (descY - popupY) - 150f;
        
        string descText = "";
        if (_guidePageIndex == 0)
        {
            descText = "<color=#00FF88><b>• Mục tiêu:</b></color> Sửa chữa thiết bị điện bằng cách nối các cọc nguồn có cùng màu với nhau.\n" +
                       "<color=#00FF88><b>• Thao tác:</b></color> Nhấn và giữ nút trái chuột từ cọc nguồn đã chọn rồi kéo theo các ô lưới trên bảng mạch để vẽ đường dây tới cọc cùng màu còn lại.";
        }
        else if (_guidePageIndex == 1)
        {
            descText = "<color=#00FF88><b>★ Hoàn hảo (Perfect):</b></color> Các sợi dây đi vuông góc theo ô, <color=#FF8888><b>tuyệt đối không bị chồng đè cắt chéo nhau</b></color>.\n" +
                       "<color=#FFC800><b>⚠ Nối ẩu (Passable):</b></color> Nếu bạn vẽ hai sợi dây cắt ngang qua nhau ở ô thường, mạch vẫn chạy nhưng sẽ bị trừ điểm/tiền thưởng.\n" +
                       "<color=#FF5555><b>❌ Chưa hoàn thành (Broken):</b></color> Vẫn còn cặp màu chưa được nối thông.";
        }
        else if (_guidePageIndex == 2)
        {
            descText = "<color=#FFDF55><b>🌁 Cầu vượt gốm (Khung chữ thập):</b></color> Là điểm giao cắt cách điện đặc biệt được đặt trên lưới!\n" +
                       "• Bạn có thể cho <color=#FFDF55><b>2 sợi dây (Ngang & Dọc) đi vuông góc xuyên qua nhau</b></color> tại đây mà <color=#00FF88>KHÔNG BỊ PHẠT CHÉO DÂY</color>!\n" +
                       "• Ở độ khó Khó & Khổ Hạnh, tận dụng Cầu vượt là chìa khóa duy nhất để đạt đánh giá Perfect.";
        }
        else if (_guidePageIndex == 3)
        {
            descText = "<color=#FF5555><b>💥 Tụ điện cháy (Obstacle - Ô đen có tia lửa):</b></color> Là vùng nguy hiểm, <color=#FF5555><b>tuyệt đối không thể nối dây xuyên qua</b></color>, bắt buộc phải vẽ đường vòng tránh.\n" +
                       "• Dùng nút <b>[↩ UNDO]</b> (Lùi 1 bước) hoặc <b>[🗑 RESET]</b> (Xóa làm lại) khi gặp thế cờ khó trước khi kiểm tra chất lượng.";
        }

        GUI.Label(new Rect(illusX, descY, illusW, descH), descText, _guideBodyStyle);

        // Dòng chỉ báo trang (Dots / Page numbers right above bottom button)
        string dots = "";
        for (int i = 0; i < 4; i++)
        {
            if (i == _guidePageIndex) dots += " <color=#00FF88><b>[ ● Trang " + (i + 1) + " ]</b></color> ";
            else dots += " <color=#888888>○ Trang " + (i + 1) + "</color> ";
        }
        GUI.Label(new Rect(popupX, popupY + popupH - 152f, popupW, 48f), dots, _guidePageIndicatorStyle);

        // Nút THOÁT / XÁC NHẬN siêu rộng bên dưới cùng (Bottom Exit Button like sketch)
        float btnW = Mathf.Min(680f, popupW - 140f);
        float btnH = 68f;
        float btnX = popupX + (popupW - btnW) / 2f;
        float btnY = popupY + popupH - 94f;

        string btnText = _guidePageIndex < 3 ? "✔ ĐÃ HIỂU (THOÁT)" : "✔ ĐÃ HIỂU & BẮT ĐẦU CHƠI";
        if (GUI.Button(new Rect(btnX, btnY, btnW, btnH), btnText, _guideCloseBtnStyle))
        {
            _showGuide = false;
        }
    }

    private void DrawSlideIllustration(int pageIndex, Rect rect)
    {
        GUI.Box(rect, "", _guideIllustrationBoxStyle);

        Rect innerRect = new Rect(rect.x + 20f, rect.y + 15f, rect.width - 40f, rect.height - 30f);

        if (pageIndex == 0)
        {
#if UNITY_EDITOR
            if (_guideStep1Texture == null)
            {
                _guideStep1Texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/UI/Sprites/Guide_Step1.png");
            }
#endif
            if (_guideStep1Texture != null)
            {
                float pad = 12f;
                Rect drawRect = new Rect(rect.x + pad, rect.y + pad, rect.width - pad * 2f, rect.height - pad * 2f);
                float texAspect = (float)_guideStep1Texture.width / _guideStep1Texture.height;
                float boxAspect = drawRect.width / drawRect.height;
                if (texAspect > boxAspect)
                {
                    float h = drawRect.width / texAspect;
                    drawRect.y += (drawRect.height - h) / 2f;
                    drawRect.height = h;
                }
                else
                {
                    float w = drawRect.height * texAspect;
                    drawRect.x += (drawRect.width - w) / 2f;
                    drawRect.width = w;
                }
                GUI.DrawTexture(drawRect, _guideStep1Texture, ScaleMode.ScaleToFit);
            }
            else
            {
                string illus = "<color=#55B8FF><b>[ ■ CỌC NGUỒN (Xanh Dương) ]</b></color>   ━━━━━( 👆 Nhấn giữ & Kéo theo ô lưới )━━━━━>   <color=#55B8FF><b>[ ■ CỌC ĐÍCH (Xanh Dương) ]</b></color>\n\n" +
                               "<color=#00FF88><b>[ ■ CỌC NGUỒN (Xanh Lá) ]   </b></color>   ━━━━━( 👆 Nhấn giữ & Kéo theo ô lưới )━━━━━>   <color=#00FF88><b>[ ■ CỌC ĐÍCH (Xanh Lá) ]   </b></color>";
                GUI.Label(innerRect, illus, _guideTitleStyle);
            }
        }
        else if (pageIndex == 1)
        {
            // Minh họa Perfect vs Passable
            float halfW = rect.width / 2f;
            string leftText = "<color=#00FF88><b>★ HOÀN HẢO (PERFECT) ★</b></color>\n\n" +
                              "<b>[■]</b> ━━━━━ <b>[■]</b> (Dây Đỏ)\n" +
                              "<b>[■]</b> ━━━━━ <b>[■]</b> (Dây Xanh)\n\n" +
                              "<color=#00FF88>✔ 0 ô chồng đè chéo nhau\n✔ Nhận 100% Tiền Thưởng</color>";

            string rightText = "<color=#FFC800><b>⚠ NỐI ẨU (PASSABLE) ⚠</b></color>\n\n" +
                               "<b>[■]</b> ━━━<color=#FF5555><b>[ X ]</b></color>━━━ <b>[■]</b> (Đè chéo!)\n\n\n" +
                               "<color=#FFC800>⚠ Dây Đỏ & Xanh cắt ngang ô thường\n⚠ Bị trừ điểm & tiền thưởng</color>";

            GUI.Label(new Rect(rect.x, rect.y + 20f, halfW, rect.height - 25f), leftText, _guideBodyStyle);
            GUI.Label(new Rect(rect.x + halfW, rect.y + 20f, halfW, rect.height - 25f), rightText, _guideBodyStyle);
        }
        else if (pageIndex == 2)
        {
            // Minh họa Cầu vượt gốm
            string illus = "<color=#FFDF55><b>🌁 [ + CẦU VƯỢT GỐM CÁCH ĐIỆN + ] 🌁</b></color>\n\n" +
                           "Dây Ngang (<color=#FF8888>━━━</color>) & Dây Dọc (<color=#55B8FF>┃</color>) đi vuông góc qua nhau tại ô Cầu vượt\n\n" +
                           "<color=#00FF88><b>★★★ ĐẶC BIỆT: KHÔNG BỊ TÍNH LÀ CHÉO DÂY - BẮT BUỘC DÙNG Ở ĐỘ KHÓ CAO ★★★</b></color>";
            GUI.Label(innerRect, illus, _guideTitleStyle);
        }
        else if (pageIndex == 3)
        {
            // Minh họa Chướng ngại vật
            string illus = "<color=#FF5555><b>💥 [ X TỤ ĐIỆN CHÁY - VÙNG CÁCH LY NGUY HIỂM X ] 💥</b></color>\n\n" +
                           "Dây điện (<color=#00FF88>━━━</color>) buộc phải bẻ góc vẽ vòng qua các ô bên cạnh để tránh Tụ cháy!\n\n" +
                           "<color=#AAAAAA>Công cụ hỗ trợ: <b>[↩ UNDO]</b> (Lùi bước) | <b>[🗑 RESET]</b> (Xóa làm lại) | <b>[★ KIỂM TRA CHẤT LƯỢNG ★]</b></color>";
            GUI.Label(innerRect, illus, _guideTitleStyle);
        }
    }

    private Color GetColorValue(WireColor c)
    {
        switch (c)
        {
            case WireColor.Red: return new Color(0.95f, 0.2f, 0.2f);
            case WireColor.Green: return new Color(0.2f, 0.85f, 0.35f);
            case WireColor.Blue: return new Color(0.2f, 0.55f, 0.95f);
            case WireColor.Yellow: return new Color(0.95f, 0.85f, 0.2f);
            case WireColor.Orange: return new Color(0.98f, 0.55f, 0.1f);
            case WireColor.Purple: return new Color(0.7f, 0.3f, 0.9f);
            case WireColor.Brown: return new Color(0.6f, 0.35f, 0.2f);
            case WireColor.White: return new Color(0.95f, 0.95f, 0.95f);
            case WireColor.Black: return new Color(0.15f, 0.15f, 0.15f);
            default: return Color.white;
        }
    }

    [ContextMenu("Spawn Bridge & Obstacle Previews in Scene")]
    public void SpawnBridgeAndObstaclePreviews()
    {
        GameObject root = GameObject.Find("--- REWIRING PREVIEWS ---");
        if (root == null)
        {
            root = new GameObject("--- REWIRING PREVIEWS ---");
        }

        while (root.transform.childCount > 0)
        {
            if (Application.isPlaying) Destroy(root.transform.GetChild(0).gameObject);
            else DestroyImmediate(root.transform.GetChild(0).gameObject);
        }

        float cellW = (_cellWidth > 0) ? _cellWidth : 1.0f;
        float cellH = (_cellHeight > 0) ? _cellHeight : 1.0f;
        Vector3 baseCenter = new Vector3(0, 0, 0);

        // 1. Cầu Vượt (Wire Bridge)
        GameObject bridgeObj = new GameObject("Preview_Bridge_Cell (Ceramic Insulator)");
        bridgeObj.transform.SetParent(root.transform, false);
        bridgeObj.transform.position = baseCenter + new Vector3(-cellW * 1.5f, 0, 0);
        RewiringBridge bridgeComp = bridgeObj.AddComponent<RewiringBridge>();
        bridgeComp.Initialize(new Vector2Int(1, 2));
        bridgeComp.CreateVisualModel(cellW, cellH);

        // 2. Chướng Ngại Vật - Tụ Điện Cháy (Burnt Capacitor)
        GameObject obsCapObj = new GameObject("Preview_Obstacle_Cell (Burnt Capacitor)");
        obsCapObj.transform.SetParent(root.transform, false);
        obsCapObj.transform.position = baseCenter;
        RewiringObstacle obsCapComp = obsCapObj.AddComponent<RewiringObstacle>();
        obsCapComp.Initialize(new Vector2Int(2, 2), RewiringObstacle.ObstacleType.BurntCapacitor);
        obsCapComp.CreateVisualModel(cellW, cellH);

        Debug.Log("[RewiringDemoUI] Đã tạo thành công mô hình xem trước (Bridge & Obstacle) trong Scene View!");
    }
}

