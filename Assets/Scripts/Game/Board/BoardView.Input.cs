using ColoringBoot.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;

namespace ColoringBoot.Game
{
    // BoardView 입력 — 끌기 · 탭 · 끌기 취소 · 키보드 칸 이동 (필드는 BoardView.cs)
    public sealed partial class BoardView
    {
        // 키보드: 선택한 칸에서 want 쪽(화면 방향)의 가장 알맞은 칸으로. 선택이 없으면 첫 칸
        public void MoveSelection(Vector2 want)
        {
            if (!_interactable || _board == null) return;
            int current = _selected < 0 ? 0 : _selected;
            int best = current;
            float bestScore = float.MinValue;
            for (int i = 0; i < _centers.Length; i++)
            {
                if (i == current || _board.KindOf(i) == CellKind.Wall) continue;
                Vector2 offset = _centers[i] - _centers[current];
                float distance = offset.magnitude;
                float dot = Vector2.Dot(offset, want) / distance;
                if (dot < KeyMinDot) continue;
                float score = dot - distance / (_radius * KeyDistanceWeight);
                if (score <= bestScore) continue;
                bestScore = score;
                best = i;
            }
            Select(best);
        }

        // 키보드: 선택한 칸에서 dir로 긋는다. 선택이 없으면 첫 칸을 고르기만 한다(프로토타입과 같음)
        public void BrushSelected(HexDirection dir)
        {
            if (!_interactable || _board == null) return;
            if (_selected < 0) Select(0);
            else if (_board.LineLength(_selected, dir) > 1) OnDirectionClicked(dir);
        }

        public void ClearSelection() => Select(-1);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_interactable || _board == null || _pointerId != NoPointer) return;
            Vector2 local = ToBoardLocal(eventData);
            int cell = CellAt(local);
            if (cell < 0)
            {
                Select(-1);
                return;
            }
            _pointerId = eventData.pointerId;
            _dragCell = cell;
            _dragStart = local;
            _dragDirection = -1;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Vector2 delta = ToBoardLocal(eventData) - _dragStart;
            int direction = delta.magnitude < DragThreshold * _radius ? -1 : NearestDirection(delta);
            if (direction == _dragDirection) return;
            _dragDirection = direction;
            if (direction < 0)
            {
                ClearPreview();
                return;
            }
            Select(-1);
            ShowPreview(_dragCell, (HexDirection)direction);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            if (IsCanceledTouch(eventData))
            {
                CancelDrag();
                return;
            }
            _pointerId = NoPointer;
            if (_dragDirection < 0)
            {
                // 끌지 않고 뗌 = 탭: 칸 선택(같은 칸이면 해제) → 방향 버튼
                Select(_dragCell == _selected ? -1 : _dragCell);
                return;
            }
            var dir = (HexDirection)_dragDirection;
            _dragDirection = -1;
            ClearPreview();
            if (_board.LineLength(_dragCell, dir) > 1) BrushRequested?.Invoke(_dragCell, dir);
        }

        // 앱이 포커스를 잃으면(앱 전환 · 알림) 끌던 획을 버린다 — 뒤따르는 OnPointerUp은 추적 중인 포인터가 없어 무시된다
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && _pointerId != NoPointer) CancelDrag();
        }

        // 끌기 취소: 획을 긋지 않고 미리보기만 지운다 (프로토타입 pointercancel과 같음 — 점검 F4). 되돌리기 · 처음부터도 부른다(PuzzleController)
        public void CancelDrag()
        {
            _pointerId = NoPointer;
            _dragDirection = -1;
            ClearPreview();
        }

        // Input System UI 모듈은 취소된 터치에도 OnPointerUp을 보낸다 → 터치가 취소(Canceled)됐거나 포커스를 잃은 채 떼어졌으면 취소로 본다
        private static bool IsCanceledTouch(PointerEventData eventData)
        {
            if (!(eventData is ExtendedPointerEventData touch) || touch.pointerType != UIPointerType.Touch) return false;
            if (!Application.isFocused) return true;
            if (!(touch.device is Touchscreen screen)) return false;
            foreach (TouchControl control in screen.touches)
            {
                if (control.touchId.ReadValue() == touch.touchId)
                    return control.phase.ReadValue() == UnityEngine.InputSystem.TouchPhase.Canceled;
            }
            return false;
        }

        // 누른 자리의 칸 — 벽은 어느 줄에도 없어 고를 수 없다(기믹, 2026-10-10)
        private int CellAt(Vector2 local)
        {
            int best = -1;
            float bestDistance = HitRadius * _radius;
            for (int i = 0; i < _centers.Length; i++)
            {
                if (_board.KindOf(i) == CellKind.Wall) continue;
                float distance = Vector2.Distance(_centers[i], local);
                if (distance > bestDistance) continue;
                bestDistance = distance;
                best = i;
            }
            return best;
        }

        private static int NearestDirection(Vector2 delta)
        {
            int best = 0;
            float bestDot = float.MinValue;
            for (int d = 0; d < DirectionCount; d++)
            {
                float dot = Vector2.Dot(delta, _directionVectors[d]);
                if (dot <= bestDot) continue;
                bestDot = dot;
                best = d;
            }
            return best;
        }

        // 화면 좌표 → 보드 영역 가운데 기준 좌표
        private Vector2 ToBoardLocal(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, eventData.position, eventData.pressEventCamera, out Vector2 local);
            return local - _rect.rect.center;
        }
    }
}
