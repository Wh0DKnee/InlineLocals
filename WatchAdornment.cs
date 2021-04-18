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
    // If we want more space between the end of the line and the watch button,
    // we can use a StackPanel with vertical orientation for our content and add
    // a first child that is invisible, which will function as padding.
    class WatchAdornment : Button
    {
        internal WatchAdornment(WatchTag watchTag) {
            Update(watchTag);
        }

        private Brush MakeBrush(Color color) {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        internal void Update(WatchTag watchTag) {
            Color backgroundColor = Colors.Gray;
            backgroundColor.ScA = 0.5F;
            this.Background = MakeBrush(backgroundColor);

            this.Foreground = MakeBrush(Colors.LightGray);

            Color foregroundColor = Colors.Black;
            foregroundColor.ScA = 0.5F;
            this.BorderBrush = MakeBrush(foregroundColor);
            this.FontStyle = FontStyles.Italic;
            Content = watchTag.WatchValue;
        }
    }
}
