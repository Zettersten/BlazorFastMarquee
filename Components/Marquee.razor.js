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
 * @param {HTMLElement} marqueeElement - First marquee element (used for reference)
 * @param {boolean} vertical - Whether to enable vertical dragging
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
function createDragHandler(container, marqueeElement, vertical) {
  console.log('createDragHandler called', { container, marqueeElement, vertical });

  if (!container || !marqueeElement) {
    console.log('createDragHandler: missing container or marqueeElement');
    return null;
  }

  // Get all marquee elements (there are always 2 for seamless looping)
  const marqueeElements = container.querySelectorAll('.bfm-marquee');
  
  const state = {
    container,
    marqueeElements: Array.from(marqueeElements),
    vertical: Boolean(vertical),
    disposed: false,
    isDragging: false,
    startX: 0,
    startY: 0,
    currentX: 0,
    currentY: 0,
    dragOffset: 0,
    persistentOffset: 0, // Cumulative offset that persists between drags
    baseTransforms: [], // Store the animation's current transform when drag starts
    originalPlayStates: [],
    pointerDownHandler: null,
    pointerMoveHandler: null,
    pointerUpHandler: null,
    pointerCancelHandler: null
  };

  // Store original animation play state and current transform for all marquee elements
  const savePlayState = () => {
    state.originalPlayStates = [];
    state.baseTransforms = [];
    
    state.marqueeElements.forEach(el => {
      const computedStyle = window.getComputedStyle(el);
      state.originalPlayStates.push(computedStyle.animationPlayState);
      
      // Get the current transform from the animation
      const transform = computedStyle.transform;
      console.log('Saved base transform:', transform);
      state.baseTransforms.push(transform);
    });
  };

  // Pause animation during drag for all marquee elements
  const pauseAnimation = () => {
    state.marqueeElements.forEach(el => {
      el.style.animationPlayState = 'paused';
    });
  };

  // Restore animation after drag for all marquee elements
  const restoreAnimation = () => {
    state.marqueeElements.forEach((el, index) => {
      if (state.originalPlayStates[index]) {
        el.style.animationPlayState = state.originalPlayStates[index];
      }
    });
  };

  // Apply transform during active drag
  const applyDragTransform = () => {
    const totalOffset = state.persistentOffset + state.dragOffset;
    console.log('Applying drag transform, total offset:', totalOffset);
    
    state.marqueeElements.forEach((el, index) => {
      const baseTransform = state.baseTransforms[index] || 'none';
      
      // Parse the base transform matrix to get current translation
      let baseX = 0, baseY = 0;
      if (baseTransform !== 'none') {
        const matrixMatch = baseTransform.match(/matrix\(([^)]+)\)/);
        if (matrixMatch) {
          const values = matrixMatch[1].split(',').map(v => parseFloat(v.trim()));
          baseX = values[4] || 0;
          baseY = values[5] || 0;
        }
      }
      
      // Apply total offset (persistent + current drag) on top of base transform
      const newX = baseX + (state.vertical ? 0 : totalOffset);
      const newY = baseY + (state.vertical ? totalOffset : 0);
      
      const combinedTransform = `translate(${newX}px, ${newY}px)`;
      
      // Apply with !important to override animation during drag
      el.style.setProperty('transform', combinedTransform, 'important');
    });
  };

  // Apply persistent offset after drag ends (allows animation to resume)
  const applyPersistentOffset = () => {
    console.log('Applying persistent offset:', state.persistentOffset);
    
    state.marqueeElements.forEach(el => {
      if (state.persistentOffset === 0) {
        // No offset, remove the custom property
        el.style.removeProperty('--drag-offset-x');
        el.style.removeProperty('--drag-offset-y');
      } else {
        // Set CSS variables for persistent offset
        if (state.vertical) {
          el.style.setProperty('--drag-offset-x', '0px');
          el.style.setProperty('--drag-offset-y', `${state.persistentOffset}px`);
        } else {
          el.style.setProperty('--drag-offset-x', `${state.persistentOffset}px`);
          el.style.setProperty('--drag-offset-y', '0px');
        }
      }
    });
  };

  // Handle pointer down (mouse/touch start)
  state.pointerDownHandler = (e) => {
    if (state.disposed) return;

    console.log('Pointer down event triggered');

    // Support both mouse and touch events
    const clientX = e.type.includes('touch') ? e.touches[0].clientX : e.clientX;
    const clientY = e.type.includes('touch') ? e.touches[0].clientY : e.clientY;

    console.log('Start position:', { clientX, clientY });

    state.isDragging = true;
    state.startX = clientX - state.currentX;
    state.startY = clientY - state.currentY;

    savePlayState();
    pauseAnimation();

    // Add grabbing cursor
    state.container.style.cursor = 'grabbing';
    state.container.style.userSelect = 'none';

    console.log('Drag state initialized, isDragging:', state.isDragging);

    // Prevent text selection during drag
    e.preventDefault();
  };

  // Handle pointer move (mouse/touch move)
  state.pointerMoveHandler = (e) => {
    if (!state.isDragging || state.disposed) {
      if (!state.isDragging) console.log('Move event but not dragging');
      return;
    }

    console.log('Pointer move event, isDragging:', state.isDragging);

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

    console.log('Calculated drag offset:', state.dragOffset, { currentX: state.currentX, currentY: state.currentY });

    applyDragTransform();
    e.preventDefault();
  };

  // Handle pointer up (mouse/touch end)
  state.pointerUpHandler = (e) => {
    if (!state.isDragging || state.disposed) return;

    console.log('Pointer up - ending drag, final offset:', state.dragOffset);

    state.isDragging = false;
    
    // Reset cursor
    state.container.style.cursor = 'grab';
    state.container.style.userSelect = '';

    // Add current drag offset to persistent offset
    state.persistentOffset += state.dragOffset;
    console.log('New persistent offset:', state.persistentOffset);

    // Reset current drag state
    state.currentX = 0;
    state.currentY = 0;
    state.dragOffset = 0;
    state.baseTransforms = [];

    // Remove inline transform (applied during drag)
    state.marqueeElements.forEach(el => {
      el.style.removeProperty('transform');
    });

    // Apply persistent offset via CSS variables
    applyPersistentOffset();
    
    // Restore animation - it will continue from the new position
    restoreAnimation();
    
    console.log('Drag ended, animation resumed from new position');
  };

  // Handle pointer cancel (touch cancel)
  state.pointerCancelHandler = (e) => {
    if (!state.isDragging || state.disposed) return;
    state.pointerUpHandler(e);
  };

  // Set initial cursor
  state.container.style.cursor = 'grab';

  console.log('Adding event listeners to container and document');

  // Add mouse event listeners
  state.container.addEventListener('mousedown', state.pointerDownHandler, { passive: false });
  document.addEventListener('mousemove', state.pointerMoveHandler, { passive: false });
  document.addEventListener('mouseup', state.pointerUpHandler, { passive: true });

  console.log('Mouse event listeners added');

  // Add touch event listeners for mobile support
  state.container.addEventListener('touchstart', state.pointerDownHandler, { passive: false });
  document.addEventListener('touchmove', state.pointerMoveHandler, { passive: false });
  document.addEventListener('touchend', state.pointerUpHandler, { passive: true });
  document.addEventListener('touchcancel', state.pointerCancelHandler, { passive: true });

  console.log('Touch event listeners added');
  console.log('Drag handler setup complete for', state.marqueeElements.length, 'marquee elements');

  return {
    /**
     * Updates the drag orientation.
     * @param {boolean} vertical - Whether to enable vertical dragging
     */
    update(vertical) {
      console.log('drag handler update called', vertical);
      if (state.disposed) return;
      
      const wasVertical = state.vertical;
      state.vertical = Boolean(vertical);
      
      // If orientation changed, reset persistent offset
      if (wasVertical !== state.vertical) {
        console.log('Orientation changed, resetting persistent offset');
        state.persistentOffset = 0;
        applyPersistentOffset();
      }
      
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
        state.marqueeElements.forEach(el => {
          el.style.removeProperty('transform');
        });
        restoreAnimation();
      }

      // Clean up persistent offset
      state.persistentOffset = 0;
      state.marqueeElements.forEach(el => {
        el.style.removeProperty('--drag-offset-x');
        el.style.removeProperty('--drag-offset-y');
      });

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
 * @returns {Object|null} Drag handler or null if invalid parameters
 */
export function setupDragHandler(container, marqueeElement, vertical) {
  console.log('setupDragHandler function called');
  return createDragHandler(container, marqueeElement, Boolean(vertical));
}

console.log('Marquee JavaScript module loaded');
