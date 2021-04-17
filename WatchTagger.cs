using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace InlineWatch
{
    /// <summary>
    /// Helper base class for writing simple taggers based on regular expressions.
    /// </summary>
    /// <remarks>
    /// Regular expressions are expected to be single-line.
    /// </remarks>
    /// <typeparam name="T">The type of tags that will be produced by this tagger.</typeparam>
    internal class WatchTagger : ITagger<WatchTag>
    {
        private ITextBuffer Buffer;
        private List<ITagSpan<WatchTag>> TagSpans = new List<ITagSpan<WatchTag>>();

        public WatchTagger(ITextBuffer buffer) {

            //buffer.Changed += (sender, args) => HandleBufferChanged(args);
            this.Buffer = buffer;
            DebuggerCallback.Instance.LocalsChangedEvent += (sender, args) => HandleDebuggerLocalsChanged(args);
        }
        #region ITagger implementation

        public virtual IEnumerable<ITagSpan<WatchTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
            foreach(var tagSpan in TagSpans) {
                yield return tagSpan;
            }
        }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        private void HandleDebuggerLocalsChanged(LocalsChangedEventArgs args) {
            if(Buffer.CurrentSnapshot.LineCount <= args.lineNumber) { // TODO: replace with file comparison to filename in args.
                return;
            }

            /*TagSpans.Clear();
            ITextSnapshotLine line = Buffer.CurrentSnapshot.GetLineFromLineNumber((int)args.lineNumber);
            SnapshotSpan span = new SnapshotSpan(line.Start, line.End);
            TagSpans.Add(new TagSpan<WatchTag>(span, new WatchTag("hi")));

            var temp = TagsChanged;
            if (temp == null)
                return;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(Buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start,
                Buffer.CurrentSnapshot.GetLineFromLineNumber(Buffer.CurrentSnapshot.LineCount - 1).End);
            temp(this, new SnapshotSpanEventArgs(totalAffectedSpan));*/
        }

        /// <summary>
        /// Handle buffer changes. The default implementation expands changes to full lines and sends out
        /// a <see cref="TagsChanged"/> event for these lines.
        /// </summary>
        /// <param name="args">The buffer change arguments.</param>
        protected virtual void HandleBufferChanged(TextContentChangedEventArgs args) {
            if (args.Changes.Count == 0)
                return;

            var temp = TagsChanged;
            if (temp == null)
                return;

            // Combine all changes into a single span so that
            // the ITagger<>.TagsChanged event can be raised just once for a compound edit
            // with many parts.

            ITextSnapshot snapshot = args.After;

            int start = args.Changes[0].NewPosition;
            int end = args.Changes[args.Changes.Count - 1].NewEnd;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(
                snapshot.GetLineFromPosition(start).Start,
                snapshot.GetLineFromPosition(end).End);

            temp(this, new SnapshotSpanEventArgs(totalAffectedSpan));
        }
    }
}