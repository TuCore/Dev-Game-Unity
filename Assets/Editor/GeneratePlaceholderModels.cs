using UnityEngine;
using UnityEditor;

public class GeneratePlaceholderModels : EditorWindow
{
    [MenuItem("Tools/4. Tạo Model Máy Lạnh & Loa (Placeholder)")]
    public static void GenerateModels()
    {
        CreateAirConditioner();
        CreateSpeaker();
        
        // Refresh lại thư mục để thấy prefab
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("==== HOÀN TẤT TẠO MODEL MÁY LẠNH & LOA ====");
    }

    private static void CreateAirConditioner()
    {
        // Tạo root
        GameObject acRoot = new GameObject("AC_Placeholder");
        
        // Thân máy lạnh (hình chữ nhật dài)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(acRoot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(1.5f, 0.4f, 0.5f);
        
        // Khe gió (Màu đen/tối)
        GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vent.name = "Vent";
        vent.transform.SetParent(acRoot.transform);
        vent.transform.localPosition = new Vector3(0, -0.1f, -0.2f);
        vent.transform.localScale = new Vector3(1.2f, 0.1f, 0.15f);

        // Gắn màu cho máy lạnh
        Renderer bodyRend = body.GetComponent<Renderer>();
        Renderer ventRend = vent.GetComponent<Renderer>();
        
        if (bodyRend != null) bodyRend.sharedMaterial = CreateColorMaterial("AC_White", Color.white);
        if (ventRend != null) ventRend.sharedMaterial = CreateColorMaterial("AC_Dark", new Color(0.1f, 0.1f, 0.1f));

        // Lưu thành Prefab
        SaveAsPrefab(acRoot, "AirConditioner_Placeholder");
    }

    private static void CreateSpeaker()
    {
        // Tạo root
        GameObject speakerRoot = new GameObject("Speaker_Placeholder");
        
        // Thùng loa (hình hộp cao chữ nhật)
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "WoodenBox";
        body.transform.SetParent(speakerRoot.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.6f, 1.2f, 0.5f);
        
        // Củ loa trên
        GameObject driverTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        driverTop.name = "DriverTop";
        driverTop.transform.SetParent(speakerRoot.transform);
        driverTop.transform.localPosition = new Vector3(0, 0.3f, -0.25f);
        driverTop.transform.localRotation = Quaternion.Euler(90, 0, 0); // Xoay ra mặt trước
        driverTop.transform.localScale = new Vector3(0.35f, 0.05f, 0.35f);

        // Củ loa dưới
        GameObject driverBottom = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        driverBottom.name = "DriverBottom";
        driverBottom.transform.SetParent(speakerRoot.transform);
        driverBottom.transform.localPosition = new Vector3(0, -0.3f, -0.25f);
        driverBottom.transform.localRotation = Quaternion.Euler(90, 0, 0);
        driverBottom.transform.localScale = new Vector3(0.45f, 0.05f, 0.45f);

        // Gắn màu cho loa
        Renderer bodyRend = body.GetComponent<Renderer>();
        Renderer topRend = driverTop.GetComponent<Renderer>();
        Renderer botRend = driverBottom.GetComponent<Renderer>();
        
        Material woodMat = CreateColorMaterial("Speaker_Wood", new Color(0.4f, 0.2f, 0.1f));
        Material blackMat = CreateColorMaterial("Speaker_Black", new Color(0.15f, 0.15f, 0.15f));

        if (bodyRend != null) bodyRend.sharedMaterial = woodMat;
        if (topRend != null) topRend.sharedMaterial = blackMat;
        if (botRend != null) botRend.sharedMaterial = blackMat;

        // Lưu thành Prefab
        SaveAsPrefab(speakerRoot, "Speaker_Placeholder");
    }

    private static Material CreateColorMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            // Đảm bảo thư mục Materials tồn tại
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
            
            mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    private static void SaveAsPrefab(GameObject obj, string name)
    {
        string path = "Assets/Prefabs/" + name + ".prefab";
        
        // Đảm bảo thư mục Prefabs tồn tại
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        
        PrefabUtility.SaveAsPrefabAsset(obj, path);
        
        // Xóa object tạm trên scene
        DestroyImmediate(obj);
        
        Debug.Log("Đã tạo: " + path);
    }
}
