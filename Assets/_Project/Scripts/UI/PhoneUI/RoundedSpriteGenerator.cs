using UnityEngine;

public static class RoundedSpriteGenerator
{
    /// <summary>
    /// Tạo ra một Sprite hình chữ nhật có bo góc mềm mại (Anti-aliasing cơ bản)
    /// </summary>
    public static Sprite GenerateRoundedRect(int width, int height, int radius, Color color)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Khoảng cách từ pixel hiện tại tới tâm của góc bo tròn
                float dx = Mathf.Max(0, radius - x, x - (width - 1 - radius));
                float dy = Mathf.Max(0, radius - y, y - (height - 1 - radius));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                // Khử răng cưa cơ bản bằng alpha blending
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                
                Color pColor = color;
                pColor.a *= alpha;
                
                pixels[y * width + x] = pColor;
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // Cắt 9-slice để khi Scale không bị méo góc bo
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
