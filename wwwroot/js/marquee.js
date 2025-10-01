const noop = () => {};

function normalizeOptions(options) {
    const direction = (options?.direction ?? 'left').toString().toLowerCase();
    return {
        direction: direction === 'right' || direction === 'up' || direction === 'down' ? direction : 'left',
        autoFill: Boolean(options?.autoFill)
    };
}

function measure(state) {
    if (!state.container || !state.marquee) {
        return;
    }

    const containerRect = state.container.getBoundingClientRect();
    const marqueeRect = state.marquee.getBoundingClientRect();

    const vertical = state.options.direction === 'up' || state.options.direction === 'down';
    const containerSpan = vertical ? containerRect.height : containerRect.width;
    const marqueeSpan = vertical ? marqueeRect.height : marqueeRect.width;

    state.dotnetRef?.invokeMethodAsync('UpdateLayout', containerSpan, marqueeSpan).catch(noop);
}

function createHandle(state) {
    return {
        updateOptions(opts) {
            state.options = normalizeOptions(opts);
            requestAnimationFrame(() => measure(state));
        },
        forceMeasure() {
            requestAnimationFrame(() => measure(state));
        },
        dispose() {
            state.resizeObserver?.disconnect();
            state.mutationObserver?.disconnect();
            state.dotnetRef = null;
        }
    };
}

export function initialize(container, marquee, options, dotnetRef) {
    if (!container || !marquee) {
        return null;
    }

    const state = {
        container,
        marquee,
        options: normalizeOptions(options),
        dotnetRef,
        resizeObserver: null,
        mutationObserver: null
    };

    if (typeof ResizeObserver !== 'undefined') {
        const resizeObserver = new ResizeObserver(() => measure(state));
        resizeObserver.observe(container);
        resizeObserver.observe(marquee);
        state.resizeObserver = resizeObserver;
    } else {
        const resizeHandler = () => measure(state);
        window.addEventListener('resize', resizeHandler);
        state.resizeObserver = {
            disconnect() {
                window.removeEventListener('resize', resizeHandler);
            }
        };
    }

    if (typeof MutationObserver !== 'undefined') {
        const mutationObserver = new MutationObserver(() => measure(state));
        mutationObserver.observe(marquee, { childList: true, subtree: true, characterData: true });
        state.mutationObserver = mutationObserver;
    }

    requestAnimationFrame(() => measure(state));

    return createHandle(state);
}

