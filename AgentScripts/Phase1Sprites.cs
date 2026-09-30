using System.IO;
using UnityEditor;
using UnityEngine;

// Phase 1.2 임시 스프라이트 4종 생성 (흰색 · 가장자리 안티앨리어싱, 색은 코드가 Image.color로 입힌다)
// 실행: run_script(file=AgentScripts/Phase1Sprites.cs, entry=Phase1Sprites.Build) — 여러 번 실행해도 같은 결과(덮어씀)
// Phase 4.3 아이콘 2종(자물쇠 · 별, 스테이지 선택 화면): entry=Phase1Sprites.BuildIcons — 작게(128px) 만든다
public static class Phase1Sprites
{
    private const string Folder = "Assets/Art/Sprites";
    private const int Size = 256;
    private const int IconSize = 128;
    private const int Samples = 4; // 픽셀당 4×4 슈퍼샘플링
    private const float HexRadius = 0.98f;
    private const float RingWidth = 0.14f; // 반지름 대비 테두리 두께

    public static string Build()
    {
        Directory.CreateDirectory(Folder);
        Vector2[] hex = Hexagon(HexRadius);
        Vector2[] inner = Hexagon(HexRadius * (1f - RingWidth));
        // 프로토타입 방향 버튼 화살표(오른쪽을 가리킴)를 버튼 원 반지름 0.42 기준에서 1 기준으로 옮긴 것
        Vector2[] arrow = Scale(1f / 0.42f,
            new Vector2(0.25f, 0f), new Vector2(0.02f, -0.2f), new Vector2(0.02f, -0.075f), new Vector2(-0.22f, -0.075f),
            new Vector2(-0.22f, 0.075f), new Vector2(0.02f, 0.075f), new Vector2(0.02f, 0.2f));

        Save("HexFill", p => Inside(hex, p));
        Save("HexRing", p => Inside(hex, p) && !Inside(inner, p));
        Save("Circle", p => p.magnitude <= HexRadius);
        Save("Arrow", p => Inside(arrow, p));
        return $"스프라이트 4종 → {Folder}";
    }

    public static string BuildIcons()
    {
        // 별: 꼭짓점 5개, 안쪽 반지름 0.42
        var star = new Vector2[10];
        for (int k = 0; k < star.Length; k++)
        {
            float angle = Mathf.Deg2Rad * (90f + 36f * k);
            star[k] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (k % 2 == 0 ? 0.95f : 0.42f);
        }
        Save("Star", p => Inside(star, p), IconSize);
        // 자물쇠: 몸통(둥근 모서리 없는 사각형) + 위쪽 고리(반원 띠)
        Save("Lock", p =>
        {
            bool body = p.x >= -0.62f && p.x <= 0.62f && p.y >= -0.85f && p.y <= 0.1f;
            Vector2 c = p - new Vector2(0f, 0.1f);
            bool shackle = p.y >= 0.1f && c.magnitude <= 0.46f && c.magnitude >= 0.27f;
            bool legs = p.y >= 0f && p.y < 0.1f && Mathf.Abs(p.x) >= 0.27f && Mathf.Abs(p.x) <= 0.46f;
            return body || shackle || legs;
        }, IconSize);
        return $"아이콘 2종(자물쇠 · 별, {IconSize}px) → {Folder}";
    }

    // 꼭짓점이 위를 향하는 육각형 (꼭짓점 각도 90° + 60°k)
    private static Vector2[] Hexagon(float radius)
    {
        var points = new Vector2[6];
        for (int k = 0; k < 6; k++)
        {
            float angle = Mathf.Deg2Rad * (90f + 60f * k);
            points[k] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    private static Vector2[] Scale(float factor, params Vector2[] points)
    {
        for (int i = 0; i < points.Length; i++) points[i] *= factor;
        return points;
    }

    // 짝홀 규칙 점 포함 판정 (오목 다각형도 됨)
    private static bool Inside(Vector2[] poly, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
        {
            if ((poly[i].y > p.y) != (poly[j].y > p.y) &&
                p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x)
                inside = !inside;
        }
        return inside;
    }

    private static void Save(string name, System.Func<Vector2, bool> shape, int size = Size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < Samples; sy++)
                {
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        // 텍스처 좌표 → [-1, 1] (y 위쪽)
                        var p = new Vector2((x + (sx + 0.5f) / Samples) / size * 2f - 1f, (y + (sy + 0.5f) / Samples) / size * 2f - 1f);
                        if (shape(p)) hits++;
                    }
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * hits / (Samples * Samples)));
            }
        }
        texture.SetPixels32(pixels);
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
