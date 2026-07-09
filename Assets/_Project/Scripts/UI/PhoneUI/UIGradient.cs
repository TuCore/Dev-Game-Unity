using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("UI/Effects/Gradient")]
public class UIGradient : BaseMeshEffect
{
    public Color color1 = Color.white;
    public Color color2 = Color.white;
    [Range(-180f, 180f)]
    public float angle = 0f;

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || vh.currentVertCount == 0) return;

        float bottomY = float.MaxValue;
        float topY = float.MinValue;
        float leftX = float.MaxValue;
        float rightX = float.MinValue;

        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (vertex.position.y > topY) topY = vertex.position.y;
            if (vertex.position.y < bottomY) bottomY = vertex.position.y;
            if (vertex.position.x > rightX) rightX = vertex.position.x;
            if (vertex.position.x < leftX) leftX = vertex.position.x;
        }

        float uiElementHeight = topY - bottomY;
        float uiElementWidth = rightX - leftX;

        float dirX = Mathf.Sin(angle * Mathf.Deg2Rad);
        float dirY = Mathf.Cos(angle * Mathf.Deg2Rad);

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            
            float xPos = uiElementWidth > 0 ? (vertex.position.x - leftX) / uiElementWidth - 0.5f : 0;
            float yPos = uiElementHeight > 0 ? (vertex.position.y - bottomY) / uiElementHeight - 0.5f : 0;
            
            float localGradientPos = xPos * dirX + yPos * dirY + 0.5f;
            vertex.color = Color.Lerp(color2, color1, Mathf.Clamp01(localGradientPos));
            vh.SetUIVertex(vertex, i);
        }
    }
}
