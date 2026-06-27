using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DevGameUnity.Delivery
{
    public sealed class DeliveryPrototypeManager : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public Transform carryAnchor;

        [Header("Interaction")]
        public float interactDistance = 3.2f;
        public KeyCode interactKey = KeyCode.E;

        [Header("Session")]
        public int cash;
        public int packagesInBag;
        public int completedDeliveries;

        private readonly List<DeliveryPoint> points = new();
        private DeliveryDepot depot;
        private GameObject heldBox;
        private string status = "Den kho EMS de nhan ca giao hang.";
        private Canvas hudCanvas;
        private Text packagesText;
        private Text deliveriesText;
        private Text cashText;
        private Text statusText;
        private Text promptText;
        private Image progressFill;
        private GameObject promptPanel;

        private void Awake()
        {
            points.AddRange(FindObjectsByType<DeliveryPoint>(FindObjectsSortMode.None));
            depot = FindFirstObjectByType<DeliveryDepot>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            EnsureHeldBox();
            RefreshHeldBox();
            EnsureHud();
            RefreshHud();
        }

        private void Update()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (Input.GetKeyDown(interactKey))
            {
                TryInteract();
            }

            RefreshHud();
        }

        private void TryInteract()
        {
            if (playerCamera == null)
            {
                status = "Chua tim thay camera nguoi choi.";
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                status = "Lai gan kho hoac cua nha hon de tuong tac.";
                return;
            }

            var hitDepot = hit.collider.GetComponentInParent<DeliveryDepot>();
            if (hitDepot != null)
            {
                ReceivePackages(hitDepot);
                return;
            }

            var point = hit.collider.GetComponentInParent<DeliveryPoint>();
            if (point != null)
            {
                Deliver(point);
                return;
            }

            status = "Khong co gi de tuong tac o day.";
        }

        private void ReceivePackages(DeliveryDepot hitDepot)
        {
            var remaining = points.Count(p => !p.delivered);
            if (remaining <= 0)
            {
                status = "Da giao het don trong prototype.";
                return;
            }

            packagesInBag = Mathf.Min(hitDepot.packageCapacity, remaining);
            status = $"Da nhan {packagesInBag} thung hang. Tim marker mau cam de giao.";
            RefreshHeldBox();
            RefreshHud();
        }

        private void Deliver(DeliveryPoint point)
        {
            if (point.delivered)
            {
                status = $"{point.addressId} da nhan hang roi.";
                return;
            }

            if (packagesInBag <= 0)
            {
                status = "Ban chua co hang. Quay ve kho EMS nhan thung.";
                return;
            }

            point.delivered = true;
            packagesInBag--;
            completedDeliveries++;
            cash += point.reward;
            status = $"Giao thanh cong {point.addressId}. +{point.reward:n0} VND";
            SetPointVisual(point, delivered: true);
            RefreshHeldBox();
            RefreshHud();
        }

        private void EnsureHeldBox()
        {
            if (heldBox != null || playerCamera == null)
            {
                return;
            }

            if (carryAnchor == null)
            {
                var anchor = new GameObject("Package Carry Anchor");
                anchor.transform.SetParent(playerCamera.transform);
                anchor.transform.localPosition = new Vector3(0.34f, -0.34f, 0.72f);
                anchor.transform.localRotation = Quaternion.Euler(-8f, 14f, 0f);
                carryAnchor = anchor.transform;
            }

            heldBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heldBox.name = "Held Package Placeholder";
            heldBox.transform.SetParent(carryAnchor);
            heldBox.transform.localPosition = Vector3.zero;
            heldBox.transform.localRotation = Quaternion.identity;
            heldBox.transform.localScale = new Vector3(0.32f, 0.24f, 0.28f);

            if (heldBox.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }

            var renderer = heldBox.GetComponent<Renderer>();
            renderer.material.color = new Color(0.62f, 0.42f, 0.22f);
        }

        private void RefreshHeldBox()
        {
            EnsureHeldBox();
            if (heldBox != null)
            {
                heldBox.SetActive(packagesInBag > 0);
            }
        }

        private static void SetPointVisual(DeliveryPoint point, bool delivered)
        {
            foreach (var renderer in point.GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = delivered
                    ? new Color(0.18f, 0.72f, 0.32f)
                    : new Color(1.0f, 0.48f, 0.02f);
            }
        }

        private void EnsureHud()
        {
            if (hudCanvas != null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("Delivery HUD Canvas");
            canvasObject.transform.SetParent(transform);
            hudCanvas = canvasObject.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 20;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateImage("Shift Panel", canvasObject.transform, new Color(0.035f, 0.04f, 0.046f, 0.52f));
            SetRect(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(342f, 108f), new Vector2(0f, 1f));

            var accent = CreateImage("EMS Accent", panel.transform, new Color(1f, 0.48f, 0.02f, 1f));
            SetRect(accent.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 108f), new Vector2(0f, 1f));

            CreateText("Shift Title", panel.transform, font, "EMS DELIVERY", 13, FontStyle.Bold, new Color(1f, 0.68f, 0.28f), new Vector2(16f, -13f), new Vector2(310f, 18f), TextAnchor.MiddleLeft);
            statusText = CreateText("Status Text", panel.transform, font, status, 14, FontStyle.Normal, new Color(0.91f, 0.93f, 0.92f), new Vector2(16f, -35f), new Vector2(310f, 30f), TextAnchor.MiddleLeft);

            packagesText = CreateChip(panel.transform, font, "Hang 0", new Vector2(16f, -72f), new Color(0.02f, 0.025f, 0.03f, 0.36f));
            deliveriesText = CreateChip(panel.transform, font, "Don 0/0", new Vector2(121f, -72f), new Color(0.02f, 0.025f, 0.03f, 0.36f));
            cashText = CreateChip(panel.transform, font, "0 VND", new Vector2(226f, -72f), new Color(0.02f, 0.025f, 0.03f, 0.36f));

            var progressBack = CreateImage("Delivery Progress Back", panel.transform, new Color(0.18f, 0.2f, 0.21f, 0.82f));
            SetRect(progressBack.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -99f), new Vector2(310f, 3f), new Vector2(0f, 0.5f));
            progressFill = CreateImage("Delivery Progress Fill", progressBack.transform, new Color(1f, 0.48f, 0.02f, 1f));
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            SetRect(progressFill.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0f, 3f), new Vector2(0f, 0.5f));

            CreateCrosshair(canvasObject.transform);

            promptPanel = CreateImage("Interaction Prompt", canvasObject.transform, new Color(0.025f, 0.03f, 0.034f, 0.74f)).gameObject;
            SetRect(promptPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(420f, 46f), new Vector2(0.5f, 0f));
            var keyBadge = CreateImage("Key Badge", promptPanel.transform, new Color(1f, 0.48f, 0.02f, 1f));
            SetRect(keyBadge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(36f, 28f), new Vector2(0f, 0.5f));
            CreateText("Key Text", keyBadge.transform, font, "E", 16, FontStyle.Bold, Color.white, Vector2.zero, new Vector2(36f, 28f), TextAnchor.MiddleCenter);
            promptText = CreateText("Prompt Text", promptPanel.transform, font, string.Empty, 16, FontStyle.Normal, new Color(0.95f, 0.96f, 0.94f), new Vector2(64f, 0f), new Vector2(336f, 32f), TextAnchor.MiddleLeft);
            promptPanel.SetActive(false);
        }

        private Text CreateChip(Transform parent, Font font, string value, Vector2 anchoredPosition, Color background)
        {
            var chip = CreateImage($"{value} Chip", parent, background);
            SetRect(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, new Vector2(92f, 25f), new Vector2(0f, 0.5f));
            return CreateText($"{value} Text", chip.transform, font, value, 14, FontStyle.Bold, new Color(0.96f, 0.97f, 0.95f), Vector2.zero, new Vector2(88f, 23f), TextAnchor.MiddleCenter);
        }

        private static Image CreateImage(string objectName, Transform parent, Color color)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            var image = gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void CreateCrosshair(Transform parent)
        {
            var color = new Color(1f, 1f, 1f, 0.76f);
            var top = CreateImage("Crosshair Top", parent, color);
            SetRect(top.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(2f, 8f));
            var bottom = CreateImage("Crosshair Bottom", parent, color);
            SetRect(bottom.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(2f, 8f));
            var left = CreateImage("Crosshair Left", parent, color);
            SetRect(left.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-12f, 0f), new Vector2(8f, 2f));
            var right = CreateImage("Crosshair Right", parent, color);
            SetRect(right.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(12f, 0f), new Vector2(8f, 2f));
        }

        private static Text CreateText(string objectName, Transform parent, Font font, string value, int size, FontStyle style, Color color, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor alignment)
        {
            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            var text = gameObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            SetRect(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, sizeDelta, AlignmentToPivot(alignment));
            return text;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            SetRect(rect, anchorMin, anchorMax, anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f));
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Vector2 AlignmentToPivot(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft or TextAnchor.MiddleLeft or TextAnchor.LowerLeft => new Vector2(0f, 0.5f),
                TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight => new Vector2(1f, 0.5f),
                TextAnchor.UpperCenter => new Vector2(0.5f, 1f),
                TextAnchor.LowerCenter => new Vector2(0.5f, 0f),
                _ => new Vector2(0.5f, 0.5f)
            };
        }

        private void RefreshHud()
        {
            EnsureHud();
            if (packagesText == null)
            {
                return;
            }

            packagesText.text = $"Hang {packagesInBag}";
            deliveriesText.text = $"Don {completedDeliveries}/{points.Count}";
            cashText.text = $"{cash:n0} VND";
            statusText.text = status;

            var total = Mathf.Max(1, points.Count);
            progressFill.fillAmount = Mathf.Clamp01((float)completedDeliveries / total);

            var prompt = GetLookPrompt();
            var hasPrompt = !string.IsNullOrEmpty(prompt);
            promptPanel.SetActive(hasPrompt);
            if (hasPrompt)
            {
                promptText.text = prompt;
            }
        }

        private string GetLookPrompt()
        {
            if (playerCamera == null)
            {
                return string.Empty;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance, ~0, QueryTriggerInteraction.Collide))
            {
                return string.Empty;
            }

            var hitDepot = hit.collider.GetComponentInParent<DeliveryDepot>();
            if (hitDepot != null)
            {
                return hitDepot.Prompt;
            }

            var point = hit.collider.GetComponentInParent<DeliveryPoint>();
            return point != null ? point.Prompt : string.Empty;
        }
    }
}
