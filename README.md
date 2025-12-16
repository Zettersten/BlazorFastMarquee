# Blazor Fast Marquee

[![NuGet](https://img.shields.io/nuget/v/BlazorFastMarquee.svg)](https://www.nuget.org/packages/BlazorFastMarquee/)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/Zettersten/BlazorFastMarquee)

Blazor Fast Marquee is a high-performance marquee component for Blazor inspired by the battle-tested [`react-fast-marquee`](https://www.react-fast-marquee.com) library. It delivers the same API surface while embracing .NET's ahead-of-time (AOT) compilation and aggressive trimming so your applications remain lean without sacrificing fidelity.

Built for **.NET 10** with **C# 14** language features, this component is production-ready and fully compatible with the latest Blazor runtime improvements.

> **Why another marquee?** Because shipping to WebAssembly or native ahead-of-time targets demands components that are deterministic, trimming safe, and optimized from the first render. Blazor Fast Marquee was built from the ground up with those goals in mind.

## Live Demo

🚀 **[View the interactive demo](https://zettersten.github.io/BlazorFastMarquee/)**

## Examples

![Sample 1](https://raw.githubusercontent.com/Zettersten/BlazorFastMarquee/refs/heads/main/sample-1.gif)

![Sample 2](https://raw.githubusercontent.com/Zettersten/BlazorFastMarquee/refs/heads/main/sample-2.gif)

![Sample 3](https://raw.githubusercontent.com/Zettersten/BlazorFastMarquee/refs/heads/main/sample-3.gif)

## Highlights

- ⚡ **API parity with React Fast Marquee.** Drop-in familiar parameters such as `speed`, `gradientColor`, `pauseOnHover`, and lifecycle callbacks like `onCycleComplete`.
- 🖱️ **Interactive drag support.** Enable drag-to-pan for desktop and mobile with `EnableDrag`. Automatically respects direction (horizontal for left/right, vertical for up/down). Smart click detection ensures buttons and links work seamlessly inside draggable marquees.
- 🪶 **Trimming-friendly by design.** The library is marked as trimmable, ships without reflection, and has analyzers enabled so you can confidently publish with `PublishTrimmed=true`.
- 🚀 **AOT ready.** `RunAOTCompilation` is enabled so the component is validated against Native AOT constraints during publish.
- 🧭 **Deterministic layout.** A lightweight JavaScript module only measures the rendered width/height and falls back gracefully when observers are unavailable.
- 🧩 **Composable.** Works with any child content—text, components, images, or complex layouts.
- 🎯 **.NET 10 optimized.** Leverages the latest C# 14 language features and .NET 10 runtime improvements for peak performance.

## Getting Started

### Installation

Install the package from NuGet:

```bash
dotnet add package BlazorFastMarquee
```

### Setup

**1. Configure your Blazor app** in `Program.cs`:

For **Blazor WebAssembly**:
```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
// ... rest of your configuration
await builder.Build().RunAsync();
```

For **Blazor Server** or **Blazor Web App**:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents(); // If using WebAssembly interactivity
```

**2. CSS and JavaScript are automatically included** via Blazor's static web assets system. The component's styles (`Marquee.razor.css`) and JavaScript module (`Marquee.razor.js`) are bundled and served automatically—no manual script or link tags required.

**3. Import the namespace** in your `_Imports.razor`:
```razor
@using BlazorFastMarquee
```

### Usage

**Basic example:**

```razor
@page "/marquee-demo"
@using BlazorFastMarquee

<Marquee Speed="80" Gradient GradientColor="#111827" GradientWidth="128">
    <div class="marquee-chip">🚀 Blazor Fast Marquee</div>
    <div class="marquee-chip">🔥 AOT-ready animations</div>
    <div class="marquee-chip">🧭 Deterministic layout</div>
</Marquee>
```

**With drag support:**

```razor
<Marquee Speed="50" EnableDrag="true" Direction="MarqueeDirection.Left" Gradient>
    <div class="marquee-chip">🖱️ Drag me!</div>
    <div class="marquee-chip">👆 Click & hold to pan</div>
    <div class="marquee-chip">📱 Touch support included</div>
</Marquee>
```

Add some styling (optional):

```css
.marquee-chip {
    padding: 0.5rem 1rem;
    margin-right: 1rem;
    border-radius: 9999px;
    background: rgba(59, 130, 246, 0.12);
}
```

## Production Builds with Trimming & AOT

Blazor Fast Marquee is validated with trimming analyzers and Native AOT so you can ship the smallest possible payloads. When publishing your application run:

```bash
 dotnet publish -c Release -p:PublishTrimmed=true -p:TrimMode=link -p:RunAOTCompilation=true
```

The library opts into invariant globalization to minimize ICU payload size. If your app requires full globalization data, override `InvariantGlobalization` in your project file.

## Props

| Parameter         | Type                                   | Default  | Description |
| :---------------- | :------------------------------------- | :------- | :---------- |
| `Style`           | `string?`                              | `string.Empty` | Inline style for the container `<div>`.
| `ClassName`       | `string?`                              | `string.Empty` | Additional CSS class names for the container `<div>`.
| `AutoFill`        | `bool`                                 | `false`  | Automatically duplicates content to fill unused marquee space.
| `Play`            | `bool`                                 | `true`   | Whether the marquee animation is running.
| `PauseOnHover`    | `bool`                                 | `false`  | Pause the marquee while the pointer hovers over it.
| `PauseOnClick`    | `bool`                                 | `false`  | Pause the marquee when the container is pressed.
| `EnableDrag`      | `bool`                                 | `false`  | Enable drag-to-pan functionality. Users can click/touch and drag to manually pan the marquee (respects direction). Animation pauses during drag.
| `Direction`       | `MarqueeDirection` (`Left`, `Right`, `Up`, `Down`) | `MarqueeDirection.Left` | Direction of travel. Vertical marquees rotate content to maintain orientation.
| `Speed`           | `double`                               | `50`     | Speed in pixels per second.
| `Delay`           | `double`                               | `0`      | Delay in seconds before the animation starts.
| `Loop`            | `int`                                  | `0`      | Number of times the marquee loops. `0` means infinite.
| `Gradient`        | `bool`                                 | `false`  | Enables a fade gradient overlay at the edges.
| `GradientColor`   | `string`                               | `"white"` | Color used by the gradient overlay.
| `GradientWidth`   | `CssLength` (`double`, `int`, or `string`) | `200` | Width of the gradient on both sides.
| `OnFinish`        | `EventCallback`                        | `default` | Invoked after the marquee finishes all loops (`Loop > 0`).
| `OnCycleComplete` | `EventCallback`                        | `default` | Invoked at the end of each animation cycle.
| `OnMount`         | `EventCallback`                        | `default` | Invoked once after the marquee has measured its layout.
| `ChildContent`    | `RenderFragment?`                      | `null`   | The content rendered inside the marquee.

## Accessibility & Performance Tips

- Prefer semantic markup inside `ChildContent` and provide `aria-label`s when the marquee conveys essential information.
- Keep `Speed` within a comfortable range and consider offering controls to pause for accessibility compliance.
- Combine marquees with `prefers-reduced-motion` to disable animations for users who request it.

## Testing

The solution includes automated BUnit tests covering layout measurement, callbacks, and attribute forwarding. Run them locally with:

```bash
 dotnet test
```

## License

MIT
