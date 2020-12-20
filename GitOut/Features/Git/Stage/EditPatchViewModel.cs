using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;

namespace GitOut.Features.Git.Stage
{
    public class EditPatchViewModel : INotifyPropertyChanged, IHunkLineVisitorProvider
    {
        private readonly int fromRangeIndex;
        private readonly IEnumerable<HunkLine> lines;
        private readonly HunkLine preline;
        private readonly HunkLine postline;

        string text;

        public EditPatchViewModel(int fromRangeIndex, IEnumerable<HunkLine> lines, HunkLine preline, HunkLine postline)
        {
            this.fromRangeIndex = fromRangeIndex;
            this.lines = lines;
            this.text = string.Join(Environment.NewLine, lines.Where(line => line.Type != DiffLineType.Removed).Select(line => line.StrippedLine));
            this.preline = preline;
            this.postline = postline;
        }

        public string Text
        {
            get => text;
            set => SetProperty(ref text, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public IHunkLineVisitor GetHunkVisitor(PatchMode mode) => new EditPatchHunkLineVisitor(this);

        public static EditPatchViewModel StageFrom(IHunkLineVisitor visitor)
        {
            HunkLine preline = visitor.FindPrepositionHunk();
            int fromRangeIndex;
            switch (preline.Type)
            {
                case DiffLineType.Header:
                    fromRangeIndex = preline.FromIndex!.Value;
                    break;
                case DiffLineType.Removed:
                case DiffLineType.None:
                    fromRangeIndex = preline.FromIndex!.Value;
                    break;
                default:
                    throw new InvalidOperationException("Preline is not of expected type");
            }
            var lines = visitor.TraverseSelectionHunks().ToList();
            if (!visitor.IsDone)
            {
                throw new InvalidOperationException("Can only patch one hunk");
            }
            HunkLine? postline = visitor.FindPostpositionHunk();
            if (postline is null)
            {
                throw new InvalidOperationException("Need a postline to create patch");
            }
            return new EditPatchViewModel(fromRangeIndex, lines, preline, postline);
        }

        private bool SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
        {
            if (!ReferenceEquals(prop, value))
            {
                prop = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }
            return false;
        }

        private class EditPatchHunkLineVisitor : IHunkLineVisitor
        {
            private readonly EditPatchViewModel parent;

            public EditPatchHunkLineVisitor(EditPatchViewModel editPatchViewModel) => this.parent = editPatchViewModel;

            public bool IsDone => true;

            public HunkLine Current => throw new NotImplementedException();

            public HunkLine FindPostpositionHunk() => parent.postline;
            public HunkLine FindPrepositionHunk() => parent.preline;
            public IEnumerable<HunkLine> TraverseSelectionHunks() => parent.Text
                .Split(Environment.NewLine)
                .Select((line, index) => HunkLine.AsAdded($"+{line}", parent.fromRangeIndex + index))
                .Concat(parent.lines
                .Where(line => line.Type == DiffLineType.None || line.Type == DiffLineType.Removed)
                .Select((line, index) => HunkLine.AsRemoved($"-{line.StrippedLine}", parent.fromRangeIndex + index)));
        }
    }
}
