# BlazorFastMarquee Demo Application

A comprehensive showcase application demonstrating all the capabilities of the BlazorFastMarquee component library.

## 🚀 Quick Start

### View Live Demo

The demo is automatically deployed to GitHub Pages: **[View Live Demo](https://Zettersten.github.io/BlazorFastMarquee/)**

### Running Locally

**Prerequisites:**
- .NET 9.0 SDK or later

**Steps:**
```bash
cd samples/BlazorFastMarquee.Demo
dotnet run
```

Then open your browser to `https://localhost:5001` (or the URL shown in the console).

### Deploying to GitHub Pages

**⚠️ Important:** Before the GitHub Actions workflow can deploy successfully, GitHub Pages must be configured:

1. Go to **Repository Settings → Pages**
2. Under "Build and deployment" → "Source"
3. Select **"GitHub Actions"**

For detailed setup instructions, see [`.github/PAGES_SETUP.md`](../../.github/PAGES_SETUP.md)

The demo automatically deploys to GitHub Pages on every push to `main` that affects the demo or component files.

## 📑 Pages Overview

The demo application includes multiple pages showcasing different features and use cases:

### Core Features
- **Home** - Introduction with hero showcase and basic examples
- **Directions** - All 4 scrolling directions (left, right, up, down)
- **Speed & Timing** - Various speed settings, delays, and loop controls
- **AutoFill** - Automatic content duplication to fill container width
- **Gradients** - Gradient overlay effects and configurations
- **Interactive** - Pause on hover/click and programmatic playback control

### Real-World Use Cases
- **News Ticker** - Breaking news, sports scores, financial data, weather, and event announcements
- **Testimonials** - Customer reviews, social proof, and testimonial carousels
- **Logo Showcase** - Partner logos, client lists, and brand showcases
- **Image Gallery** - Photo galleries, product showcases, and image carousels

## 🎨 Features Demonstrated

### Animation Control
- ✅ Variable speed settings (25-200 px/s)
- ✅ Animation delays
- ✅ Loop count control
- ✅ Play/pause states
- ✅ All 4 directions (left, right, up, down)

### Visual Effects
- ✅ Gradient overlays with customizable colors
- ✅ Adjustable gradient widths
- ✅ Smooth edge fading
- ✅ Responsive layouts

### Interactivity
- ✅ Pause on hover
- ✅ Pause on click
- ✅ Programmatic control with buttons/sliders
- ✅ Dynamic speed adjustment

### Performance Features
- ✅ Zero allocation design
- ✅ AutoFill mode for seamless scrolling
- ✅ Responsive to window resizing
- ✅ GPU-accelerated CSS animations

## 🏗️ Project Structure

```
BlazorFastMarquee.Demo/
├── Components/
│   ├── Layout/
│   │   └── MainLayout.razor        # Main layout with sidebar navigation
│   ├── Pages/
│   │   ├── Home.razor              # Landing page
│   │   ├── Directions.razor        # Direction examples
│   │   ├── Speed.razor             # Speed & timing examples
│   │   ├── AutoFill.razor          # AutoFill mode examples
│   │   ├── Gradients.razor         # Gradient effects
│   │   ├── Interactive.razor       # Interactivity examples
│   │   ├── NewsTicker.razor        # News ticker use cases
│   │   ├── Testimonials.razor      # Testimonial displays
│   │   ├── Logos.razor             # Logo showcases
│   │   └── Gallery.razor           # Image galleries
│   ├── App.razor                   # Root component
│   ├── Routes.razor                # Routing configuration
│   └── _Imports.razor              # Global imports
├── wwwroot/
│   └── css/
│       └── app.css                 # Global styles
├── Program.cs                      # Application entry point
└── BlazorFastMarquee.Demo.csproj  # Project file
```

## 🎯 Learning Path

1. **Start with Home** - Understand basic usage and see the component in action
2. **Explore Core Features** - Learn about directions, speeds, gradients, and interactivity
3. **Study Use Cases** - See real-world implementations for inspiration
4. **Experiment** - All examples support pause-on-hover, so examine the code and behavior

## 💡 Code Examples

### Basic Marquee
```razor
<Marquee>
    <span>Your scrolling content here</span>
</Marquee>
```

### With All Options
```razor
<Marquee 
    Speed="50"
    Direction="MarqueeDirection.Left"
    Delay="0"
    Loop="0"
    Gradient="true"
    GradientColor="#ffffff"
    GradientWidth="200"
    AutoFill="true"
    PauseOnHover="true"
    PauseOnClick="false"
    Play="true">
    <div>Your content</div>
</Marquee>
```

### Vertical Scrolling
```razor
<Marquee Direction="MarqueeDirection.Up" Speed="30">
    <div class="vertical-items">
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
    </div>
</Marquee>
```

## 🔧 Customization

All pages include inline styles that can be extracted to separate CSS files. The component is fully customizable through:
- CSS classes on the Marquee component
- Inline styles on container and content elements
- CSS variables for animations
- Standard Blazor component parameters

## 📚 Additional Resources

- [Main Project Repository](/)
- [Component Documentation](../../README.md)
- [Test Suite](../../tests/BlazorFastMarquee.Tests/)

## 🙏 Acknowledgments

This demo showcases the BlazorFastMarquee component, a high-performance, zero-allocation marquee component for Blazor applications.

---

**Built with ❤️ using Blazor and .NET 9.0**
