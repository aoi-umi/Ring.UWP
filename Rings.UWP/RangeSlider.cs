using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

// The Templated Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234235

namespace Rings.UWP
{
    public class RangeSlider : Control
    {
        public RangeSlider()
        {
            this.DefaultStyleKey = typeof(RangeSlider);
        }

        Canvas ContainerCanvas;
        Thumb MinThumb;
        Thumb MaxThumb;
        Thumb ValueThumb;
        Rectangle ActiveRectangle;
        Rectangle TotalRectangle;
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            ContainerCanvas = GetTemplateChild(nameof(ContainerCanvas)) as Canvas;
            MinThumb = GetTemplateChild(nameof(MinThumb)) as Thumb;
            MaxThumb = GetTemplateChild(nameof(MaxThumb)) as Thumb;
            ValueThumb = GetTemplateChild(nameof(ValueThumb)) as Thumb;
            ActiveRectangle = GetTemplateChild(nameof(ActiveRectangle)) as Rectangle;
            TotalRectangle = GetTemplateChild(nameof(TotalRectangle)) as Rectangle;
            AddEvent();
        }

        void AddEvent()
        {
            ContainerCanvas.SizeChanged += ContainerCanvas_SizeChanged;
            MinThumb.DragCompleted += MinThumb_DragCompleted;
            MinThumb.DragDelta += MinThumb_DragDelta;
            MaxThumb.DragCompleted += MaxThumb_DragCompleted;
            MaxThumb.DragDelta += MaxThumb_DragDelta;
            ValueThumb.DragDelta += ValueThumb_DragDelta;
            ActiveRectangle.Tapped += ActiveRectangle_Tapped;
        }

        public delegate void RangeBaseValueChangedEventHandler(object sender, RangeBaseValueChangedEventArgs e);
        public event RangeBaseValueChangedEventHandler RangeMaxChanged;
        public event RangeBaseValueChangedEventHandler RangeMinChanged;
        public event RangeBaseValueChangedEventHandler ValueChanged;
        public event RangeBaseValueChangedEventHandler RangeValueChanged;

        public bool IsDragging { get { return ValueThumb == null ? false : ValueThumb.IsDragging; } }        

        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public double RangeMin
        {
            get { return (double)GetValue(RangeMinProperty); }
            set { SetValue(RangeMinProperty, value); }
        }

        public double RangeMax
        {
            get { return (double)GetValue(RangeMaxProperty); }
            set { SetValue(RangeMaxProperty, value); }
        }

        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double RangeValue
        {
            get { return (double)GetValue(RangeValueProperty); }
            private set { SetValue(RangeValueProperty, value); }
        }

        #region Property
        public static readonly DependencyProperty MinimumProperty = 
            DependencyProperty.Register("Minimum", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnMinimumPropertyChanged));

        public static readonly DependencyProperty MaximumProperty = 
            DependencyProperty.Register("Maximum", typeof(double), typeof(RangeSlider), new PropertyMetadata(10.0, OnMaxmumPropertyChanged));

        public static readonly DependencyProperty RangeMinProperty = 
            DependencyProperty.Register("RangeMin", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnRangeMinPropertyChanged));

        public static readonly DependencyProperty RangeMaxProperty = 
            DependencyProperty.Register("RangeMax", typeof(double), typeof(RangeSlider), new PropertyMetadata(10.0, OnRangeMaxPropertyChanged));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnValuePropertyChanged));

        public static readonly DependencyProperty RangeValueProperty =
            DependencyProperty.Register("RangeValue", typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnRangeValueChanged));
        #endregion

        #region OnPropertyChanged
        private static void OnMinimumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            var newValue = (double)e.NewValue;
            if (ele.RangeMin < newValue) ele.RangeMin = newValue;
            else ele.UpdateMinThumb(ele.RangeMin);
        }

        private static void OnMaxmumPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            var newValue = (double)e.NewValue;
            if (ele.RangeMax > newValue) ele.RangeMax = newValue;
            else ele.UpdateMaxThumb(ele.RangeMax);
            ele.OnRangeMaxChanged(new RangeBaseValueChangedEventArgs((double)e.OldValue, (double)e.NewValue));
        }

        private static void OnRangeMinPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            var newValue = (double)e.NewValue;

            if (Double.IsInfinity(newValue) || Double.IsNaN(newValue)) return;
            if (newValue < ele.Minimum)
            {
                ele.RangeMin = ele.Minimum;
            }
            else if (newValue > ele.Maximum)
            {
                ele.RangeMin = ele.Maximum;
            }
            else
            {
                ele.RangeMin = newValue;
            }

            if (ele.RangeMin > ele.RangeMax)
            {
                ele.RangeMax = ele.RangeMin;
            }

            ele.UpdateMinThumb(ele.RangeMin);
            ele.RangeValue = ele.RangeMax - ele.RangeMin;
            ele.OnRangeMinChanged(new RangeBaseValueChangedEventArgs((double)e.OldValue, (double)e.NewValue));
        }

        private static void OnRangeMaxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            var newValue = (double)e.NewValue;
            if (Double.IsInfinity(newValue) || Double.IsNaN(newValue)) return;
            if (newValue < ele.Minimum)
            {
                ele.RangeMax = ele.Minimum;
            }
            else if (newValue > ele.Maximum)
            {
                ele.RangeMax = ele.Maximum;
            }
            else
            {
                ele.RangeMax = newValue;
            }

            if (ele.RangeMax < ele.RangeMin)
            {
                ele.RangeMin = ele.RangeMax;
            }

            ele.UpdateMaxThumb(ele.RangeMax);
            ele.RangeValue = ele.RangeMax - ele.RangeMin;
        }

        private static void OnValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            var newValue = (double)e.NewValue;
            if (ele.RangeMin > newValue) ele.Value = ele.RangeMin;
            if (ele.RangeMax < newValue) ele.Value = ele.RangeMax;
            ele.UpdateValueThumb(ele.Value);
            ele.OnValueChanged(new RangeBaseValueChangedEventArgs((double)e.OldValue, (double)e.NewValue));
        }

        private static void OnRangeValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ele = (RangeSlider)d;
            ele.OnRangeValueChanged(new RangeBaseValueChangedEventArgs((double)e.OldValue, (double)e.NewValue));
        }
        #endregion

        virtual protected void OnRangeMaxChanged(RangeBaseValueChangedEventArgs e)
        {
            RangeMaxChanged?.Invoke(this, e);
        }

        virtual protected void OnRangeMinChanged(RangeBaseValueChangedEventArgs e)
        {
            RangeMinChanged?.Invoke(this, e);
        }

        virtual protected void OnValueChanged(RangeBaseValueChangedEventArgs e)
        {
            ValueChanged?.Invoke(this, e);
        }

        virtual protected void OnRangeValueChanged(RangeBaseValueChangedEventArgs e)
        {
            RangeValueChanged?.Invoke(this, e);
        }

        public void UpdateMinThumb(double min, bool update = false)
        {
            if (ContainerCanvas != null)
            {
                if (update || !MinThumb.IsDragging)
                {
                    var relativeLeft = ((min - Minimum) / (Maximum - Minimum)) * ContainerCanvas.ActualWidth;
                    if (Double.IsNaN(relativeLeft) || Double.IsInfinity(relativeLeft)) return;

                    if (Canvas.GetLeft(ValueThumb) < relativeLeft)
                    {
                        Canvas.SetLeft(ValueThumb, relativeLeft);
                        Value = min;
                    }
                    Canvas.SetLeft(MinThumb, relativeLeft);
                    Canvas.SetLeft(ActiveRectangle, relativeLeft);
                    var width = (RangeMax - min) / (Maximum - Minimum) * ContainerCanvas.ActualWidth;
                    if (width < 0) width = 0;
                    else if (Double.IsNaN(width) || Double.IsInfinity(width)) return;
                    ActiveRectangle.Width = width;
                }
            }
        }

        public void UpdateMaxThumb(double max, bool update = false)
        {
            if (ContainerCanvas != null)
            {
                if (update || !MaxThumb.IsDragging)
                {
                    var relativeRight = (max - Minimum) / (Maximum - Minimum) * ContainerCanvas.ActualWidth;
                    if (Double.IsNaN(relativeRight) || Double.IsInfinity(relativeRight)) return;

                    if (Canvas.GetLeft(ValueThumb) > relativeRight)
                    {
                        Canvas.SetLeft(ValueThumb, relativeRight);
                        Value = max;
                    }
                    Canvas.SetLeft(MaxThumb, relativeRight);
                    var width = (max - RangeMin) / (Maximum - Minimum) * ContainerCanvas.ActualWidth;
                    if (width < 0) width = 0;
                    else if (Double.IsNaN(width) || Double.IsInfinity(width)) return;
                    ActiveRectangle.Width = width;
                }
            }
        }

        public void UpdateValueThumb(double value, bool update = false)
        {
            if (ContainerCanvas != null)
            {
                if (update || !ValueThumb.IsDragging)
                {                    
                    var relativeLeft = ((value - Minimum) / (Maximum - Minimum)) * ContainerCanvas.ActualWidth;
                    if (Double.IsNaN(relativeLeft) || Double.IsInfinity(relativeLeft)) return;

                    Canvas.SetLeft(ValueThumb, relativeLeft);                    
                }
            }
        }

        private void ContainerCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var relativeLeft = ((RangeMin - Minimum) / (Maximum - Minimum)) * ContainerCanvas.ActualWidth;
            var relativeRight = (RangeMax - Minimum) / (Maximum - Minimum) * ContainerCanvas.ActualWidth;

            Canvas.SetLeft(MinThumb, relativeLeft);
            Canvas.SetLeft(ActiveRectangle, relativeLeft);
            Canvas.SetLeft(MaxThumb, relativeRight);
            var width = (RangeMax - RangeMin) / (Maximum - Minimum) * ContainerCanvas.ActualWidth;
            if (width < 0) width = 0;
            else if (Double.IsNaN(width) || Double.IsInfinity(width)) return;

            ActiveRectangle.Width = width;
        }

        private void MinThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var min = DragThumb(MinThumb, 0, Canvas.GetLeft(MaxThumb), e.HorizontalChange);
            UpdateMinThumb(min, true);
            RangeMin = min;
        }

        private void MaxThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var max = DragThumb(MaxThumb, Canvas.GetLeft(MinThumb), ContainerCanvas.ActualWidth, e.HorizontalChange);
            UpdateMaxThumb(max, true);
            RangeMax = max;
        }

        private void ValueThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var value = DragThumb(ValueThumb, Canvas.GetLeft(MinThumb), Canvas.GetLeft(MaxThumb), e.HorizontalChange);
            UpdateValueThumb(value, true);
            Value = value;
        }

        private double DragThumb(Thumb thumb, double min, double max, double offset)
        {
            var currentPos = Canvas.GetLeft(thumb);
            var nextPos = currentPos + offset;

            nextPos = Math.Max(min, nextPos);
            nextPos = Math.Min(max, nextPos);

            return (Minimum + (nextPos / ContainerCanvas.ActualWidth) * (Maximum - Minimum));
        }

        private void MinThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (Value < RangeMin) Value = RangeMin;
            UpdateMinThumb(RangeMin);
            Canvas.SetZIndex(MinThumb, 10);
            Canvas.SetZIndex(MaxThumb, 0);
        }

        private void MaxThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (Value > RangeMax) Value = RangeMax;
            UpdateMaxThumb(RangeMax);
            Canvas.SetZIndex(MinThumb, 0);
            Canvas.SetZIndex(MaxThumb, 10);
        }

        private void ActiveRectangle_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var point = e.GetPosition(TotalRectangle);
            var value = point.X / TotalRectangle.ActualWidth * (Maximum - Minimum);
            if (value >= RangeMin && value <= RangeMax) Value = value;
        }
    }

    public class RangeBaseValueChangedEventArgs : RoutedEventArgs
    {
        public double NewValue { get; }
        public double OldValue { get; }
        public RangeBaseValueChangedEventArgs(double oldValue, double newValue)
        {
            NewValue = newValue;
            OldValue = oldValue;
        }
    }
}
