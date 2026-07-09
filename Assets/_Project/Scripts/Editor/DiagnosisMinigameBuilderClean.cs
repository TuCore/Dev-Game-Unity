using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Minigames.Diagnosis.Editor
{
    public class DiagnosisMinigameBuilderClean : UnityEditor.Editor
    {
        [MenuItem("AnhThoDien/Tạo Minigame Khám Bệnh (PCB Siêu Sạch)")]
        public static void BuildCleanMinigame()
        {
            // 1. Refresh AssetDatabase và kiểm tra ảnh
            AssetDatabase.Refresh();
            string spritePath = "Assets/_Project/Sprites/DiagnosisPCB_Clean.png";
            
            TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }

            Sprite boardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);

            // 2. Tạo Canvas
            GameObject canvasObj = new GameObject("DiagnosisCanvas2D");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // 3. Tạo Bo mạch
            GameObject boardObj = new GameObject("MinigameBoard");
            boardObj.transform.SetParent(canvasObj.transform, false);
            
            RectTransform boardRect = boardObj.AddComponent<RectTransform>();
            boardRect.sizeDelta = new Vector2(1000, 1000); 
            
            Image boardImage = boardObj.AddComponent<Image>();
            boardImage.sprite = boardSprite;

            // 4. Tạo 16 điểm đo xếp thành vòng vuông quanh IC trung tâm
            // Bán kính hình vuông khoảng 220px từ tâm
            List<DiagnosisNode> allNodes = new List<DiagnosisNode>();
            // 3. Khai báo danh sách các tọa độ của các mối hàn Vàng (Gold Pads) có thật trên ảnh
            List<Vector2> allPads = new List<Vector2>()
            {
                // Vòng quanh IC trung tâm
                new Vector2(-60, 140), new Vector2(0, 140), new Vector2(60, 140),
                new Vector2(140, 60), new Vector2(140, 0), new Vector2(140, -60),
                new Vector2(60, -140), new Vector2(0, -140), new Vector2(-60, -140),
                new Vector2(-140, -60), new Vector2(-140, 0), new Vector2(-140, 60),
                
                // Hàng rào Header phía trên
                new Vector2(-200, 360), new Vector2(-150, 360), new Vector2(-100, 360), new Vector2(-50, 360),
                new Vector2(50, 360), new Vector2(100, 360), new Vector2(150, 360), new Vector2(200, 360),
                
                // Hàng rào Header phía dưới (né nút bấm Diagnosis Complete ở giữa)
                new Vector2(-250, -350), new Vector2(-200, -350), new Vector2(-150, -350), 
                new Vector2(150, -350), new Vector2(200, -350), new Vector2(250, -350),
                
                // Các pad to ở 2 bên mép viền
                new Vector2(-340, 0), new Vector2(340, 0),
                new Vector2(-340, 60), new Vector2(340, 60),
                new Vector2(-340, -60), new Vector2(340, -60),
                
                // Các cụm tụ điện/trở ở 4 góc
                new Vector2(-250, 220), new Vector2(250, 220),
                new Vector2(-250, -220), new Vector2(250, -220)
            };

            // Xáo trộn mảng để chọn ngẫu nhiên
            System.Random rng = new System.Random();
            int n = allPads.Count;
            while (n > 1) {
                n--;
                int k = rng.Next(n + 1);
                Vector2 value = allPads[k];
                allPads[k] = allPads[n];
                allPads[n] = value;
            }

            for (int i = 0; i < 8; i++)
            {
                GameObject nodeObj = new GameObject($"Node_{i + 1}");
                nodeObj.transform.SetParent(boardObj.transform, false);

                RectTransform nodeRect = nodeObj.AddComponent<RectTransform>();
                nodeRect.anchoredPosition = allPads[i];
                nodeRect.sizeDelta = new Vector2(50, 50); // Phóng to hơn một chút để dễ bấm

                Image nodeImg = nodeObj.AddComponent<Image>();
                Color highlightColor = new Color(1f, 0.85f, 0f, 0.65f); // Màu vàng đồng dạ quang 65%
                nodeImg.color = highlightColor;

                Button btn = nodeObj.AddComponent<Button>();
                DiagnosisNode diagnosisNode = nodeObj.AddComponent<DiagnosisNode>();
                diagnosisNode.NodeId = $"Node_{i + 1}";
                
                // Ép màu mặc định (unprobedColor) của Node thành màu dạ quang để không bị đè về màu trắng mờ
                SerializedObject so = new SerializedObject(diagnosisNode);
                so.FindProperty("unprobedColor").colorValue = highlightColor;
                so.ApplyModifiedProperties();
                
                allNodes.Add(diagnosisNode);
            }

            // 5. Nút Hoàn Thành nguỵ trang thành dải đồng (Pad)
            GameObject finishBtnObj = new GameObject("FinishDiagnosisButton");
            finishBtnObj.transform.SetParent(boardObj.transform, false);

            RectTransform finishRect = finishBtnObj.AddComponent<RectTransform>();
            finishRect.anchorMin = new Vector2(0.5f, 0);
            finishRect.anchorMax = new Vector2(0.5f, 0);
            finishRect.pivot = new Vector2(0.5f, 0);
            finishRect.anchoredPosition = new Vector2(0, 50); // Nằm sát mép dưới bo mạch
            finishRect.sizeDelta = new Vector2(300, 60);

            Image finishImg = finishBtnObj.AddComponent<Image>();
            finishImg.color = new Color(0.9f, 0.7f, 0.2f); // Màu vàng đồng

            Button finishBtn = finishBtnObj.AddComponent<Button>();
            finishBtnObj.AddComponent<FinishDiagnosisUIHelper>(); 

            // Text của nút (Chữ in lụa trắng)
            GameObject textObj = new GameObject("SilkscreenText");
            textObj.transform.SetParent(finishBtnObj.transform, false);
            
            Text txt = textObj.AddComponent<Text>();
            txt.text = "DIAGNOSIS COMPLETE";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 1f, 1f, 0.9f); 
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            // 6. Tạo Manager
            GameObject managerObj = new GameObject("DiagnosisManager");
            DiagnosisMinigame minigameScript = managerObj.AddComponent<DiagnosisMinigame>();
            managerObj.AddComponent<DemoMinigameRunner>();

            // Nối dây
            SerializedObject serializedManager = new SerializedObject(minigameScript);
            serializedManager.FindProperty("minigameBoardUI").objectReferenceValue = boardObj;
            
            SerializedProperty nodesProp = serializedManager.FindProperty("allNodes");
            nodesProp.ClearArray();
            for (int i = 0; i < allNodes.Count; i++)
            {
                nodesProp.InsertArrayElementAtIndex(i);
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = allNodes[i];
            }
            serializedManager.ApplyModifiedProperties();

            Debug.Log("<color=green>Đã tạo xong Giao Diện Khám Bệnh Sạch! 16 Node bao quanh IC.</color>");
            Selection.activeGameObject = canvasObj;
        }
    }
}
