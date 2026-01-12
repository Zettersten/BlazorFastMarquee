namespace BlazorFastMarquee;

public partial class Marquee
{
  #region Disposal

  /// <summary>
  /// Disposes resources and cleans up JS interop references.
  /// Handles both WASM and Server scenarios gracefully.
  /// </summary>
  public async ValueTask DisposeAsync()
  {
    if (_isDisposed)
      return;

    _isDisposed = true;

    // Dispose .NET object reference
    _dotNetRef.Dispose();

    // Cleanup JS interop resources
    await DisposeDragHandlerAsync();
    await DisposeAnimationHandlerAsync();
    await DisposeObserverAsync();
    await DisposeModuleAsync();

    _dragHandlerLock.Dispose();
  }

  private async ValueTask DisposeDragHandlerAsync()
  {
    if (_dragHandler is null)
      return;

    var handlerToDispose = _dragHandler;
    _dragHandler = null; // Clear immediately to prevent re-entry

    try
    {
      await handlerToDispose.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }

    try
    {
      await handlerToDispose.DisposeAsync();
    }
    catch
    {
      // Suppress disposal errors
    }
  }

  private async ValueTask DisposeAnimationHandlerAsync()
  {
    if (_animationHandler is null)
      return;

    try
    {
      await _animationHandler.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      try
      {
        await _animationHandler.DisposeAsync();
      }
      catch
      {
        // Suppress disposal errors
      }

      _animationHandler = null;
    }
  }

  private async ValueTask DisposeObserverAsync()
  {
    if (_observer is null)
      return;

    try
    {
      await _observer.InvokeVoidAsync("dispose");
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      try
      {
        await _observer.DisposeAsync();
      }
      catch
      {
        // Suppress disposal errors
      }

      _observer = null;
    }
  }

  private async ValueTask DisposeModuleAsync()
  {
    if (_module is null)
      return;

    try
    {
      await _module.DisposeAsync();
    }
    catch (JSDisconnectedException)
    {
      // Circuit already disconnected - expected
    }
    catch (ObjectDisposedException)
    {
      // Already disposed - expected
    }
    catch
    {
      // Suppress other errors during disposal
    }
    finally
    {
      _module = null;
    }
  }

  #endregion Disposal
}

