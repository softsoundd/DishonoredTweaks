using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DishonoredTweaks.Behaviors
{
    public class SmoothScrollBehavior
    {
        private static readonly Dictionary<ScrollViewer, double> TargetOffsets = new();
        private static readonly Dictionary<ScrollViewer, bool> IsAnimating = new();
        private static readonly Dictionary<ScrollViewer, EventHandler> RenderingHandlers = new();

        public static readonly DependencyProperty SmoothScrollProperty =
            DependencyProperty.RegisterAttached(
                "SmoothScroll",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnSmoothScrollChanged));

        public static readonly DependencyProperty FadeEdgesProperty =
            DependencyProperty.RegisterAttached(
                "FadeEdges",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnFadeEdgesChanged));

        public static bool GetSmoothScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(SmoothScrollProperty);
        }

        public static void SetSmoothScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(SmoothScrollProperty, value);
        }

        public static bool GetFadeEdges(DependencyObject obj)
        {
            return (bool)obj.GetValue(FadeEdgesProperty);
        }

        public static void SetFadeEdges(DependencyObject obj, bool value)
        {
            obj.SetValue(FadeEdgesProperty, value);
        }

        private static void OnSmoothScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.Loaded -= ScrollViewerLoaded;
                scrollViewer.PreviewMouseWheel -= ScrollViewerPreviewMouseWheel;
                scrollViewer.ScrollChanged -= ScrollViewerScrollChanged;

                if ((bool)e.NewValue)
                {
                    scrollViewer.Loaded += ScrollViewerLoaded;
                    scrollViewer.PreviewMouseWheel += ScrollViewerPreviewMouseWheel;
                    scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
                }
                else
                {
                    TargetOffsets.Remove(scrollViewer);
                    IsAnimating.Remove(scrollViewer);

                    if (RenderingHandlers.ContainsKey(scrollViewer))
                    {
                        CompositionTarget.Rendering -= RenderingHandlers[scrollViewer];
                        RenderingHandlers.Remove(scrollViewer);
                    }
                }
            }
        }

        private static void OnFadeEdgesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer scrollViewer)
            {
                scrollViewer.Loaded -= ScrollViewerLoadedForFade;
                scrollViewer.ScrollChanged -= ScrollViewerScrollChangedForFade;
                scrollViewer.SizeChanged -= ScrollViewerSizeChangedForFade;

                if ((bool)e.NewValue)
                {
                    scrollViewer.Loaded += ScrollViewerLoadedForFade;
                    scrollViewer.ScrollChanged += ScrollViewerScrollChangedForFade;
                    scrollViewer.SizeChanged += ScrollViewerSizeChangedForFade;
                }
            }
        }

        private static void ScrollViewerLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                scrollViewer.PreviewMouseWheel += ScrollViewerPreviewMouseWheel;
                scrollViewer.ScrollChanged += ScrollViewerScrollChanged;
            }
        }

        private static void ScrollViewerPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (ShouldIgnoreMouseWheel(e.OriginalSource as DependencyObject, scrollViewer))
                {
                    return;
                }

                if (!TargetOffsets.ContainsKey(scrollViewer))
                {
                    TargetOffsets[scrollViewer] = scrollViewer.VerticalOffset;
                }

                double newTargetOffset = TargetOffsets[scrollViewer] - (e.Delta * 0.5);
                newTargetOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, newTargetOffset));
                TargetOffsets[scrollViewer] = newTargetOffset;

                if (!RenderingHandlers.ContainsKey(scrollViewer))
                {
                    EventHandler handler = (_, _) => OnRendering(scrollViewer);
                    RenderingHandlers[scrollViewer] = handler;
                    IsAnimating[scrollViewer] = true;
                    CompositionTarget.Rendering += handler;
                }
                else if (!IsAnimating.ContainsKey(scrollViewer) || !IsAnimating[scrollViewer])
                {
                    IsAnimating[scrollViewer] = true;
                    CompositionTarget.Rendering += RenderingHandlers[scrollViewer];
                }

                e.Handled = true;
            }
        }

        private static void ScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (!IsAnimating.ContainsKey(scrollViewer) || !IsAnimating[scrollViewer])
                {
                    if (e.VerticalChange != 0)
                    {
                        TargetOffsets[scrollViewer] = scrollViewer.VerticalOffset;
                    }
                }
            }
        }

        private static void OnRendering(ScrollViewer scrollViewer)
        {
            if (!TargetOffsets.ContainsKey(scrollViewer))
            {
                StopAnimation(scrollViewer);
                return;
            }

            double current = scrollViewer.VerticalOffset;
            double target = TargetOffsets[scrollViewer];
            double difference = target - current;

            if (Math.Abs(difference) < 0.5)
            {
                scrollViewer.ScrollToVerticalOffset(target);
                StopAnimation(scrollViewer);
                return;
            }

            double newOffset = current + (difference * 0.10);
            scrollViewer.ScrollToVerticalOffset(newOffset);
        }

        private static void StopAnimation(ScrollViewer scrollViewer)
        {
            if (RenderingHandlers.ContainsKey(scrollViewer))
            {
                CompositionTarget.Rendering -= RenderingHandlers[scrollViewer];
            }

            IsAnimating[scrollViewer] = false;
        }

        private static void ScrollViewerLoadedForFade(object sender, RoutedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                UpdateFadeMask(scrollViewer);
            }
        }

        private static void ScrollViewerScrollChangedForFade(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                UpdateFadeMask(scrollViewer);
            }
        }

        private static void ScrollViewerSizeChangedForFade(object sender, SizeChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                UpdateFadeMask(scrollViewer);
            }
        }

        private static void UpdateFadeMask(ScrollViewer scrollViewer)
        {
            if (scrollViewer.ScrollableHeight == 0)
            {
                scrollViewer.OpacityMask = null;
                return;
            }

            double topFadeStart = 0;
            double topFadeEnd = 0.05;
            double bottomFadeStart = 0.95;
            double bottomFadeEnd = 1;

            bool atTop = scrollViewer.VerticalOffset <= 0.1;
            bool atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 0.1;

            LinearGradientBrush brush = new();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(0, 1);

            if (atTop && atBottom)
            {
                scrollViewer.OpacityMask = null;
                return;
            }
            else if (atTop)
            {
                brush.GradientStops.Add(new GradientStop(Colors.Black, 0));
                brush.GradientStops.Add(new GradientStop(Colors.Black, bottomFadeStart));
                brush.GradientStops.Add(new GradientStop(Colors.Transparent, bottomFadeEnd));
            }
            else if (atBottom)
            {
                brush.GradientStops.Add(new GradientStop(Colors.Transparent, topFadeStart));
                brush.GradientStops.Add(new GradientStop(Colors.Black, topFadeEnd));
                brush.GradientStops.Add(new GradientStop(Colors.Black, 1));
            }
            else
            {
                brush.GradientStops.Add(new GradientStop(Colors.Transparent, topFadeStart));
                brush.GradientStops.Add(new GradientStop(Colors.Black, topFadeEnd));
                brush.GradientStops.Add(new GradientStop(Colors.Black, bottomFadeStart));
                brush.GradientStops.Add(new GradientStop(Colors.Transparent, bottomFadeEnd));
            }

            scrollViewer.OpacityMask = brush;
        }

        private static bool ShouldIgnoreMouseWheel(DependencyObject? originalSource, ScrollViewer mainScrollViewer)
        {
            DependencyObject? current = originalSource;

            while (current != null)
            {
                if (current == mainScrollViewer)
                {
                    return false;
                }

                if (current is ScrollViewer ||
                    current is ComboBox ||
                    current is System.Windows.Controls.Primitives.Selector)
                {
                    return true;
                }

                current = GetParent(current);
            }

            return true;
        }

        private static DependencyObject? GetParent(DependencyObject child)
        {
            if (child is Visual || child is System.Windows.Media.Media3D.Visual3D)
            {
                DependencyObject? visualParent = VisualTreeHelper.GetParent(child);
                if (visualParent != null)
                {
                    return visualParent;
                }
            }

            return LogicalTreeHelper.GetParent(child);
        }
    }
}
