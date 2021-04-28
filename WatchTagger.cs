using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnvDTE90a;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace InlineLocals
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
            DebuggerCallback.Instance.DebuggingStoppedEvent += (sender) => HandleDebuggingStopped();
        }
        #region ITagger implementation

        public virtual IEnumerable<ITagSpan<WatchTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
            foreach (var tagSpan in TagSpans) {
                yield return tagSpan;
            }
        }
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #endregion

        private void HandleDebuggingStopped() {
            TagSpans.Clear();

            ForceUpdateBuffers();
        }

        private void HandleDebuggerLocalsChanged(StackFrame2 stackFrame) {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            string fileName = Helpers.GetPath(Buffer);
            if(fileName != stackFrame.FileName) { // debugger is currently not in the file attached to this tagger
                return;
            }
            
            EnvDTE.Expressions locals = stackFrame.Locals;

            bool isLangCSharp = stackFrame.Language.Contains("#");

            TagSpans.Clear();

            int startLineIndex = GetFunctionStartLineIndex(stackFrame);
            if(startLineIndex == -1) {
                return;
            }
            int lastLineIndex = (int)stackFrame.LineNumber - 1; // TODO: calculate lastLine by matching the opening curly brace found by 
                                                                // match in GetFunctionStartLineIndex(). But do we want this behavior?
                                                                // Probably best to implement and let the user decide in settings.

            int longestLineWidth = GetLongestLineWidth(Buffer.CurrentSnapshot, startLineIndex, lastLineIndex);
            for(int i = startLineIndex; i <= lastLineIndex; ++i) {
                ITextSnapshotLine textSnapshotLine = Buffer.CurrentSnapshot.GetLineFromLineNumber(i);
                string debugString = textSnapshotLine.GetText();
                TagSpan<WatchTag> tagSpan = CreateTagSpanForLine(locals, isLangCSharp, textSnapshotLine, longestLineWidth);
                if(!(tagSpan is null)) {
                    TagSpans.Add(tagSpan);
                }
            }

            ForceUpdateBuffers();
        }

        private int GetLongestLineWidth(ITextSnapshot snapshot, int firstLineIndex, int lastLineIndex) {
            int longestWidth = 0;
            for (int i = firstLineIndex; i <= lastLineIndex; ++i) {
                ITextSnapshotLine textSnapshotLine = Buffer.CurrentSnapshot.GetLineFromLineNumber(i);
                longestWidth = Math.Max(GetLineWidth(textSnapshotLine.GetText()), longestWidth);
            }
            return longestWidth;
        }

        private int GetLineWidth(string line) {
            Font font = null;
            if (!Helpers.TryGetFont(ref font)) {
                return 0; // TODO: dont silently fail
            }
            return TextRenderer.MeasureText(line, font).Width;
        }

        private int GetFunctionStartLineIndex(StackFrame2 stackFrame) {
            string[] stringSeparators = new string[] {".", "::"};
            string[] functionNameStrings = stackFrame.FunctionName.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            // remove stuff like namespace, class name, application name, we only want the actual function name.
            string actualFunctionName = functionNameStrings[functionNameStrings.Length - 1];
            Regex regex = new Regex(actualFunctionName + @"[\t\s]*\(.*\)[\t\s]*\{");

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
            string[] stringSeparators = new string[] {" ", "(", ")", "[", "]", "{", "}", "\t", ";", "-", "+", "/", "*", "%",
                "+=", "-=", "%=", "?", ":", "<", ">", "<=", ">=", "==",",", "<<", ">>", "||", "&&", "|", "&", "^" };
            sourceText = sourceText.Replace("->", "."); // hack to get around "->" being split, because we have "-" as a separator
            // leads to "this->member" being displayed as "this.member", fix this somehow.
            return sourceText.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private TagSpan<WatchTag> CreateTagSpanForLine(EnvDTE.Expressions locals, bool isLangCSharp, ITextSnapshotLine snapshotLine, int longestLineWidth) {
            Dictionary<string, LocalInfo> localsDict = new Dictionary<string, LocalInfo>();
            // TODO: allow duplicates (happens if you have two recursive calls in a function, say "return func(x-1)*func(x-2);")

            string lineText = snapshotLine.GetText();
            string[] words = GetWords(lineText);

            foreach (string word in words) {
                string value;
                string type;
                if (Contains(locals, word, out value, out type)) { // TODO: also check for word + "returned" to display values returned by function call.
                    localsDict[word] = new LocalInfo(value, type); // may override previous occurence, TODO: handle this (see above comment about when this occurs)
                }
                string thisString = isLangCSharp ? "this." : "this->";
                if (Contains(locals, thisString + word, out value, out type)) { // also search for current member variables
                    localsDict[thisString + word] = new LocalInfo(value, type); 
                }
            }
            if (localsDict.Count == 0) {
                return null;
            }

            longestLineWidth = 0; // disabling this for now, doesnt quite work yet.
            return new TagSpan<WatchTag>(new SnapshotSpan(snapshotLine.End, 0), new WatchTag(localsDict, longestLineWidth));
        }

        private bool Contains(EnvDTE.Expressions locals, string word, out string value, out string type) {
            string[] memberHierarchy = word.Split(new string[]{".", "->"}, StringSplitOptions.RemoveEmptyEntries);
            return Contains(locals, memberHierarchy, 0, out value, out type);
        }
        
        // Recursively traverse locals tree.
        private bool Contains(EnvDTE.Expressions locals, string[] memberHierarchy, int index, out string value, out string type) {
            foreach (EnvDTE.Expression local in locals) {
                if (local.Name == memberHierarchy[index]) {
                    if(index == memberHierarchy.Length - 1) {
                        value = local.Value;
                        type = local.Type;
                        return true;
                    }
                    return Contains(local.DataMembers, memberHierarchy, ++index, out value, out type);
                }
            }

            value = null;
            type = null;
            return false;
        }

        // This only does work on collections, i.e. locals that have a child that matches the regex [[0-9]*]
        // for which it changes the value string from "{size = 5}" to the actual list contents.
        // However, this is pretty expensive, and finding out if a local is a collection takes
        // O(n), so maybe we should only call this function when the user hovers over the local
        // and then change the displayed value to the collection.
        //
        // TODO: This sometimes fails for maps/dictionaries. Say we have the map {{1,2} {2,3}}, then
        // this will show {2,3} because we have a child [1] with value 2 and a child [2] with value 3.
        // Instead of defining a collection as a local that has a child matching [[0-9]*], we should
        // probably have a list of types for which we apply this method (vector, arrays, lists, etc), but
        // there a quite a few collection types in C# and C++...
        private string CreateValueString(EnvDTE.Expression local) {
            bool isCollection = false;
            string collectionValues = "{";
            int maxCollectionValuesToShow = 10;
            Regex regex = new Regex(@"\[([0-9]+)\]");
            foreach (EnvDTE.Expression expr in local.DataMembers) {
                Match match = regex.Match(expr.Name);
                if (match.Success) {
                    if(Int32.Parse(match.Groups[1].Value) >= maxCollectionValuesToShow) {
                        return collectionValues += "...}";
                    }
                    isCollection = true;
                    collectionValues += expr.Value + ", ";
                }
            }
            if (isCollection) {
                collectionValues = collectionValues.Substring(0, collectionValues.Length - 2);
                collectionValues += "}";
                return collectionValues;
            }
            return local.Value;
        }


        /// <summary>
        /// Usually, GetTags will only be called if the text buffer changed. Our tag changes are independent of the
        /// text buffer changes (tags are updated when the debugger steps), so we force an internal call to GetTags 
        /// by simulating a buffer change with an empty edit. (I think this works)
        /// </summary>
        private void ForceUpdateBuffers() {
            var temp = TagsChanged;
            if (temp == null)
                return;

            SnapshotSpan totalAffectedSpan = new SnapshotSpan(Buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start,
                Buffer.CurrentSnapshot.GetLineFromLineNumber(Buffer.CurrentSnapshot.LineCount - 1).End);
            temp(this, new SnapshotSpanEventArgs(totalAffectedSpan));

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
            // TODO: Delete all this.
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