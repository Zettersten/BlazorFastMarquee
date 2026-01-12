/**
 * High-performance marquee measurement and observation module.
 * Optimized for minimal allocations and efficient DOM operations.
 */

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
    .catch(() => {});
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
 * Sets up animation event listeners for marquee element.
 * @param {HTMLElement} marqueeElement - The marquee animation element
 * @param {Object} dotnetRef - .NET object reference for callbacks
 * @returns {Object} Animation handler with dispose method
 */
function createAnimationHandler(marqueeElement, dotnetRef) {
  if (!marqueeElement || !dotnetRef) {
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
    if (state.disposed || !state.dotnetRef) return;

    state.dotnetRef
      .invokeMethodAsync('HandleAnimationIteration')
      .catch(() => {});
  };

  // Animation end event handler
  state.endHandler = () => {
    if (state.disposed || !state.dotnetRef) return;

    state.dotnetRef
      .invokeMethodAsync('HandleAnimationEnd')
      .catch(() => {});
  };

  // Add event listeners
  marqueeElement.addEventListener('animationiteration', state.iterationHandler, { passive: true });
  marqueeElement.addEventListener('animationend', state.endHandler, { passive: true });

  return {
    /**
     * Disposes the animation event listeners.
     */
    dispose() {
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

/**
 * Sets up animation event listeners for the marquee element.
 * @param {HTMLElement} marqueeElement - The marquee animation element
 * @param {Object} dotnetRef - .NET object reference for callbacks
 * @returns {Object|null} Animation handler or null if invalid parameters
 */
export function setupAnimationEvents(marqueeElement, dotnetRef) {
  return createAnimationHandler(marqueeElement, dotnetRef);
}

/**
 * Creates a drag handler for pan/drag functionality.
 * Scrubs through the animation timeline based on drag movement.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marqueeElement - First marquee element (used for reference)
 * @param {boolean} vertical - Whether to enable vertical dragging
 * @param {boolean} reversed - Whether the animation direction is reversed (Right/Up)
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
function createDragHandler(container, marqueeElement, vertical, reversed) {
  if (!container || !marqueeElement) {
    return null;
  }

  // Get all marquee elements (there are always 2 for seamless looping)
  const marqueeElements = Array.from(container.querySelectorAll('.bfm-marquee'));

  const state = {
    container,
    marqueeElements,
    vertical: Boolean(vertical),
    reversed: Boolean(reversed),
    disposed: false,
    pointerId: null,
    isDragging: false,
    hasMoved: false, // moved beyond threshold (true drag vs click)
    suppressClick: false,
    startX: 0,
    startY: 0,
    lastX: 0,
    lastY: 0,
    marqueeSize: 1,
    // cached animations for this drag gesture (avoid allocations in move handler)
    animations: [],
    pointerDownHandler: null,
    pointerMoveHandler: null,
    pointerUpHandler: null,
    pointerCancelHandler: null,
    clickCaptureHandler: null
  };

  // Drag threshold in pixels - movement less than this is considered a click
  const DRAG_THRESHOLD = 5;

  const updateTouchAction = () => {
    // Allow page scrolling in the orthogonal direction while draggable.
    // Horizontal marquee => allow vertical scroll; Vertical marquee => allow horizontal scroll.
    state.container.style.touchAction = state.vertical ? 'pan-x' : 'pan-y';
  };

  const captureAnimations = () => {
    state.animations.length = 0;
    for (let i = 0; i < state.marqueeElements.length; i++) {
      const el = state.marqueeElements[i];
      const anims = el.getAnimations();
      const anim = anims && anims.length > 0 ? anims[0] : null;
      const duration = anim && anim.effect && anim.effect.getTiming ? anim.effect.getTiming().duration : 0;
      state.animations.push({ anim, duration });
    }

    if (state.marqueeElements.length > 0) {
      const rect = state.marqueeElements[0].getBoundingClientRect();
      const size = state.vertical ? rect.height : rect.width;
      state.marqueeSize = size > 0 ? size : 1;
    } else {
      state.marqueeSize = 1;
    }
  };

  state.clickCaptureHandler = (e) => {
    if (!state.suppressClick) return;
    // A drag gesture just occurred; suppress the synthetic click.
    e.preventDefault();
    e.stopImmediatePropagation();
    state.suppressClick = false;
  };

  state.pointerDownHandler = (e) => {
    if (state.disposed) return;

    // Only left mouse button initiates drag; touch/pen are always ok.
    if (e.pointerType === 'mouse' && e.button !== 0) return;

    state.pointerId = e.pointerId;
    state.isDragging = true;
    state.hasMoved = false;
    state.suppressClick = false;

    state.startX = e.clientX;
    state.startY = e.clientY;
    state.lastX = e.clientX;
    state.lastY = e.clientY;

    captureAnimations();

    // Capture pointer so we keep receiving move/up events.
    try { state.container.setPointerCapture(e.pointerId); } catch (_) {}
  };

  state.pointerMoveHandler = (e) => {
    if (state.disposed || !state.isDragging || state.pointerId !== e.pointerId) return;

    const clientX = e.clientX;
    const clientY = e.clientY;

    const totalDeltaX = Math.abs(clientX - state.startX);
    const totalDeltaY = Math.abs(clientY - state.startY);
    const totalMovement = state.vertical ? totalDeltaY : totalDeltaX;

    if (!state.hasMoved && totalMovement > DRAG_THRESHOLD) {
      state.hasMoved = true;
      state.suppressClick = true;

      state.container.classList.add('bfm-dragging');

      // Pause animation while dragging.
      for (let i = 0; i < state.marqueeElements.length; i++) {
        state.marqueeElements[i].style.animationPlayState = 'paused';
      }
    }

    if (!state.hasMoved) return;

    let deltaX = clientX - state.lastX;
    let deltaY = clientY - state.lastY;

    if (state.reversed) {
      deltaX = -deltaX;
      deltaY = -deltaY;
    }

    const delta = state.vertical ? deltaY : deltaX;
    state.lastX = clientX;
    state.lastY = clientY;

    const size = state.marqueeSize || 1;

    // Scrub through each element's animation timeline (no getAnimations/getTiming in hot path)
    for (let i = 0; i < state.animations.length; i++) {
      const entry = state.animations[i];
      const anim = entry.anim;
      const duration = entry.duration;
      if (!anim || !duration || duration <= 0) continue;

      let currentTime = anim.currentTime || 0;
      currentTime -= (delta / size) * duration;

      // Wrap around [0, duration)
      currentTime = ((currentTime % duration) + duration) % duration;
      anim.currentTime = currentTime;
    }

    // Prevent scrolling while actively dragging.
    e.preventDefault();
  };

  state.pointerUpHandler = (e) => {
    if (state.disposed || !state.isDragging || state.pointerId !== e.pointerId) return;

    state.isDragging = false;

    if (state.hasMoved) {
      state.container.classList.remove('bfm-dragging');

      // Resume animation from wherever we scrubbed to.
      for (let i = 0; i < state.marqueeElements.length; i++) {
        state.marqueeElements[i].style.animationPlayState = '';
      }
    }

    state.hasMoved = false;
    state.pointerId = null;

    try { state.container.releasePointerCapture(e.pointerId); } catch (_) {}
  };

  state.pointerCancelHandler = (e) => {
    if (state.disposed) return;
    state.pointerUpHandler(e);
  };

  // Initial configuration
  state.container.classList.add('bfm-draggable');
  updateTouchAction();

  // Pointer events unify mouse/touch/pen and avoid document-level listeners.
  state.container.addEventListener('pointerdown', state.pointerDownHandler, { passive: true });
  state.container.addEventListener('pointermove', state.pointerMoveHandler, { passive: false });
  state.container.addEventListener('pointerup', state.pointerUpHandler, { passive: true });
  state.container.addEventListener('pointercancel', state.pointerCancelHandler, { passive: true });
  // Capture-phase click suppression only when a drag happened.
  state.container.addEventListener('click', state.clickCaptureHandler, true);

  return {
    /**
     * Updates the drag orientation and direction.
     */
    update(vertical, reversed) {
      if (state.disposed) return;
      state.vertical = Boolean(vertical);
      state.reversed = Boolean(reversed);
      updateTouchAction();
      
      // End any active drag when orientation changes
      if (state.isDragging) {
        try {
          // Best-effort termination; pointerup won't fire if capture is lost in some cases.
          state.isDragging = false;
          state.hasMoved = false;
          state.pointerId = null;
          state.container.classList.remove('bfm-dragging');
          for (let i = 0; i < state.marqueeElements.length; i++) {
            state.marqueeElements[i].style.animationPlayState = '';
          }
        } catch (_) {}
      }
    },

    /**
     * Disposes the drag handler and cleans up event listeners.
     */
    dispose() {
      if (state.disposed) return;
      state.disposed = true;

      // Clean up active drag state
      if (state.isDragging) {
        state.isDragging = false;
        state.marqueeElements.forEach(el => {
          el?.style.removeProperty('animation-play-state');
        });
      }

      if (state.container) {
        state.container.classList.remove('bfm-dragging');
        state.container.classList.remove('bfm-draggable');
        state.container.style.removeProperty('touch-action');
      }

      // Remove event listeners
      if (state.container && state.pointerDownHandler) {
        state.container.removeEventListener('pointerdown', state.pointerDownHandler);
        state.container.removeEventListener('pointermove', state.pointerMoveHandler);
        state.container.removeEventListener('pointerup', state.pointerUpHandler);
        state.container.removeEventListener('pointercancel', state.pointerCancelHandler);
      }

      if (state.container && state.clickCaptureHandler) {
        state.container.removeEventListener('click', state.clickCaptureHandler, true);
      }

      // Clear all references
      state.container = null;
      state.marqueeElements = [];
      state.pointerDownHandler = null;
      state.pointerMoveHandler = null;
      state.pointerUpHandler = null;
      state.pointerCancelHandler = null;
      state.clickCaptureHandler = null;
      state.animations = [];
    }
  };
}

/**
 * Sets up drag handler for the marquee.
 * @param {HTMLElement} container - Container element
 * @param {HTMLElement} marqueeElement - Marquee element to manipulate
 * @param {boolean} vertical - Whether to enable vertical dragging
 * @param {boolean} reversed - Whether the animation direction is reversed (Right/Up)
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
export function setupDragHandler(container, marqueeElement, vertical, reversed) {
  return createDragHandler(container, marqueeElement, Boolean(vertical), Boolean(reversed));
}
