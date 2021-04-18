using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EnvDTE90a;
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

            buffer.Changed += (sender, args) => HandleBufferChanged(args);
            this.Buffer = buffer;
            DebuggerCallback.Instance.LocalsChangedEvent += (sender, args) => HandleDebuggerLocalsChanged(args);
        }
        #region ITagger implementation

        public virtual IEnumerable<ITagSpan<WatchTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
            foreach (var tagSpan in TagSpans) {
                yield return tagSpan;
            }
        }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        private void HandleDebuggerLocalsChanged(StackFrame2 stackFrame) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            string fileName = Helpers.GetPath(Buffer);
            if(fileName != stackFrame.FileName) { // debugger is currently not in the file attached to this tagger
                return;
            }

            Dictionary<string, string> localsDict = new Dictionary<string, string>();
            EnvDTE.Expressions locals = stackFrame.Locals;
            foreach (EnvDTE.Expression local in locals) {
                localsDict[local.Name] = local.Value; 
                // TODO: allow duplicates (happens if you have two recursive calls in a function, say "return func(x-1)*func(x-2);")
            }

            TagSpans.Clear();

            int startLineIndex = GetFunctionStartLineIndex(stackFrame);
            if(startLineIndex == -1) {
                return;
            }
            int lastLineIndex = (int)stackFrame.LineNumber - 1; // TODO: calculate lastLine by matching the opening curly brace found by 
                                                                // match in GetFunctionStartLineIndex()
            for(int i = startLineIndex; i <= lastLineIndex; ++i) {
                ITextSnapshotLine textSnapshotLine = Buffer.CurrentSnapshot.GetLineFromLineNumber(i);
                string debugString = textSnapshotLine.GetText();
                TagSpan<WatchTag> tagSpan = CreateTagSpanForLine(localsDict, textSnapshotLine);
                if(!(tagSpan is null)) {
                    TagSpans.Add(tagSpan);
                }
            }

            ForceUpdateBuffers();
            var temp = TagsChanged;
            if (temp == null)
                return;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(Buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start,
                Buffer.CurrentSnapshot.GetLineFromLineNumber(Buffer.CurrentSnapshot.LineCount - 1).End);
            temp(this, new SnapshotSpanEventArgs(totalAffectedSpan));
        }

        private int GetFunctionStartLineIndex(StackFrame2 stackFrame) {
            Regex regex = new Regex(stackFrame.FunctionName + @"[\t\s]*\(.*\)[\t\s]*\{");

            int currentLineIndex = (int)stackFrame.LineNumber - 1; // stack frame line number starts at 1, snapshot line numbers a 0, hence the -1
            string sourceString = "";
            Match match = null;
            ITextSnapshotLine currentLine = null;
            while(match is null || !match.Success) {
                if(currentLineIndex < 0 || currentLineIndex >= Buffer.CurrentSnapshot.LineCount) {
                    return -1; // TODO: probably better to return a bool and pass an int as out parameter
                }
                currentLine = Buffer.CurrentSnapshot.GetLineFromLineNumber(currentLineIndex);
                sourceString = currentLine.GetText() + sourceString;
                match = regex.Match(sourceString);
                --currentLineIndex;
            }

            return ++currentLineIndex; // a bit ugly with re-incrementing here, but it works I guess
        }

        private string[] GetWords(string sourceText) {
            string[] stringSeparators = new string[] {" ", "(", ")", "[", "]", "{", "}", "\t", ";", "-", "+", "/", "*" };
            return sourceText.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private TagSpan<WatchTag> CreateTagSpanForLine(Dictionary<string, string> locals, ITextSnapshotLine snapshotLine) {
            string[] words = GetWords(snapshotLine.GetText());
            string lineTagString = "";
            HashSet<string> addedLocals = new HashSet<string>(); // locals that have been added to the watch already
            foreach (string word in words) {
                if (locals.ContainsKey(word) && !addedLocals.Contains(word)) { // TODO: also check for word + "returned" to display values returned by function call.
                    lineTagString += (word + ": " + locals[word] + " ");
                    addedLocals.Add(word);
                }
            }
            if (lineTagString == "") {
                return null;
            }
            return new TagSpan<WatchTag>(new SnapshotSpan(snapshotLine.End, 0), new WatchTag(lineTagString));
        }

        /// <summary>
        /// Usually, GetTags will only be called if the text buffer changed. Our tag changes are independent of the
        /// text buffer changes (tags are updated when the debugger steps), so we force an internal call to GetTags 
        /// by simulating a buffer change with an empty edit. (I think this works)
        private void ForceUpdateBuffers() {
            var fakeEdit = Buffer.CreateEdit();
            fakeEdit.Apply();

            // We also invoke an AfterLocalsChangedEvent, which the WatchAdornmentTagger
            // subscribes to, so that it knows that the tags have changed and it needs
            // to update the corresponding adornments.
            DebuggerCallback.Instance.InvokeAfterLocalsChangedEvent();
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