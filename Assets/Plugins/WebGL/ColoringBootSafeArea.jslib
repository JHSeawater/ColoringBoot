// 앱인토스 안전영역 조회 — SafeAreaFitter 전용. 예외를 던지지 않는다(토스 밖에서는 window.AppsInToss가 없다)
mergeInto(LibraryManager.library, {
    // 토스 브리지가 처음 보일 때 · 변경 구독이 성공할 때 · SDK가 인셋 변경을 알릴 때마다 오르는 번호(브리지가 없으면 0).
    // SafeAreaFitter가 주기적으로 부르므로 인셋은 읽지 않는다. 구독이 실패했으면 부를 때마다 다시 시도한다
    CbSafeAreaVersion: function () {
        var state = Module.cbSafeArea || (Module.cbSafeArea = { version: 0, bridgeSeen: false, subscribed: false });
        if (!window.AppsInToss) return state.version;
        if (!state.bridgeSeen) {
            state.bridgeSeen = true;
            state.version++;
        }
        if (!state.subscribed) {
            try {
                window.AppsInToss.SafeAreaInsets.subscribe({ onEvent: function () { state.version++; } });
                state.subscribed = true;
                state.version++;
            } catch (e) {
                // 다음 호출에서 다시 시도한다
            }
        }
        return state.version;
    },

    // out(float 6개)에 top · bottom · left · right 인셋과 캔버스 width · height를 CSS 픽셀로 쓴다. 여섯 값을 모두 검사한 뒤에만 쓴다.
    // 돌려주는 값: 1 성공 · 2 성공했지만 변경 구독이 아직 안 됨 · 0 토스 밖(window.AppsInToss 없음) · -1 토스 안인데 읽기 실패(out은 그대로)
    CbSafeAreaRead: function (out) {
        if (!window.AppsInToss) return 0;
        try {
            var insets = window.AppsInToss.SafeAreaInsets.get();
            var values = [insets.top, insets.bottom, insets.left, insets.right, Module.canvas.clientWidth, Module.canvas.clientHeight];
            for (var i = 0; i < values.length; i++) {
                var value = values[i];
                if (typeof value !== "number" || !isFinite(value) || value < 0 || (i >= 4 && value <= 0)) return -1;
            }
            for (var j = 0; j < values.length; j++) HEAPF32[(out >> 2) + j] = values[j];
            return Module.cbSafeArea && Module.cbSafeArea.subscribed ? 1 : 2;
        } catch (e) {
            return -1;
        }
    }
});
