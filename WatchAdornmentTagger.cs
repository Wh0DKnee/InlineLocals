﻿using System;
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
        private string filePath;

        private WatchAdornmentTagger(IWpfTextView view, ITagAggregator<WatchTag> watchTagAggr)
           : base(view) {
            this.watchTagAggr = watchTagAggr;
            filePath = Helpers.GetPath(view);
        }

        protected override WatchAdornment CreateAdornment(WatchTag watchTag, SnapshotSpan span) {
            return new WatchAdornment(watchTag);
        }

        protected override IEnumerable<Tuple<SnapshotSpan, PositionAffinity?, WatchTag>> GetAdornmentData(NormalizedSnapshotSpanCollection spans) {
            /*ITextSnapshotLine line = view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(0);
            SnapshotSpan tmpSpan = new SnapshotSpan(line.End, 0);
            yield return Tuple.Create(tmpSpan, (PositionAffinity?)PositionAffinity.Successor, new WatchTag(null));*/

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