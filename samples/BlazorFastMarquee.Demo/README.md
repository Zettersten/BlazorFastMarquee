# BlazorFastMarquee Demo

This is the demo application showcasing the BlazorFastMarquee component.

## Package

📦 **[BlazorFastMarquee on NuGet](https://www.nuget.org/packages/BlazorFastMarquee/)**

Install via:
```bash
dotnet add package BlazorFastMarquee
```

## Building

### Local Development

For local development (runs at `http://localhost:5000/`):

```bash
dotnet run
```

Or build for local hosting:

```bash
./build-demo.sh local
# or
dotnet publish -p:BaseHref="/"
```

### GitHub Pages

For GitHub Pages deployment (runs at `https://username.github.io/BlazorFastMarquee/`):

```bash
./build-demo.sh github
# or
dotnet publish -p:BaseHref="/BlazorFastMarquee/"
```

## How It Works

The build system uses MSBuild properties to set the correct `<base href>` at build time:

- **Local**: `<base href="/" />` - Works with `dotnet run` and local servers
- **GitHub Pages**: `<base href="/BlazorFastMarquee/" />` - Works with GitHub Pages subpath

### Build-Time Base Href

The `.csproj` file includes an MSBuild target that replaces the base href during the publish process:

```xml
<PropertyGroup>
  <!-- Default base path for local development -->
  <BaseHref Condition="'$(BaseHref)' == ''">/</BaseHref>
</PropertyGroup>

<Target Name="ReplaceBaseHref" AfterTargets="Publish">
  <!-- Replaces base href in published index.html -->
</Target>
```

### Setting Base Href

You can set the base href in multiple ways:

1. **Command line**:
   ```bash
   dotnet publish -p:BaseHref="/BlazorFastMarquee/"
   ```

2. **Build script**:
   ```bash
   ./build-demo.sh github
   ```

3. **CI/CD** (GitHub Actions, etc.):
   ```yaml
   - name: Publish
     run: dotnet publish -p:BaseHref="/BlazorFastMarquee/"
   ```

## Deployment

### GitHub Pages

1. Build for GitHub Pages:
   ```bash
   ./build-demo.sh github
   ```

2. Deploy the contents of `bin/Release/net9.0/publish/wwwroot` to your `gh-pages` branch

3. Or use the provided GitHub Action workflow (if available)

### Other Hosting

For other hosting providers, set the appropriate base href:

```bash
# Azure Static Web Apps
dotnet publish -p:BaseHref="/"

# Netlify subdirectory
dotnet publish -p:BaseHref="/subdirectory/"

# Custom domain root
dotnet publish -p:BaseHref="/"
```

## Development

Run in development mode:

```bash
dotnet watch run
```

This will automatically reload when you make changes to the code.
