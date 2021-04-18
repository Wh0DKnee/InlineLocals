using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace InlineWatch
{
    internal sealed class WatchAdornmentTagger : IntraTextAdornmentTagger<WatchTag, WatchAdornment>
    {
        
        internal static ITagger<IntraTextAdornmentTag> GetTagger(IWpfTextView view, Lazy<ITagAggregator<WatchTag>> watchTagAggr) {
            return view.Properties.GetOrCreateSingletonProperty<WatchAdornmentTagger>(
                () => new WatchAdornmentTagger(view, watchTagAggr.Value));
        }

        private ITagAggregator<WatchTag> watchTagAggr;

        private WatchAdornmentTagger(IWpfTextView view, ITagAggregator<WatchTag> watchTagAggr)
           : base(view) {
            this.watchTagAggr = watchTagAggr;
            DebuggerCallback.Instance.AfterLocalsChangedEvent += HandleAfterLocalsChanged;
        }

        protected override WatchAdornment CreateAdornment(WatchTag watchTag, SnapshotSpan span) {
            return new WatchAdornment(watchTag);
        }

        private void HandleAfterLocalsChanged(object sender) { // for now just, invalidate all spans
            List<SnapshotSpan> spans = new List<SnapshotSpan>();
            SnapshotSpan entireSpan = new SnapshotSpan(view.TextBuffer.CurrentSnapshot, 0, view.TextBuffer.CurrentSnapshot.Length);
            spans.Add(entireSpan);
            InvalidateSpans(spans);
        }

        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, WatchTag>> GetAdornmentData(NormalizedSnapshotSpanCollection spans) {
            if (spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            var watchTags = watchTagAggr.GetTags(spans);

            foreach (IMappingTagSpan<WatchTag> dataTagSpan in watchTags) {
                NormalizedSnapshotSpanCollection watchTagSpans = dataTagSpan.Span.GetSpans(snapshot);

                // Ignore data tags that are split by projection.
                // This is theoretically possible but unlikely in current scenarios.
                if (watchTagSpans.Count != 1)
                    continue;

                SnapshotSpan adornmentSpan = new SnapshotSpan(watchTagSpans[0].Start, 0);

                yield return Tuple.Create(adornmentSpan, (PositionAffinity?)PositionAffinity.Successor, dataTagSpan.Tag);
            }
        }

        protected override bool UpdateAdornment(WatchAdornment adornment, WatchTag watchTag) {
            adornment.Update(watchTag);
            return true;
        }
    }
}
