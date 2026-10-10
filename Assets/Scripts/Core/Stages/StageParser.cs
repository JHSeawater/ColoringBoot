using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ColoringBoot.Core
{
    // 스테이지 코드(JSON) 파서 — 이 형식만 읽는다 (GDD §3):
    // {"name": 문자열, "cells": [[q, r, 시작 색, 목표 색], ...], "palette": 문자열(선택), "minMoves": 정수(선택)}
    // 기믹 칸(선택, 2026-10-10): "walls" · "water": [[q, r], ...](cells에 없는 자리) · "coated": [[q, r], ...](cells의 칸 — 시작 색 = 목표 색).
    // 칸 순서는 cells(코팅 포함) → 벽 → 물. 순수 로직 계층이라 JsonUtility를 쓸 수 없고, JsonUtility는 중첩 배열도 읽지 못해서 직접 읽는다. 형식 오류는 FormatException
    internal sealed class StageParser
    {
        private readonly string _json;
        private int _pos;

        private StageParser(string json)
        {
            _json = json;
        }

        public static Stage Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            return new StageParser(json).ReadStage();
        }

        private Stage ReadStage()
        {
            string name = null;
            List<StageCell> cells = null;
            string palette = null;
            int? minMoves = null;
            List<HexCoord> walls = null, water = null, coated = null;

            Expect('{');
            do
            {
                string key = ReadString();
                Expect(':');
                switch (key)
                {
                    case "name": name = ReadString(); break;
                    case "cells": cells = ReadCells(); break;
                    case "palette": palette = ReadString(); break;
                    case "minMoves": minMoves = ReadInt(); break;
                    case "walls": walls = ReadCoords(); break;
                    case "water": water = ReadCoords(); break;
                    case "coated": coated = ReadCoords(); break;
                    default: throw Error($"알 수 없는 키 '{key}'");
                }
            } while (TryConsume(','));
            Expect('}');
            SkipWhitespace();
            if (_pos < _json.Length) throw Error("스테이지 코드 뒤에 다른 내용이 있습니다");

            if (name == null) throw Error("name이 없습니다");
            if (cells == null) throw Error("cells가 없습니다");
            if (coated != null) MarkCoated(cells, coated);
            if (walls != null) foreach (HexCoord coord in walls) cells.Add(new StageCell(coord, PaintColor.Empty, PaintColor.Empty, CellKind.Wall));
            if (water != null) foreach (HexCoord coord in water) cells.Add(new StageCell(coord, PaintColor.Empty, PaintColor.Empty, CellKind.Water));
            try
            {
                return new Stage(name, cells, palette, minMoves);
            }
            catch (ArgumentException e)
            {
                throw new FormatException($"스테이지 코드 형식 오류: {e.Message}", e);
            }
        }

        private List<StageCell> ReadCells()
        {
            var cells = new List<StageCell>();
            Expect('[');
            if (TryConsume(']')) return cells;
            do
            {
                Expect('[');
                int q = ReadInt();
                Expect(',');
                int r = ReadInt();
                Expect(',');
                PaintColor start = ReadColor();
                Expect(',');
                PaintColor target = ReadColor();
                Expect(']');
                cells.Add(new StageCell(new HexCoord(q, r), start, target));
            } while (TryConsume(','));
            Expect(']');
            return cells;
        }

        // [[q, r], ...]
        private List<HexCoord> ReadCoords()
        {
            var coords = new List<HexCoord>();
            Expect('[');
            if (TryConsume(']')) return coords;
            do
            {
                Expect('[');
                int q = ReadInt();
                Expect(',');
                int r = ReadInt();
                Expect(']');
                coords.Add(new HexCoord(q, r));
            } while (TryConsume(','));
            Expect(']');
            return coords;
        }

        // coated의 좌표마다 cells의 그 칸을 코팅 칸으로 바꾼다
        private void MarkCoated(List<StageCell> cells, List<HexCoord> coated)
        {
            var marks = new HashSet<HexCoord>();
            foreach (HexCoord coord in coated)
            {
                if (!marks.Add(coord)) throw Error($"코팅 칸 좌표가 중복됩니다: {coord}");
            }
            int found = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                StageCell cell = cells[i];
                if (!marks.Contains(cell.Coord)) continue;
                cells[i] = new StageCell(cell.Coord, cell.Start, cell.Target, CellKind.Coated);
                found++;
            }
            if (found != marks.Count) throw Error("코팅 칸 좌표가 cells에 없습니다");
        }

        private PaintColor ReadColor()
        {
            int value = ReadInt();
            if (value < (int)PaintColor.Empty || value > (int)PaintColor.Black) throw Error($"색 값은 0~7이어야 합니다: {value}");
            return (PaintColor)value;
        }

        private int ReadInt()
        {
            SkipWhitespace();
            int start = _pos;
            if (_pos < _json.Length && _json[_pos] == '-') _pos++;
            while (_pos < _json.Length && _json[_pos] >= '0' && _json[_pos] <= '9') _pos++;
            if (!int.TryParse(_json.Substring(start, _pos - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
                throw Error("정수가 필요합니다");
            return value;
        }

        private string ReadString()
        {
            Expect('"');
            var text = new StringBuilder();
            while (true)
            {
                if (_pos >= _json.Length) throw Error("문자열이 닫히지 않았습니다");
                char c = _json[_pos++];
                if (c == '"') return text.ToString();
                if (c != '\\')
                {
                    text.Append(c);
                    continue;
                }

                if (_pos >= _json.Length) throw Error("문자열이 닫히지 않았습니다");
                char escaped = _json[_pos++];
                switch (escaped)
                {
                    case '"':
                    case '\\':
                    case '/': text.Append(escaped); break;
                    case 'b': text.Append('\b'); break;
                    case 'f': text.Append('\f'); break;
                    case 'n': text.Append('\n'); break;
                    case 'r': text.Append('\r'); break;
                    case 't': text.Append('\t'); break;
                    case 'u':
                        if (_pos + 4 > _json.Length || !ushort.TryParse(_json.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort code))
                            throw Error("잘못된 유니코드 이스케이프");
                        text.Append((char)code);
                        _pos += 4;
                        break;
                    default: throw Error($"잘못된 이스케이프 문자 '{escaped}'");
                }
            }
        }

        private void Expect(char c)
        {
            if (!TryConsume(c)) throw Error($"'{c}' 필요");
        }

        private bool TryConsume(char c)
        {
            SkipWhitespace();
            if (_pos >= _json.Length || _json[_pos] != c) return false;
            _pos++;
            return true;
        }

        private void SkipWhitespace()
        {
            while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos])) _pos++;
        }

        private FormatException Error(string message) => new FormatException($"스테이지 코드 형식 오류 (위치 {_pos}): {message}");
    }
}
