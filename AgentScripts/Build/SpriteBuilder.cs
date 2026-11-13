using System.IO;
using UnityEditor;
using UnityEngine;

// Phase 1.2 임시 스프라이트 4종 생성 (흰색 · 가장자리 안티앨리어싱, 색은 코드가 Image.color로 입힌다)
// 실행: run_script(file=AgentScripts/Build/SpriteBuilder.cs, entry=SpriteBuilder.Build) — 여러 번 실행해도 같은 결과(덮어씀)
// Phase 4.3 아이콘 2종(자물쇠 · 별, 스테이지 선택 화면): entry=SpriteBuilder.BuildIcons — 작게(128px) 만든다
// Phase 7.1 버튼 모양(디자인 시안 A): entry=SpriteBuilder.BuildRound — 둥근 사각형 바탕 · 외곽선(9-slice) · 얇은 육각 외곽선
// 2026-11-13 따라 하기 · 힌트 손가락: entry=SpriteBuilder.BuildFinger — 사용자 그림(ArtSource/UI/finger2.png)을 줄이고 손끝을 pivot으로(색 그대로)
public static class SpriteBuilder
{
    private const string Folder = "Assets/Art/Sprites";
    private const int Size = 256;
    private const int IconSize = 128;
    private const int Samples = 4; // 픽셀당 4×4 슈퍼샘플링
    private const float HexRadius = 0.98f;
    private const float RingWidth = 0.14f; // 반지름 대비 테두리 두께
    private const int RoundSize = 96;        // 둥근 사각형 스프라이트 (Phase 7.1)
    private const float RoundRadius = 32f;   // 모서리 반지름(픽셀)
    private const float RoundLine = 7f;      // 외곽선 두께(픽셀)
    private const int FrameSize = 48;        // 그림 테두리 스프라이트
    private const float FrameRadius = 12f;
    private const float HexLineWidth = 0.055f; // 스테이지 버튼 외곽선 — 반지름 대비
    private const string FingerSource = "ArtSource/UI/finger2.png";  // 손가락 표시 원본(사용자 그림 — 2026-11-13 finger.png에서 바꿈)
    private const int FingerHeight = 256;    // 손가락 스프라이트 높이(픽셀)
    private const byte AlphaCut = 128;       // 여백 · 손끝을 찾을 때 불투명으로 보는 알파

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

    // 둥근 사각형은 9-slice라 어떤 크기의 버튼에도 모서리 반지름 · 선 두께가 그대로다(캔버스 기준 픽셀 = 스프라이트 픽셀)
    public static string BuildRound()
    {
        SaveRound("RoundFill", RoundSize, RoundRadius, 0f);
        SaveRound("RoundRing", RoundSize, RoundRadius, RoundLine);
        // 그림 테두리: 모서리를 작게 해야 사각형 그림의 모서리가 테두리 밖으로 삐져나오지 않는다(그림 밖으로 선 두께만큼 넓혀 씌움)
        SaveRound("FrameRing", FrameSize, FrameRadius, RoundLine);
        Vector2[] hex = Hexagon(HexRadius);
        Vector2[] inner = Hexagon(HexRadius * (1f - HexLineWidth));
        Save("HexLine", p => Inside(hex, p) && !Inside(inner, p));
        return $"버튼 모양 4종(RoundFill · RoundRing 반지름 {RoundRadius} · FrameRing 반지름 {FrameRadius} · 선 {RoundLine} · HexLine) → {Folder}";
    }

    // 따라 하기 · 힌트 손가락 표시(2026-11-13): 사용자 그림 ArtSource/UI/finger2.png(원본은 그대로)를 여백을 잘라 높이 FingerHeight로 줄이고,
    // 손끝(검지 끝 둥근 부분의 가운데)을 pivot으로 둔다 — BoardView가 pivot을 칸 가운데에 맞춘다. 색은 그림 그대로(코드가 입히지 않음)
    public static string BuildFinger()
    {
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!source.LoadImage(File.ReadAllBytes(FingerSource))) throw new System.InvalidOperationException($"{FingerSource}: 그림을 읽지 못함");
        int w = source.width, h = source.height;
        Color32[] src = source.GetPixels32();   // 아래 줄부터
        int minX = w, maxX = -1, minY = h, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (src[y * w + x].a < AlphaCut) continue;
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
            }
        // 그림이 비었거나 손끝을 못 찾으면 엉뚱한 pivot으로 저장하지 않고 멈춘다(2026-11-13 리뷰 5)
        if (maxX < 0) throw new System.InvalidOperationException($"{FingerSource}: 불투명 픽셀(알파 {AlphaCut} 이상)이 없음");
        // 손끝: 맨 위에서 조금 내려간 줄의 불투명 구간 = 검지 굵기 → 가운데 x, 위 끝에서 굵기 절반 아래
        int probe = maxY - (maxY - minY) / 20, left = -1, right = -1;
        for (int x = minX; x <= maxX; x++)
        {
            if (src[probe * w + x].a < AlphaCut) continue;
            if (left < 0) left = x;
            right = x;
        }
        if (left < 0) throw new System.InvalidOperationException($"{FingerSource}: 손끝 줄(y={probe})에 불투명 픽셀이 없음");
        // 픽셀 가장자리 기준: 구간 [left, right + 1] · 위 끝 maxY + 1 (리뷰 6)
        float cropW = maxX - minX + 1, cropH = maxY - minY + 1, tip = right - left + 1;
        var pivot = new Vector2((left + tip * 0.5f - minX) / cropW, (maxY + 1 - tip * 0.5f - minY) / cropH);

        int outW = Mathf.RoundToInt(cropW * FingerHeight / cropH);
        var texture = new Texture2D(outW, FingerHeight, TextureFormat.RGBA32, false);
        var pixels = new Color32[outW * FingerHeight];
        for (int y = 0; y < FingerHeight; y++)
            for (int x = 0; x < outW; x++)
            {
                // 4×4 표본 평균(알파 가중 — 가장자리가 검게 번지지 않게)
                Vector3 rgb = Vector3.zero;
                float alpha = 0f;
                for (int sy = 0; sy < Samples; sy++)
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float u = (minX + (x + (sx + 0.5f) / Samples) * cropW / outW) / w;
                        float v = (minY + (y + (sy + 0.5f) / Samples) * cropH / FingerHeight) / h;
                        Color c = source.GetPixelBilinear(u, v);
                        rgb += new Vector3(c.r, c.g, c.b) * c.a;
                        alpha += c.a;
                    }
                Color result = alpha > 0f ? new Color(rgb.x / alpha, rgb.y / alpha, rgb.z / alpha, alpha / (Samples * Samples)) : Color.clear;
                pixels[y * outW + x] = result;
            }
        texture.SetPixels32(pixels);
        Object.DestroyImmediate(source);
        string path = WriteSprite("Finger", texture, Vector4.zero, pivot);
        return $"손가락 {outW}×{FingerHeight}px · 손끝 pivot ({pivot.x:0.000}, {pivot.y:0.000}) · 원본 여백 자름 [{minX}..{maxX}]×[{minY}..{maxY}] → {path} ({new FileInfo(path).Length:N0}바이트)";
    }

    // line = 0이면 채운 둥근 사각형, 아니면 그 두께의 외곽선만
    private static void SaveRound(string name, int size, float radius, float line)
    {
        float half = size / 2f;
        bool InRound(Vector2 p, float inset)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) * half;   // [-1, 1] → 가운데 기준 픽셀
            float edge = half - inset, r = radius - inset;
            Vector2 corner = new Vector2(edge - r, edge - r);
            if (q.x <= corner.x || q.y <= corner.y) return q.x <= edge && q.y <= edge;
            return (q - corner).magnitude <= r;
        }
        Save(name, p => InRound(p, 0f) && (line <= 0f || !InRound(p, line)), size, radius + 2f);
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

    // border > 0이면 9-slice 테두리(픽셀)
    private static void Save(string name, System.Func<Vector2, bool> shape, int size = Size, float border = 0f)
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
        WriteSprite(name, texture, Vector4.one * border, null);
    }

    // PNG로 쓰고(텍스처는 지움) UI 스프라이트로 임포트한다. pivot이 없으면 가운데 — Save · BuildFinger 공용(2026-11-13 리뷰 7)
    private static string WriteSprite(string name, Texture2D texture, Vector4 border, Vector2? pivot)
    {
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
        importer.spriteBorder = border;
        if (pivot.HasValue)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot.Value;
            importer.SetTextureSettings(settings);
        }
        importer.SaveAndReimport();
        return path;
    }
}
