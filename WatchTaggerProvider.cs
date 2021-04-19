using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace InlineLocals
{
    [Export(typeof(ITaggerProvider))]
    [ContentType("text")]
    [TagType(typeof(WatchTag))]
    internal sealed class WatchTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            return buffer.Properties.GetOrCreateSingletonProperty(() => new WatchTagger(buffer)) as ITagger<T>;
        }
    }
}