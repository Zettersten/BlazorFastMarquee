/**
 * High-performance marquee measurement and observation module.
 * Optimized for minimal allocations and efficient DOM operations.
 */

const noop = () => { };

/**
 * Measures container and marquee dimensions.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marquee - Marquee element
 * @param {boolean} vertical - Whether to measure height instead of width
 * @returns {{containerSpan: number, marqueeSpan: number}} Measurement results
 */
function measureSpan(container, marquee, vertical) {
  console.log('measureSpan called', { container, marquee, vertical });

  if (!container || !marquee) {
    console.log('measureSpan: missing container or marquee');
    return { containerSpan: 0, marqueeSpan: 0 };
  }

  const containerRect = container.getBoundingClientRect();
  const marqueeRect = marquee.getBoundingClientRect();
  const containerSpan = vertical ? containerRect.height : containerRect.width;
  const marqueeSpan = vertical ? marqueeRect.height : marqueeRect.width;

  console.log('measureSpan results', { containerSpan, marqueeSpan });
  return { containerSpan, marqueeSpan };
}

/**
 * Notifies .NET about layout changes.
 * @param {Object} state - Observer state
 */
function notify(state) {
  console.log('notify called', { state: state?.disposed, hasRef: !!state?.dotnetRef });

  if (!state.dotnetRef || state.disposed) {
    console.log('notify: skipping due to disposed or no dotnetRef');
    return;
  }

  const { containerSpan, marqueeSpan } = measureSpan(
    state.container,
    state.marquee,
    state.vertical
  );

  console.log('notify: calling UpdateLayout with', { containerSpan, marqueeSpan });
  state.dotnetRef
    .invokeMethodAsync('UpdateLayout', containerSpan, marqueeSpan)
    .catch(err => console.error('notify error:', err));
}

/**
 * Creates a resize observer or fallback listener.
 * @param {Object} state - Observer state
 * @returns {Object} Observer handle with update/dispose methods
 */
function createResizeHandle(state) {
  console.log('createResizeHandle called');
  let resizeObserver = null;
  let resizeHandler = null;

  // Use ResizeObserver for efficient resize detection
  if (typeof ResizeObserver !== 'undefined') {
    console.log('Using ResizeObserver');
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
    console.log('Using window resize fallback');
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
      console.log('observer update called', vertical);
      if (state.disposed) return;
      state.vertical = Boolean(vertical);
      notify(state);
    },

    /**
    * Disposes the observer and cleans up resources.
         */
    dispose() {
      console.log('observer dispose called');
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
 * Sets up animation event listeners for marquee element.
 * @param {HTMLElement} marqueeElement - The marquee animation element
 * @param {Object} dotnetRef - .NET object reference for callbacks
 * @returns {Object} Animation handler with dispose method
 */
function createAnimationHandler(marqueeElement, dotnetRef) {
  console.log('createAnimationHandler called', { marqueeElement, dotnetRef });

  if (!marqueeElement || !dotnetRef) {
    console.log('createAnimationHandler: missing element or dotnetRef');
    return null;
  }

  const state = {
    element: marqueeElement,
    dotnetRef,
    disposed: false,
    iterationHandler: null,
    endHandler: null
  };

  // Animation iteration event handler
  state.iterationHandler = () => {
    console.log('Animation iteration event fired');
    if (state.disposed || !state.dotnetRef) return;

    state.dotnetRef
      .invokeMethodAsync('HandleAnimationIteration')
      .catch(err => console.error('Animation iteration error:', err));
  };

  // Animation end event handler
  state.endHandler = () => {
    console.log('Animation end event fired');
    if (state.disposed || !state.dotnetRef) return;

    state.dotnetRef
      .invokeMethodAsync('HandleAnimationEnd')
      .catch(err => console.error('Animation end error:', err));
  };

  // Add event listeners
  console.log('Adding animation event listeners');
  marqueeElement.addEventListener('animationiteration', state.iterationHandler, { passive: true });
  marqueeElement.addEventListener('animationend', state.endHandler, { passive: true });

  return {
    /**
     * Disposes the animation event listeners.
     */
    dispose() {
      console.log('animation handler dispose called');
      if (state.disposed) return;

      state.disposed = true;

      if (state.element && state.iterationHandler) {
        state.element.removeEventListener('animationiteration', state.iterationHandler);
      }

      if (state.element && state.endHandler) {
        state.element.removeEventListener('animationend', state.endHandler);
      }

      state.element = null;
      state.dotnetRef = null;
      state.iterationHandler = null;
      state.endHandler = null;
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
  console.log('measure function called');
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
  console.log('observe function called', { container, marquee, vertical, dotnetRef });

  if (!container || !marquee || !dotnetRef) {
    console.log('observe: invalid parameters');
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

/**
 * Sets up animation event listeners for the marquee element.
 * @param {HTMLElement} marqueeElement - The marquee animation element
 * @param {Object} dotnetRef - .NET object reference for callbacks
 * @returns {Object|null} Animation handler or null if invalid parameters
 */
export function setupAnimationEvents(marqueeElement, dotnetRef) {
  console.log('setupAnimationEvents function called');
  return createAnimationHandler(marqueeElement, dotnetRef);
}

console.log('Marquee JavaScript module loaded');
