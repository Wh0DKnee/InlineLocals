using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace InlineWatch
{
    class WatchAdornment : Button
    {
        private Rectangle rect;

        internal WatchAdornment() {
            rect = new Rectangle() {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                Width = 20,
                Height = 10
            };

            Update();

            Content = rect;
        }

        private Brush MakeBrush() {
            Color color = Colors.Lime;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        internal void Update() {
            rect.Fill = MakeBrush();
        }
    }
}
