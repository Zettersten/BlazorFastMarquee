/**
 * High-performance marquee measurement and observation module.
 * Optimized for minimal allocations and efficient DOM operations.
 */

const noop = () => {};

/**
 * Measures container and marquee dimensions.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marquee - Marquee element
 * @param {boolean} vertical - Whether to measure height instead of width
 * @returns {{containerSpan: number, marqueeSpan: number}} Measurement results
 */
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

/**
 * Notifies .NET about layout changes.
 * @param {Object} state - Observer state
 */
function notify(state) {
    if (!state.dotnetRef || state.disposed) {
        return;
    }

    const { containerSpan, marqueeSpan } = measureSpan(
        state.container, 
        state.marquee, 
        state.vertical
    );
    
    state.dotnetRef
        .invokeMethodAsync('UpdateLayout', containerSpan, marqueeSpan)
        .catch(noop);
}

/**
 * Creates a resize observer or fallback listener.
 * @param {Object} state - Observer state
 * @returns {Object} Observer handle with update/dispose methods
 */
function createResizeHandle(state) {
    let resizeObserver = null;
    let resizeHandler = null;

    // Use ResizeObserver for efficient resize detection
    if (typeof ResizeObserver !== 'undefined') {
        resizeHandler = () => notify(state);
        resizeObserver = new ResizeObserver(resizeHandler);
        resizeObserver.observe(state.container);
        resizeObserver.observe(state.marquee);
        
        state.cleanup = () => {
            if (resizeObserver) {
                resizeObserver.disconnect();
                resizeObserver = null;
            }
        };
    } else {
        // Fallback to window resize for older browsers
        resizeHandler = () => notify(state);
        window.addEventListener('resize', resizeHandler, { passive: true });
        
        state.cleanup = () => {
            if (resizeHandler) {
                window.removeEventListener('resize', resizeHandler);
                resizeHandler = null;
            }
        };
    }

    // Initial measurement
    notify(state);

    return {
        /**
         * Updates the vertical measurement mode.
         * @param {boolean} vertical - Whether to measure vertically
         */
        update(vertical) {
            if (state.disposed) return;
            state.vertical = Boolean(vertical);
            notify(state);
        },
        
        /**
         * Disposes the observer and cleans up resources.
         */
        dispose() {
            if (state.disposed) return;
            
            state.disposed = true;
            
            if (state.cleanup) {
                state.cleanup();
                state.cleanup = null;
            }

            state.dotnetRef = null;
            state.container = null;
            state.marquee = null;
        }
    };
}

/**
 * Measures marquee dimensions once.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marquee - Marquee element
 * @param {boolean} vertical - Whether to measure height instead of width
 * @returns {{containerSpan: number, marqueeSpan: number}} Measurement results
 */
export function measure(container, marquee, vertical) {
    return measureSpan(container, marquee, Boolean(vertical));
}

/**
 * Creates an observer that monitors size changes.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marquee - Marquee element
 * @param {boolean} vertical - Whether to measure height instead of width
 * @param {Object} dotnetRef - .NET object reference for callbacks
 * @returns {Object|null} Observer handle or null if invalid parameters
 */
export function observe(container, marquee, vertical, dotnetRef) {
    if (!container || !marquee || !dotnetRef) {
        return null;
    }

    const state = {
        container,
        marquee,
        dotnetRef,
        vertical: Boolean(vertical),
        cleanup: null,
        disposed: false
    };

    return createResizeHandle(state);
}
