using Avalonia.Media;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="IStreamGeometryImpl"/>.
    /// </summary>
    internal class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        private Rect _bounds;
        private SKPath _strokePath;
        private SKPath? _fillPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="stroke">An existing Skia <see cref="SKPath"/> for the stroke.</param>
        /// <param name="fill">An existing Skia <see cref="SKPath"/> for the fill, can also be null or the same as the stroke</param>
        /// <param name="bounds">Precomputed path bounds.</param>
        public StreamGeometryImpl(SKPath stroke, SKPath? fill, Rect? bounds = null)
        {
            _strokePath = stroke;
            _fillPath = fill;
            _bounds = bounds ?? stroke.TightBounds.ToAvaloniaRect();
        }

        private StreamGeometryImpl(SKPath path) : this(path, path, default(Rect))
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl() : this(CreateEmptyPath())
        {
        }

        /// <inheritdoc />
        public override SKPath? StrokePath => _strokePath;

        /// <inheritdoc />
        public override SKPath? FillPath => _fillPath;

        /// <inheritdoc />
        public override Rect Bounds => _bounds;

        /// <inheritdoc />
        public IStreamGeometryImpl Clone()
        {
            var stroke = _strokePath.Clone();
            var fill = _fillPath == _strokePath ? stroke : _fillPath.Clone();
            return new StreamGeometryImpl(stroke, fill, Bounds);
        }

        /// <inheritdoc />
        public IStreamGeometryContextImpl Open()
        {
            return new StreamContext(this);
        }

        /// <summary>
        /// Create new empty <see cref="SKPath"/>.
        /// </summary>
        /// <returns>Empty <see cref="SKPath"/></returns>
        private static SKPath CreateEmptyPath()
        {
            return new SKPath
            {
                FillType = SKPathFillType.EvenOdd
            };
        }

        /// <summary>
        /// A Skia implementation of a <see cref="IStreamGeometryContextImpl"/>.
        /// </summary>
        private class StreamContext : IStreamGeometryContextImpl
        {
            private readonly StreamGeometryImpl _geometryImpl;
            private readonly SKPathBuilder _stroke;
            private SKPathBuilder? _fill;
            private SKPathBuilder Stroke => _stroke;
            private SKPathBuilder Fill => _fill ??= new();
            private bool _disposed;
            private bool _isFilled;
            private Point _startPoint;
            private bool _isFigureBroken;
            private bool Duplicate => _isFilled && !ReferenceEquals(_fill, Stroke);

            private void EnsureSeparateFillPath()
            {
                if (Stroke == Fill)
                {
                    using var path = Stroke.Snapshot();
                    _fill = new SKPathBuilder(path);
                }
            }
            
            private void BreakFigure()
            {
                if (!_isFigureBroken)
                {
                    _isFigureBroken = true;
                    EnsureSeparateFillPath();
                }
                    
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="StreamContext"/> class.
            /// <param name="geometryImpl">Geometry to operate on.</param>
            /// </summary>
            public StreamContext(StreamGeometryImpl geometryImpl)
            {
                _geometryImpl = geometryImpl;
                _stroke = new SKPathBuilder(geometryImpl._strokePath);
                _fill = ReferenceEquals(geometryImpl._fillPath, geometryImpl._strokePath)
                    ? _stroke
                    : geometryImpl._fillPath is { } fill ? new SKPathBuilder(fill) : null;
            }

            /// <inheritdoc />
            /// <remarks>Will update bounds of passed geometry.</remarks>
            public void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;

                using var stroke = Stroke;
                using var fill = ReferenceEquals(_fill, stroke) ? null : _fill;
                _geometryImpl._strokePath = stroke.Detach();
                _geometryImpl._fillPath = ReferenceEquals(_fill, stroke)
                    ? _geometryImpl._strokePath
                    : fill?.Detach();
                _geometryImpl._bounds = _geometryImpl._strokePath.TightBounds.ToAvaloniaRect();
                _geometryImpl.InvalidateCaches();
            }

            /// <inheritdoc />
            public void BeginFigure(Point startPoint, bool isFilled = true)
            {
                if (!isFilled) 
                    EnsureSeparateFillPath();

                _isFilled = isFilled;
                _startPoint = startPoint;
                _isFigureBroken = false;
                Stroke.MoveTo((float)startPoint.X, (float)startPoint.Y);
                if (Duplicate)
                    Fill.MoveTo((float)startPoint.X, (float)startPoint.Y);
            }

            /// <inheritdoc />
            public void EndFigure(bool isClosed)
            {
                if (isClosed)
                {
                    if (_isFigureBroken)
                    {
                        Stroke.LineTo(_startPoint.ToSKPoint());
                        _isFigureBroken = false;
                    }
                    else
                        Stroke.Close();
                    if (Duplicate)
                        Fill.Close();
                }
            }

            /// <inheritdoc />
            public void SetFillRule(FillRule fillRule)
            {
                Fill.FillType = fillRule == FillRule.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
            }

            /// <inheritdoc />
            public void LineTo(Point point, bool isStroked = true)
            {
                if (isStroked)
                {
                    Stroke.LineTo((float)point.X, (float)point.Y);
                }
                else
                {
                    BreakFigure();
                    Stroke.MoveTo((float)point.X, (float)point.Y);
                }
                if (Duplicate)
                    Fill.LineTo((float)point.X, (float)point.Y);
            }

            /// <inheritdoc />
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
            {
                var arc = isLargeArc ? SKPathArcSize.Large : SKPathArcSize.Small;
                var sweep = sweepDirection == SweepDirection.Clockwise
                    ? SKPathDirection.Clockwise
                    : SKPathDirection.CounterClockwise;

                if (isStroked)
                {
                    Stroke.ArcTo(
                        (float)size.Width,
                        (float)size.Height,
                        (float)rotationAngle,
                        arc,
                        sweep,
                        (float)point.X,
                        (float)point.Y);
                }
                else
                {
                    BreakFigure();
                    Stroke.MoveTo((float)point.X, (float)point.Y);
                }
                if (Duplicate)
                    Fill.ArcTo(
                        (float)size.Width,
                        (float)size.Height,
                        (float)rotationAngle,
                        arc,
                        sweep,
                        (float)point.X,
                        (float)point.Y);
            }

            /// <inheritdoc />
            public void CubicBezierTo(Point point1, Point point2, Point point3, bool isStroked = true)
            {
                if (isStroked)
                {
                    Stroke.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
                }
                else
                {
                    BreakFigure();
                    Stroke.MoveTo((float)point3.X, (float)point3.Y);
                }
                if (Duplicate)
                    Fill.CubicTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y, (float)point3.X, (float)point3.Y);
            }

            /// <inheritdoc />
            public void QuadraticBezierTo(Point point1, Point point2, bool isStroked = true)
            {
                if (isStroked)
                {
                    Stroke.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
                }
                else
                {
                    BreakFigure();
                    Stroke.MoveTo((float)point2.X, (float)point2.Y);
                }
                if (Duplicate)
                    Fill.QuadTo((float)point1.X, (float)point1.Y, (float)point2.X, (float)point2.Y);
            }
        }
    }
}
