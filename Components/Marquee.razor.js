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
  const marqueeElements = container.querySelectorAll('.bfm-marquee');
  
  const state = {
    container,
    marqueeElements: Array.from(marqueeElements),
    vertical: Boolean(vertical),
    reversed: Boolean(reversed),
    disposed: false,
    isDragging: false,
    hasMoved: false,
    startX: 0,
    startY: 0,
    lastX: 0,
    lastY: 0,
    pointerDownHandler: null,
    pointerMoveHandler: null,
    pointerUpHandler: null,
    pointerCancelHandler: null
  };

  // Drag threshold in pixels - movement less than this is considered a click
  const DRAG_THRESHOLD = 5;

  // Get marquee width/height for percentage calculations
  const getMarqueeSize = () => {
    if (state.marqueeElements.length > 0) {
      const rect = state.marqueeElements[0].getBoundingClientRect();
      return state.vertical ? rect.height : rect.width;
    }
    return 1; // Avoid division by zero
  };

  // Handle pointer down (mouse/touch start)
  state.pointerDownHandler = (e) => {
    if (state.disposed) return;

    const clientX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
    const clientY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;

    state.isDragging = true;
    state.hasMoved = false;
    state.startX = clientX;
    state.startY = clientY;
    state.lastX = clientX;
    state.lastY = clientY;

    // Don't prevent default yet - allow clicks to work
    // We'll only prevent default once actual dragging starts
  };

  // Handle pointer move (mouse/touch move)
  state.pointerMoveHandler = (e) => {
    if (!state.isDragging || state.disposed) return;

    const clientX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
    const clientY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;

    // Calculate total movement from start position
    const totalDeltaX = Math.abs(clientX - state.startX);
    const totalDeltaY = Math.abs(clientY - state.startY);
    const totalMovement = state.vertical ? totalDeltaY : totalDeltaX;

    // Check if we've moved beyond the drag threshold
    if (!state.hasMoved && totalMovement > DRAG_THRESHOLD) {
      state.hasMoved = true;
      
      // Now we're actually dragging - pause animation and update UI
      state.marqueeElements.forEach(el => {
        el.style.animationPlayState = 'paused';
      });
      state.container.style.cursor = 'grabbing';
      state.container.style.userSelect = 'none';
    }

    // Only process drag if we've moved beyond threshold
    if (state.hasMoved) {
      // Calculate movement delta
      let deltaX = clientX - state.lastX;
      let deltaY = clientY - state.lastY;
      
      // Invert delta for reversed directions (Right/Up)
      // This makes dragging feel natural with the animation direction
      if (state.reversed) {
        deltaX = -deltaX;
        deltaY = -deltaY;
      }
      
      const delta = state.vertical ? deltaY : deltaX;

      state.lastX = clientX;
      state.lastY = clientY;

      // Convert delta to percentage of marquee size
      const marqueeSize = getMarqueeSize();
      const percentChange = (delta / marqueeSize) * 100;

      // Scrub through the animation by adjusting each element's animation progress
      state.marqueeElements.forEach(el => {
        // Get current animation state
        const animations = el.getAnimations();
        if (animations.length > 0) {
          const anim = animations[0];
          const duration = anim.effect.getTiming().duration;
          
          // Current time in the animation (milliseconds)
          let currentTime = anim.currentTime || 0;
          
          // Adjust current time based on drag
          // Dragging right/down (positive delta) = rewind (go backwards in time)
          // Dragging left/up (negative delta) = advance (go forwards in time)
          currentTime -= (percentChange / 100) * duration;
          
          // Wrap around the animation duration
          if (duration > 0) {
            currentTime = ((currentTime % duration) + duration) % duration;
          }
          
          anim.currentTime = currentTime;
        }
      });

      // Prevent default only when actually dragging
      e.preventDefault();
    }
  };

  // Handle pointer up (mouse/touch end)
  state.pointerUpHandler = (e) => {
    if (!state.isDragging || state.disposed) return;

    state.isDragging = false;
    
    // Only restore cursor and resume animation if we actually dragged
    if (state.hasMoved) {
      state.container.style.cursor = 'grab';
      state.container.style.userSelect = '';

      // Resume animation from wherever we scrubbed to
      state.marqueeElements.forEach(el => {
        el.style.animationPlayState = '';
      });
    }
    
    state.hasMoved = false;
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
     * Updates the drag orientation and direction.
     */
    update(vertical, reversed) {
      if (state.disposed) return;
      state.vertical = Boolean(vertical);
      state.reversed = Boolean(reversed);
      
      // End any active drag when orientation changes
      if (state.isDragging) {
        state.pointerUpHandler(new Event('mouseup'));
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

      // Remove cursor and selection styles
      if (state.container) {
        state.container.style.removeProperty('cursor');
        state.container.style.removeProperty('user-select');
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

      // Clear all references
      state.container = null;
      state.marqueeElements = [];
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
 * @param {boolean} reversed - Whether the animation direction is reversed (Right/Up)
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
export function setupDragHandler(container, marqueeElement, vertical, reversed) {
  return createDragHandler(container, marqueeElement, Boolean(vertical), Boolean(reversed));
}
