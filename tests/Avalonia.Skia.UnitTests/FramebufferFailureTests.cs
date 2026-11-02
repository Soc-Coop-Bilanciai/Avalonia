using System;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using Xunit;

namespace Avalonia.Skia.UnitTests;

public class FramebufferFailureTests
{
    [Fact]
    public void Failed_Context_Creation_Releases_The_Framebuffer()
    {
        using var framebuffer = new TestFramebuffer(new PixelSize(0, 8));
        using var target = new FramebufferRenderTarget(framebuffer);
        Assert.Throws<ArgumentException>(() => target.CreateDrawingContext(default, out _));
        Assert.Equal(1, framebuffer.UnlockCount);
    }

    [Fact]
    public void Successful_Context_Owns_Exactly_One_Framebuffer_Unlock()
    {
        using var framebuffer = new TestFramebuffer(new PixelSize(8, 8));
        using var target = new FramebufferRenderTarget(framebuffer);
        using (target.CreateDrawingContext(default, out _))
            Assert.Equal(0, framebuffer.UnlockCount);
        Assert.Equal(1, framebuffer.UnlockCount);
    }

    private sealed class TestFramebuffer(PixelSize size) : IFramebufferPlatformSurface, IFramebufferRenderTarget
    {
        private GCHandle _pin = GCHandle.Alloc(new byte[256], GCHandleType.Pinned);
        public bool RetainsFrameContents => true;
        public int UnlockCount { get; private set; }
        public IFramebufferRenderTarget CreateFramebufferRenderTarget() => this;
        public ILockedFramebuffer Lock(IRenderTarget.RenderTargetSceneInfo sceneInfo, out FramebufferLockProperties properties)
        {
            properties = new FramebufferLockProperties(true);
            return new LockedFramebuffer(_pin.AddrOfPinnedObject(), size, 32,
                new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul, () => UnlockCount++);
        }
        public void Dispose() { if (_pin.IsAllocated) _pin.Free(); }
    }
}
