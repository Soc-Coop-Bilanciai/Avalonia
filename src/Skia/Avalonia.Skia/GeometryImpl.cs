using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using Avalonia.Utilities;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of <see cref="IGeometryImpl"/>.
    /// </summary>
    internal abstract class GeometryImpl : IGeometryImpl
    {
        private readonly object _lock = new();
        private PathCache _pathCache;
        internal static readonly bool StrokeCacheEnabled = Environment.GetEnvironmentVariable("AVALONIA_STROKE_CACHE") == "1";
        private Dictionary<(double, PenLineCap, PenLineJoin, double), SKPath>? _strokeFills;

        public bool TryDrawCachedStroke(SKCanvas canvas, IPen pen, SKPaint paint)
        {
            lock (_lock)
            {
                if (GetCachedStrokeFill(pen) is not { } outline)
                    return false;
                // Keep the native path alive until Skia has consumed or recorded it.
                paint.IsStroke = false;
                canvas.DrawPath(outline, paint);
                return true;
            }
        }

        private SKPath? GetCachedStrokeFill(IPen pen)
        {
            if (pen.Thickness <= 0 || pen.DashStyle != null || StrokePath == null) return null;
            _strokeFills ??= new();
            var key = (pen.Thickness, pen.LineCap, pen.LineJoin, pen.MiterLimit);
            if (_strokeFills.TryGetValue(key, out var cached)) return cached;
            if (_strokeFills.Count >= 4) ClearStrokeFills();
            var path = SKPathHelper.CreateStrokedPath(StrokePath, pen);
            if (path != null) _strokeFills[key] = path;
            return path;
        }

        private void ClearStrokeFills()
        {
            if (_strokeFills == null) return;
            foreach (var path in _strokeFills.Values) path.Dispose();
            _strokeFills.Clear();
        }
        private SKPathMeasure? _cachedPathMeasure;

        private SKPathMeasure CachedPathMeasure => _cachedPathMeasure ??= new SKPathMeasure(StrokePath!);

        /// <inheritdoc />
        public abstract Rect Bounds { get; }

        /// <inheritdoc />
        public double ContourLength
        {
            get
            {
                if (StrokePath is null)
                    return 0;

                return CachedPathMeasure.Length;
            }
        }

        public abstract SKPath? StrokePath { get; }
        public abstract SKPath? FillPath { get; }

        /// <inheritdoc />
        public bool FillContains(Point point)
        {
            return PathContainsCore(FillPath, point);
        }

        /// <inheritdoc />
        public bool StrokeContains(IPen? pen, Point point)
        {
            lock (_lock)
            {
                _pathCache.UpdateIfNeeded(StrokePath, pen);

                return PathContainsCore(_pathCache.ExpandedPath, point);
            }
        }
        
        /// <summary>
        /// Check Skia path if it contains a point.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="point">Point.</param>
        /// <returns>True, if point is contained in a path.</returns>
        private static bool PathContainsCore(SKPath? path, Point point)
        {
            return path is not null && path.Contains((float)point.X, (float)point.Y);
        }

        /// <inheritdoc />
        public IGeometryImpl? Intersect(IGeometryImpl geometry)
        {
            var other = geometry as GeometryImpl;
            if (other == null)
                return null;
            return CombinedGeometryImpl.TryCreate(GeometryCombineMode.Intersect, this, other);
        }

        /// <inheritdoc />
        public Rect GetRenderBounds(IPen? pen)
        {
            lock (_lock)
            {
                Rect bounds;
                if (StrokeCacheEnabled && pen != null && GetCachedStrokeFill(pen) is { } outline)
                    bounds = outline.TightBounds.ToAvaloniaRect();
                else
                {
                    _pathCache.UpdateIfNeeded(StrokePath, pen);
                    bounds = _pathCache.RenderBounds;
                }
                if (StrokePath != FillPath && FillPath != null)
                    bounds = bounds.Union(FillPath.TightBounds.ToAvaloniaRect());
                return bounds;
            }
        }

        public IGeometryImpl GetWidenedGeometry(IPen pen)
        {
            if (StrokePath is not null && SKPathHelper.CreateStrokedPath(StrokePath, pen) is { } path)
            {
                // The path returned to us by skia here does not have closed figures.
                // Fix that by calling CreateClosedPath.
                var closed = SKPathHelper.CreateClosedPath(path);
                return new StreamGeometryImpl(closed, closed);
            }
            
            return new StreamGeometryImpl(new SKPath(), null);
        }

        /// <inheritdoc />
        public ITransformedGeometryImpl WithTransform(Matrix transform)
        {
            return new TransformedGeometryImpl(this, transform);
        }

        /// <inheritdoc />
        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            if (StrokePath is null)
            {
                point = new Point();
                return false;
            }

            var res = CachedPathMeasure.GetPosition((float)distance, out var skPoint);
            point = new Point(skPoint.X, skPoint.Y);
            return res;
        }

        /// <inheritdoc />
        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
        {
            if (StrokePath is null)
            {
                point = new Point();
                tangent = new Point();
                return false;
            }

            var res = CachedPathMeasure.GetPositionAndTangent((float)distance, out var skPoint, out var skTangent);
            point = new Point(skPoint.X, skPoint.Y);
            tangent = new Point(skTangent.X, skTangent.Y);
            return res;
        }

        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
            [NotNullWhen(true)] out IGeometryImpl? segmentGeometry)
        {
            if (StrokePath is null)
            {
                segmentGeometry = null;
                return false;
            }

            segmentGeometry = null;

            using var _skPathSegment = new SKPathBuilder();

            var res = CachedPathMeasure.GetSegment((float)startDistance, (float)stopDistance, _skPathSegment, startOnBeginFigure);

            if (res)
            {
                segmentGeometry = new StreamGeometryImpl(_skPathSegment.Detach(), null);
            }

            return res;
        }

        /// <summary>
        /// Invalidate all caches. Call after chaining path contents.
        /// </summary>
        protected void InvalidateCaches()
        {
            lock (_lock)
            {
                _pathCache.Dispose();
                _pathCache = default;
                ClearStrokeFills();
                _cachedPathMeasure?.Dispose();
                _cachedPathMeasure = null;
            }
        }

        private struct PathCache : IDisposable
        {
            private int _penHash;
            private SKPath? _path, _cachedFor;
            private Rect? _renderBounds;
            private static readonly SKPath s_emptyPath = new();
            
            public Rect RenderBounds => _renderBounds ??= (_path ?? _cachedFor ?? s_emptyPath).TightBounds.ToAvaloniaRect();
            public SKPath ExpandedPath => _path ?? s_emptyPath;

            public void UpdateIfNeeded(SKPath? strokePath, IPen? pen)
            {
                if (PenHelper.GetHashCode(pen, includeBrush: false) is { } penHash &&
                    penHash == _penHash &&
                    strokePath == _cachedFor)
                {
                    // We are up to date
                    return;
                }

                _renderBounds = null;
                _cachedFor = strokePath;
                _penHash = penHash;
                _path?.Dispose();

                if (strokePath is not null && pen is not null)
                    _path = SKPathHelper.CreateStrokedPath(strokePath, pen);
                else
                    _path = null;

            }

            public void Dispose()
            {
                _path?.Dispose();
                _path = null;
            }
        }
    }
}
