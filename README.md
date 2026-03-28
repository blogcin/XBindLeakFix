# XBindLeakFix

[![NuGet](https://img.shields.io/nuget/v/XBindLeakFix)](https://www.nuget.org/packages/XBindLeakFix)

**Automatic build-time fix for the WinUI 3 `x:Bind` memory leak.**

Every WinUI 3 `Window` that uses `x:Bind` leaks memory because the XAML compiler generates an `Activated` event subscription that is never cleaned up. Since `Window` is not a `DependencyObject`, the XAML Reference Tracker can't break the resulting circular reference. This bug has been open since [June 2022](https://github.com/microsoft/microsoft-ui-xaml/issues/7282).

This package patches the generated code at build time. No code changes needed.

## Install

```
dotnet add package XBindLeakFix
```

Or in your `.csproj`:

```xml
<PackageReference Include="XBindLeakFix" Version="1.0.0" />
```

## How it works

On every build:

1. XAML compiler generates `.g.cs` files (with the bug)
2. **XBindLeakFix** patches them (injects cleanup on `Window.Closed`)
3. C# compiler compiles the patched code

Only `Window` types using `x:Bind` are patched. `Page`, `UserControl`, and other `FrameworkElement` types are unaffected.

## Results

```
BEFORE:  x:Bind windows created=5, finalized=0, leaked=5
AFTER:   x:Bind windows created=5, finalized=5, leaked=0
```

## Root cause

The XAML compiler's T4 template (`CSharpPagePass2.tt`) generates:

```csharp
element1.Activated += bindings.Activated;
```

but never generates the corresponding `-=`. For `FrameworkElement` types this is fine (Reference Tracker handles it). For `Window` (not a `DependencyObject`), the circular reference is permanent.

## Related issues

- [microsoft/microsoft-ui-xaml#7282](https://github.com/microsoft/microsoft-ui-xaml/issues/7282) — Memory leak when use x:Bind in WinUI 3
- [microsoft/microsoft-ui-xaml#9960](https://github.com/microsoft/microsoft-ui-xaml/issues/9960) — Window objects do not deregister event handlers
- [microsoft/microsoft-ui-xaml#9063](https://github.com/microsoft/microsoft-ui-xaml/issues/9063) — Memory leak in multi-window applications

## License

MIT
