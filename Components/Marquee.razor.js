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

/**
 * Creates a drag handler for pan/drag functionality.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marqueeElement - Marquee element to manipulate
 * @param {boolean} vertical - Whether to enable vertical dragging
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
function createDragHandler(container, marqueeElement, vertical) {
  console.log('createDragHandler called', { container, marqueeElement, vertical });

  if (!container || !marqueeElement) {
    console.log('createDragHandler: missing container or marqueeElement');
    return null;
  }

  const state = {
    container,
    marqueeElement,
    vertical: Boolean(vertical),
    disposed: false,
    isDragging: false,
    startX: 0,
    startY: 0,
    currentX: 0,
    currentY: 0,
    dragOffset: 0,
    originalPlayState: null,
    pointerDownHandler: null,
    pointerMoveHandler: null,
    pointerUpHandler: null,
    pointerCancelHandler: null
  };

  // Store original animation play state
  const savePlayState = () => {
    const computedStyle = window.getComputedStyle(state.marqueeElement);
    state.originalPlayState = computedStyle.animationPlayState;
  };

  // Pause animation during drag
  const pauseAnimation = () => {
    state.marqueeElement.style.animationPlayState = 'paused';
  };

  // Restore animation after drag
  const restoreAnimation = () => {
    if (state.originalPlayState) {
      state.marqueeElement.style.animationPlayState = state.originalPlayState;
    }
  };

  // Apply transform based on drag offset
  const applyDragTransform = () => {
    if (state.vertical) {
      state.marqueeElement.style.transform = `translateY(${state.dragOffset}px)`;
    } else {
      state.marqueeElement.style.transform = `translateX(${state.dragOffset}px)`;
    }
  };

  // Handle pointer down (mouse/touch start)
  state.pointerDownHandler = (e) => {
    if (state.disposed) return;

    // Support both mouse and touch events
    const clientX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
    const clientY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;

    state.isDragging = true;
    state.startX = clientX - state.currentX;
    state.startY = clientY - state.currentY;

    savePlayState();
    pauseAnimation();

    // Add grabbing cursor
    state.container.style.cursor = 'grabbing';
    state.container.style.userSelect = 'none';

    // Prevent text selection during drag
    e.preventDefault();
  };

  // Handle pointer move (mouse/touch move)
  state.pointerMoveHandler = (e) => {
    if (!state.isDragging || state.disposed) return;

    // Support both mouse and touch events
    const clientX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
    const clientY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;

    if (state.vertical) {
      state.currentY = clientY - state.startY;
      state.dragOffset = state.currentY;
    } else {
      state.currentX = clientX - state.startX;
      state.dragOffset = state.currentX;
    }

    applyDragTransform();
    e.preventDefault();
  };

  // Handle pointer up (mouse/touch end)
  state.pointerUpHandler = (e) => {
    if (!state.isDragging || state.disposed) return;

    state.isDragging = false;
    
    // Reset cursor
    state.container.style.cursor = 'grab';
    state.container.style.userSelect = '';

    // Reset transform and restore animation
    state.marqueeElement.style.transform = '';
    state.currentX = 0;
    state.currentY = 0;
    state.dragOffset = 0;
    
    restoreAnimation();
  };

  // Handle pointer cancel (touch cancel)
  state.pointerCancelHandler = (e) => {
    if (!state.isDragging || state.disposed) return;
    state.pointerUpHandler(e);
  };

  // Set initial cursor
  state.container.style.cursor = 'grab';

  // Add mouse event listeners
  state.container.addEventListener('mousedown', state.pointerDownHandler, { passive: false });
  document.addEventListener('mousemove', state.pointerMoveHandler, { passive: false });
  document.addEventListener('mouseup', state.pointerUpHandler, { passive: true });

  // Add touch event listeners for mobile support
  state.container.addEventListener('touchstart', state.pointerDownHandler, { passive: false });
  document.addEventListener('touchmove', state.pointerMoveHandler, { passive: false });
  document.addEventListener('touchend', state.pointerUpHandler, { passive: true });
  document.addEventListener('touchcancel', state.pointerCancelHandler, { passive: true });

  return {
    /**
     * Updates the drag orientation.
     * @param {boolean} vertical - Whether to enable vertical dragging
     */
    update(vertical) {
      console.log('drag handler update called', vertical);
      if (state.disposed) return;
      
      state.vertical = Boolean(vertical);
      
      // Reset drag state when switching orientation
      if (state.isDragging) {
        state.pointerUpHandler(new Event('mouseup'));
      }
    },

    /**
     * Disposes the drag handler and cleans up event listeners.
     */
    dispose() {
      console.log('drag handler dispose called');
      if (state.disposed) return;

      state.disposed = true;

      // Clean up any active drag state
      if (state.isDragging) {
        state.container.style.cursor = '';
        state.container.style.userSelect = '';
        state.marqueeElement.style.transform = '';
        restoreAnimation();
      }

      // Remove event listeners
      if (state.container && state.pointerDownHandler) {
        state.container.removeEventListener('mousedown', state.pointerDownHandler);
        state.container.removeEventListener('touchstart', state.pointerDownHandler);
      }

      if (state.pointerMoveHandler) {
        document.removeEventListener('mousemove', state.pointerMoveHandler);
        document.removeEventListener('touchmove', state.pointerMoveHandler);
      }

      if (state.pointerUpHandler) {
        document.removeEventListener('mouseup', state.pointerUpHandler);
        document.removeEventListener('touchend', state.pointerUpHandler);
      }

      if (state.pointerCancelHandler) {
        document.removeEventListener('touchcancel', state.pointerCancelHandler);
      }

      // Reset cursor
      if (state.container) {
        state.container.style.cursor = '';
      }

      state.container = null;
      state.marqueeElement = null;
      state.pointerDownHandler = null;
      state.pointerMoveHandler = null;
      state.pointerUpHandler = null;
      state.pointerCancelHandler = null;
    }
  };
}

/**
 * Sets up drag handler for the marquee.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marqueeElement - Marquee element to manipulate
 * @param {boolean} vertical - Whether to enable vertical dragging
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
export function setupDragHandler(container, marqueeElement, vertical) {
  console.log('setupDragHandler function called');
  return createDragHandler(container, marqueeElement, Boolean(vertical));
}

console.log('Marquee JavaScript module loaded');
