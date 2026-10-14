using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia render target that renders to a framebuffer surface. No gpu acceleration available.
    /// </summary>
    internal class FramebufferRenderTarget : IRenderTarget
    {
        private readonly bool _useScaledDrawing;
        private readonly ParallelRaster? _parallel = ParallelRaster.Create();
        private SKImageInfo _currentImageInfo;
        private IntPtr _currentFramebufferAddress;
        private SKSurface? _framebufferSurface;
        private PixelFormatConversionShim? _conversionShim;
        private IDisposable? _preFramebufferCopyHandler;
        private IFramebufferRenderTarget? _renderTarget;
        private bool _hadConversionShim;

        /// <summary>
        /// Create new framebuffer render target using a target surface.
        /// </summary>
        /// <param name="platformSurface">Target surface.</param>
        /// <param name="useScaledDrawing">Tells the render target to scale to framebuffer dpi</param>
        public FramebufferRenderTarget(IFramebufferPlatformSurface platformSurface, bool useScaledDrawing = false)
        {
            _useScaledDrawing = useScaledDrawing;
            var fpsSetting = Environment.GetEnvironmentVariable("AVALONIA_RENDER_FPS");
            if (fpsSetting != null)
            {
                if (!int.TryParse(fpsSetting, out var fps) || fps < 1 || fps > 120)
                    throw new ArgumentException("AVALONIA_RENDER_FPS must be 1..120.");
                if ((AvaloniaLocator.Current.GetService<IRenderLoop>() as DefaultRenderLoop)?.Timer is SleepLoopRenderTimer timer)
                {
                    timer.FrameRateLimit = fps;
                    Console.WriteLine($"RASTER CLOCK fps={timer.DesiredFps}");
                }
            }
            _renderTarget = platformSurface.CreateFramebufferRenderTarget();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
            FreeSurface();
            _parallel?.Dispose();
        }

        public RenderTargetProperties Properties => new()
        {
            RetainsPreviousFrameContents = !_hadConversionShim
                                           && _renderTarget?.RetainsFrameContents == true,
            IsSuitableForDirectRendering = true
        };

        public PlatformRenderTargetState PlatformRenderTargetState =>
            _renderTarget?.State ?? PlatformRenderTargetState.Disposed;

        /// <inheritdoc />
        public IDrawingContextImpl CreateDrawingContext(IRenderTarget.RenderTargetSceneInfo sceneInfo,
            out RenderTargetDrawingContextProperties properties)
        {
            if (_renderTarget == null)
                throw new ObjectDisposedException(nameof(FramebufferRenderTarget));
            
            var framebuffer = _renderTarget.Lock(sceneInfo, out var lockProperties);
            
            SKPictureRecorder? recorder = null;
            try
            {
                var framebufferImageInfo = new SKImageInfo(framebuffer.Size.Width, framebuffer.Size.Height,
                    framebuffer.Format.ToSkColorType(),
                    framebuffer.AlphaFormat.ToSkAlphaType());

                CreateSurface(framebufferImageInfo, framebuffer);
                _hadConversionShim |= _conversionShim != null;

                var canvas = _framebufferSurface.Canvas;

                canvas.RestoreToCount(-1);
                canvas.Save();
                canvas.ResetMatrix();

                var createInfo = new DrawingContextImpl.CreateInfo
                {
                    Surface = _framebufferSurface,
                    Dpi = framebuffer.Dpi,
                    ScaleDrawingToDpi = _useScaledDrawing
                };

                var useParallel = _parallel != null && _conversionShim == null && _renderTarget.RetainsFrameContents;
                properties = new()
                {
                    PreviousFrameIsRetained = !useParallel && !_hadConversionShim && lockProperties.PreviousFrameIsRetained
                };

                if (useParallel)
                {
                    recorder = new SKPictureRecorder();
                    createInfo.Canvas = recorder.BeginRecording(new SKRect(0, 0, framebuffer.Size.Width, framebuffer.Size.Height), true);
                    return new DrawingContextImpl(createInfo, Disposable.Create(() =>
                    {
                        try
                        {
                            using var picture = recorder.EndRecording();
                            _parallel!.Render(picture, framebufferImageInfo, framebuffer.Address, framebuffer.RowBytes);
                        }
                        finally
                        {
                            recorder.Dispose();
                            framebuffer.Dispose();
                        }
                    }));
                }
                return new DrawingContextImpl(createInfo, _preFramebufferCopyHandler, canvas, framebuffer);
            }
            catch
            {
                recorder?.Dispose();
                framebuffer.Dispose();
                throw;
            }
        }
        

        /// <summary>
        /// Check if two images info are compatible.
        /// </summary>
        /// <param name="currentImageInfo">Current.</param>
        /// <param name="desiredImageInfo">Desired.</param>
        /// <returns>True, if images are compatible.</returns>
        private static bool AreImageInfosCompatible(SKImageInfo currentImageInfo, SKImageInfo desiredImageInfo)
        {
            return currentImageInfo.Width == desiredImageInfo.Width &&
                   currentImageInfo.Height == desiredImageInfo.Height &&
                   currentImageInfo.ColorType == desiredImageInfo.ColorType;
        }

        /// <summary>
        /// Create Skia surface backed by given framebuffer.
        /// </summary>
        /// <param name="desiredImageInfo">Desired image info.</param>
        /// <param name="framebuffer">Backing framebuffer.</param>
        [MemberNotNull(nameof(_framebufferSurface))]
        private void CreateSurface(SKImageInfo desiredImageInfo, ILockedFramebuffer framebuffer)
        {
            if (_framebufferSurface != null && AreImageInfosCompatible(_currentImageInfo, desiredImageInfo) && _currentFramebufferAddress == framebuffer.Address)
            {
                return;
            }
            
            FreeSurface();
            
            _currentFramebufferAddress = framebuffer.Address;

            // A surface with a width/height of 0 is invalid and can't be created
            if (desiredImageInfo.Width <= 0 || desiredImageInfo.Height <= 0)
            {
                throw new ArgumentException(
                    $"Unable to create a surface with size {desiredImageInfo.Width}x{desiredImageInfo.Height}",
                    nameof(desiredImageInfo));
            }

            var surface = SKSurface.Create(desiredImageInfo, _currentFramebufferAddress, 
                framebuffer.RowBytes, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

            // If surface cannot be created - try to create a compatibility shim first
            if (surface == null)
            {
                _conversionShim = new PixelFormatConversionShim(desiredImageInfo, framebuffer.Address);
                _preFramebufferCopyHandler = _conversionShim.SurfaceCopyHandler;

                surface = _conversionShim.Surface;
            }

            _framebufferSurface = surface ?? throw new Exception("Unable to create a surface for pixel format " +
                                                                 framebuffer.Format +
                                                                 " or pixel format translator");
            _currentImageInfo = desiredImageInfo;
        }

        /// <summary>
        /// Free Skia surface.
        /// </summary>
        private void FreeSurface()
        {
            _conversionShim?.Dispose();
            _conversionShim = null;
            _preFramebufferCopyHandler = null;

            _framebufferSurface?.Dispose();
            _framebufferSurface = null;
            _currentFramebufferAddress = IntPtr.Zero;
        }

        /// <summary>
        /// Converts non-compatible pixel formats using bitmap copies.
        /// </summary>
        private class PixelFormatConversionShim : IDisposable
        {
            private readonly SKBitmap _bitmap;
            private readonly SKImageInfo _destinationInfo;
            private readonly IntPtr _framebufferAddress;

            public PixelFormatConversionShim(SKImageInfo destinationInfo, IntPtr framebufferAddress)
            {
                _destinationInfo = destinationInfo;
                _framebufferAddress = framebufferAddress;

                // Create bitmap using default platform settings
                _bitmap = new SKBitmap(destinationInfo.Width, destinationInfo.Height);
                SKColorType bitmapColorType;

                if (!_bitmap.CanCopyTo(destinationInfo.ColorType))
                {
                    bitmapColorType = _bitmap.ColorType;
                    _bitmap.Dispose();

                    throw new Exception(
                        $"Unable to create pixel format shim for conversion from {bitmapColorType} to {destinationInfo.ColorType}");
                }

                Surface = SKSurface.Create(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes, new SKSurfaceProperties(SKPixelGeometry.RgbHorizontal));

                if (Surface == null)
                {
                    bitmapColorType = _bitmap.ColorType;
                    _bitmap.Dispose();

                    throw new Exception(
                        $"Unable to create pixel format shim surface for conversion from {bitmapColorType} to {destinationInfo.ColorType}");
                }

                SurfaceCopyHandler = Disposable.Create(CopySurface);
            }

            /// <summary>
            /// Skia surface.
            /// </summary>
            public SKSurface Surface { get; }

            /// <summary>
            /// Handler to start conversion via surface copy.
            /// </summary>
            public IDisposable SurfaceCopyHandler { get; }

            /// <inheritdoc />
            public void Dispose()
            {
                Surface.Dispose();
                _bitmap.Dispose();
            }

            /// <summary>
            /// Convert and copy surface to a framebuffer.
            /// </summary>
            private void CopySurface()
            {
                using (var snapshot = Surface.Snapshot())
                {
                    snapshot.ReadPixels(_destinationInfo, _framebufferAddress, _destinationInfo.RowBytes, 0, 0,
                        SKImageCachingHint.Disallow);
                }
            }
        }
    }
}
