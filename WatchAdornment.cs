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
    //
    // Maybe it'd be a good idea to give each its own control element, so for example
    // we wouldn't have "x: 1   y: 2" as one button, but as two separate ones. That way
    // we could add different behavior based on the represented data type, for example
    // when hovering. The default view for a vector could be "vi: {size=5}", but when you
    // hover the button, it changes to "vi: {1, 2, 3, 4, 5}".
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
            backgroundColor.ScA = 0.12F;
            this.Background = MakeBrush(backgroundColor);

            this.Foreground = MakeBrush(Colors.LightGray);

            Color foregroundColor = Colors.Black;
            foregroundColor.ScA = 0.12F;
            this.BorderBrush = MakeBrush(foregroundColor);
            this.FontStyle = FontStyles.Italic;
            Content = watchTag.WatchValue;
        }
    }
}
