using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InlineLocals
{
    internal static class Helpers
    {
        public static string GetPath(this IWpfTextView textView) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            return GetPath(textView.TextBuffer);
        }

        public static string GetPath(this ITextBuffer buffer) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            buffer.Properties.TryGetProperty(typeof(IVsTextBuffer), out IVsTextBuffer bufferAdapter);
            var persistFileFormat = bufferAdapter as IPersistFileFormat;

            if (persistFileFormat == null) {
                return null;
            }
            persistFileFormat.GetCurFile(out string filePath, out _);
            return filePath;
        }

        public static bool TryGetFontSize(ref double size) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (DTE is null)
                return false;
            EnvDTE.Properties propertiesList = DTE.get_Properties("FontsAndColors", "TextEditor");
            if (propertiesList is null)
                return false;
            Property prop = propertiesList.Item("FontSize");
            if (prop is null)
                return false;
            int fontSize = (System.Int16)prop.Value;
            size = (double)fontSize;
            return true;
        }

        public static bool TryGetFontFamily(ref string fontFamilyString) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.DTE DTE = Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(SDTE)) as EnvDTE.DTE;
            if (DTE is null)
                return false;
            EnvDTE.Properties propertiesList = DTE.get_Properties("FontsAndColors", "TextEditor");
            if (propertiesList is null)
                return false;
            Property prop = propertiesList.Item("FontFamily");
            if (prop is null)
                return false;
            fontFamilyString = (string)prop.Value;
            return true;
        }

        public static bool TryGetFont(ref Font font) {
            string fontFamily = null;
            double fontSize = 0;
            if (!TryGetFontSize(ref fontSize)) {
                return false;
            }
            if (!TryGetFontFamily(ref fontFamily)) {
                return false;
            }
            font = new Font(fontFamily, (float)fontSize);
            return true;
        }
    }
}
