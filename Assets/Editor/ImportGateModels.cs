using UnityEngine;
using UnityEditor;
using System.IO;

public class ImportGateModels : EditorWindow
{
    [MenuItem("Tools/5. Import and Place Meshy Gates")]
    public static void ImportGates()
    {
        // Yêu cầu Unity load các file vừa copy vào
        AssetDatabase.Refresh();

        // Đường dẫn tới 2 FBX
        string leftFbxPath = "Assets/Models/Gate/GateLeftPanel/source/Meshy_AI_Gate_Left_Panel_3D_0712121142_image-to-3d-texture.fbx";
        string rightFbxPath = "Assets/Models/Gate/GateRightPanel/source/Meshy_AI_Gate_Right_Panel_3D_0712121158_image-to-3d-texture.fbx";

        GameObject leftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(leftFbxPath);
        GameObject rightPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rightFbxPath);

        if (leftPrefab == null || rightPrefab == null)
        {
            Debug.LogError("Không tìm thấy model FBX. Vui lòng kiểm tra lại đường dẫn copy!");
            return;
        }

        // Tạo Material cho cánh bên trái
        Material leftMat = new Material(Shader.Find("Standard"));
        leftMat.name = "Meshy_GateLeft_Mat";
        leftMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateLeftPanel/textures/Meshy_AI_Gate_Left_Panel_3D_0712121142_image-to-3d-texture.png");
        leftMat.EnableKeyword("_NORMALMAP");
        leftMat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateLeftPanel/textures/Meshy_AI_Gate_Left_Panel_3D_0712121142_image-to-3d-texture_normal.png"));
        leftMat.SetFloat("_Metallic", 0.5f);
        leftMat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateLeftPanel/textures/Meshy_AI_Gate_Left_Panel_3D_0712121142_image-to-3d-texture_metallic.png"));

        // Tạo Material cho cánh bên phải
        Material rightMat = new Material(Shader.Find("Standard"));
        rightMat.name = "Meshy_GateRight_Mat";
        rightMat.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateRightPanel/textures/Meshy_AI_Gate_Right_Panel_3D_0712121158_image-to-3d-texture.png");
        rightMat.EnableKeyword("_NORMALMAP");
        rightMat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateRightPanel/textures/Meshy_AI_Gate_Right_Panel_3D_0712121158_image-to-3d-texture_normal.png"));
        rightMat.SetFloat("_Metallic", 0.5f);
        rightMat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/Gate/GateRightPanel/textures/Meshy_AI_Gate_Right_Panel_3D_0712121158_image-to-3d-texture_metallic.png"));

        // Tạo folder Materials nếu chưa có
        if (!AssetDatabase.IsValidFolder("Assets/Models/Gate/Materials"))
        {
            AssetDatabase.CreateFolder("Assets/Models/Gate", "Materials");
        }

        // Lưu Material thành file để Unity nhận diện
        AssetDatabase.CreateAsset(leftMat, "Assets/Models/Gate/Materials/GateLeftMat.mat");
        AssetDatabase.CreateAsset(rightMat, "Assets/Models/Gate/Materials/GateRightMat.mat");

        // Đưa Model vào Scene
        GameObject leftObj = (GameObject)PrefabUtility.InstantiatePrefab(leftPrefab);
        GameObject rightObj = (GameObject)PrefabUtility.InstantiatePrefab(rightPrefab);

        leftObj.name = "Meshy_GateLeft";
        rightObj.name = "Meshy_GateRight";

        // Áp dụng Material
        foreach (MeshRenderer renderer in leftObj.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterial = leftMat;
        }
        foreach (MeshRenderer renderer in rightObj.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.sharedMaterial = rightMat;
        }

        // Đặt vị trí, góc xoay và tỷ lệ GIỐNG HỆT Cube.010
        Vector3 cube10Pos = new Vector3(-5.113133f, 0.6239908f, 0f);
        Quaternion cube10Rot = Quaternion.Euler(-89.98f, 0f, 0f);
        Vector3 cube10Scale = new Vector3(11.56095f, 11.56095f, 11.56095f);

        leftObj.transform.position = cube10Pos;
        leftObj.transform.rotation = cube10Rot;
        leftObj.transform.localScale = cube10Scale;

        rightObj.transform.position = cube10Pos;
        rightObj.transform.rotation = cube10Rot;
        rightObj.transform.localScale = cube10Scale;

        // Lưu ý: Hai cánh cửa được sinh ra cùng 1 vị trí, bạn có thể tự dùng công cụ Move di chuyển tụi nó ra một chút nếu bị trùng lấp
        
        Selection.objects = new Object[] { leftObj, rightObj };
        Debug.Log("Import 2 cánh cửa thành công! Material đã được setup đầy đủ.");
    }
}
