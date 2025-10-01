const noop = () => {};

function measureSpan(container, marquee, vertical) {
    if (!container || !marquee) {
        return { containerSpan: 0, marqueeSpan: 0 };
    }

    const containerRect = container.getBoundingClientRect();
    const marqueeRect = marquee.getBoundingClientRect();
    const containerSpan = vertical ? containerRect.height : containerRect.width;
    const marqueeSpan = vertical ? marqueeRect.height : marqueeRect.width;

    return { containerSpan, marqueeSpan };
}

function notify(state) {
    if (!state.dotnetRef) {
        return;
    }

    const { containerSpan, marqueeSpan } = measureSpan(state.container, state.marquee, state.vertical);
    state.dotnetRef.invokeMethodAsync('UpdateLayout', containerSpan, marqueeSpan).catch(noop);
}

function createResizeHandle(state) {
    const resizeHandler = () => notify(state);

    if (typeof ResizeObserver !== 'undefined') {
        const resizeObserver = new ResizeObserver(resizeHandler);
        resizeObserver.observe(state.container);
        resizeObserver.observe(state.marquee);
        state.cleanup = () => resizeObserver.disconnect();
    } else {
        window.addEventListener('resize', resizeHandler);
        state.cleanup = () => window.removeEventListener('resize', resizeHandler);
    }

    notify(state);

    return {
        update(vertical) {
            state.vertical = Boolean(vertical);
            notify(state);
        },
        dispose() {
            if (state.cleanup) {
                state.cleanup();
                state.cleanup = null;
            }

            state.dotnetRef = null;
        }
    };
}

export function measure(container, marquee, vertical) {
    return measureSpan(container, marquee, Boolean(vertical));
}

export function observe(container, marquee, vertical, dotnetRef) {
    if (!container || !marquee || !dotnetRef) {
        return null;
    }

    const state = {
        container,
        marquee,
        dotnetRef,
        vertical: Boolean(vertical),
        cleanup: null
    };

    return createResizeHandle(state);
}
