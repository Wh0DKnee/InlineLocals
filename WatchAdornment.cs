using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;

namespace InlineWatch
{
    class WatchAdornment : Button
    {
        internal WatchAdornment(WatchTag watchTag) {
            Update(watchTag);
        }

        private Brush MakeBrush() {
            Color color = Colors.Gray;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        internal void Update(WatchTag watchTag) {
            this.Background = MakeBrush();
            Content = watchTag.WatchValue;
        }
    }
}
