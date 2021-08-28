using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Dawn.Wpf
{
    public static class SmoothProgressBarAnimation
    {
        public static double GetPercentage(DependencyObject obj)
        {
            return (double)obj.GetValue(PercentageProperty);
        }

        public static void SetPercentage(DependencyObject obj, double value)
        {
            obj.SetValue(PercentageProperty, value);
        }

        public static readonly DependencyProperty PercentageProperty = DependencyProperty.RegisterAttached(
            "Percentage",
            typeof(double),
            typeof(SmoothProgressBarAnimation), new PropertyMetadata(0.0, OnChanged));

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ProgressBar progressBar)
            {
                var animation = new DoubleAnimation((double)e.OldValue, (double)e.NewValue, new TimeSpan(0, 0, 0, 0, 250))
                {
                    EasingFunction = new QuadraticEase()
                    {
                        EasingMode = EasingMode.EaseInOut,
                    }
                };

                progressBar.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
            }
        }
    }
}
