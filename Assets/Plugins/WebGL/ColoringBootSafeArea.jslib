// 앱인토스 안전영역 조회 — SafeAreaFitter 전용. 예외를 던지지 않는다(토스 밖에서는 window.AppsInToss가 없다)
mergeInto(LibraryManager.library, {
    // 토스 브리지가 처음 보일 때와 SDK가 인셋 변경을 알릴 때마다 오르는 번호(브리지가 없으면 0). 매 프레임 불리므로 인셋은 읽지 않는다
    CbSafeAreaVersion: function () {
        var state = Module.cbSafeArea || (Module.cbSafeArea = { version: 0, bridgeSeen: false, subscribeFailed: false });
        if (!state.bridgeSeen && window.AppsInToss) {
            state.bridgeSeen = true;
            state.version++;
            try {
                window.AppsInToss.SafeAreaInsets.subscribe({ onEvent: function () { state.version++; } });
            } catch (e) {
                state.subscribeFailed = true;
            }
        }
        return state.version;
    },

    // out(float 6개)에 top · bottom · left · right 인셋과 캔버스 width · height를 CSS 픽셀로 쓴다.
    // 돌려주는 값: 1 성공 · 2 성공했지만 변경 구독 실패 · 0 토스 밖(window.AppsInToss 없음) · -1 토스 안인데 읽기 실패
    CbSafeAreaRead: function (out) {
        if (!window.AppsInToss) return 0;
        try {
            var insets = window.AppsInToss.SafeAreaInsets.get();
            var values = [insets.top, insets.bottom, insets.left, insets.right, Module.canvas.clientWidth, Module.canvas.clientHeight];
            for (var i = 0; i < values.length; i++) {
                var value = values[i];
                if (typeof value !== "number" || !isFinite(value) || value < 0 || (i >= 4 && value <= 0)) return -1;
                HEAPF32[(out >> 2) + i] = value;
            }
            return Module.cbSafeArea && Module.cbSafeArea.subscribeFailed ? 2 : 1;
        } catch (e) {
            return -1;
        }
    }
});
