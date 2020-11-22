using System;
using System.Windows.Documents;
using System.Windows.Media;
using NUnit.Framework;

namespace GitOut.Features.Git.Stage
{
    public class DiffViewModelTest
    {
        [Test]
        public void CreateAddPatchShouldCreateValidPatchForStaging()
        {
            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            DiffOptions options = DiffOptions.Builder().Build();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult diff = builder.Build(options);
            var viewModel = DiffViewModel.ParseDiff(change, diff, new DiffDisplayOptions(1, Brushes.White, Brushes.White));
            TextPointer start = viewModel.Document.ContentStart;
            TextPointer end = viewModel.Document.ContentEnd;

            var range = new TextRange(start, end);
            GitPatch patch = viewModel.CreateAddPatch(range, PatchLineTransform.None);
            Assert.That(patch.Mode, Is.EqualTo(PatchMode.AddIndex));
            string patchText = patch.Writer.ToString().Replace("\r\n", "\n");
            Assert.That(patchText, Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,2 +1,4 @@
 line0
+line1
+line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void CreateResetPatchShouldCreateValidPatchForReset()
        {
            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            DiffOptions options = DiffOptions.Builder().Cached().Build();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult diff = builder.Build(options);
            var viewModel = DiffViewModel.ParseDiff(change, diff, new DiffDisplayOptions(1, Brushes.White, Brushes.White));
            TextPointer start = viewModel.Document.ContentStart;
            TextPointer end = viewModel.Document.ContentEnd;

            var range = new TextRange(start, end);
            GitPatch patch = viewModel.CreateResetPatch(range);
            Assert.That(patch.Mode, Is.EqualTo(PatchMode.ResetIndex));
            string patchText = patch.Writer.ToString().Replace("\r\n", "\n");
            Assert.That(patchText, Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,4 +1,2 @@
 line0
-line1
-line2
 line3
".Replace("\r\n", "\n")));
        }

        [Test]
        public void CreateResetPatchFromSelectedLineShouldCreateValidPatchForReset()
        {
            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            DiffOptions options = DiffOptions.Builder().Cached().Build();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -1,4 +1,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult diff = builder.Build(options);
            var viewModel = DiffViewModel.ParseDiff(change, diff, new DiffDisplayOptions(1, Brushes.White, Brushes.White));
            int offset = 28;
            TextPointer start = viewModel.Document.ContentStart.GetPositionAtOffset(offset);
            TextPointer end = start.GetPositionAtOffset(3);

            var range = new TextRange(start, end);
            GitPatch patch = viewModel.CreateResetPatch(range);
            Assert.That(patch.Mode, Is.EqualTo(PatchMode.ResetIndex));
            string patchText = patch.Writer.ToString().Replace("\r\n", "\n");
            Assert.That(patchText, Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -1,3 +1,2 @@
 line0
-line1
 line2
".Replace("\r\n", "\n")));
        }

        [Test]
        public void CreateResetPatchFromSelectedLineShouldCreateValidPatchForResetFromSpecificIndex()
        {
            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            DiffOptions options = DiffOptions.Builder().Cached().Build();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -50,4 +50,5 @@");
            builder.Feed(" line0");
            builder.Feed("+line1");
            builder.Feed("+line2");
            builder.Feed(" line3");
            GitDiffResult diff = builder.Build(options);
            var viewModel = DiffViewModel.ParseDiff(change, diff, new DiffDisplayOptions(1, Brushes.White, Brushes.White));
            int offset = 28;
            TextPointer start = viewModel.Document.ContentStart.GetPositionAtOffset(offset);
            TextPointer end = start.GetPositionAtOffset(3);

            var range = new TextRange(start, end);
            GitPatch patch = viewModel.CreateResetPatch(range);
            Assert.That(patch.Mode, Is.EqualTo(PatchMode.ResetIndex));
            string patchText = patch.Writer.ToString().Replace("\r\n", "\n");
            Assert.That(patchText, Is.EqualTo(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -50,3 +50,2 @@
 line0
-line1
 line2
".Replace("\r\n", "\n")));
        }

        [Test]
        public void CreateResetPatchFromSelectedLineShouldCreateValidPatchForMixedSelection()
        {
            GitStatusChange change = GitStatusChange.Parse("1 .M N... 100644 100644 100644 9e7e798e2b5cf7e72dba4554a144dcc85bf7f4d6 2952ce2c99004f4f66aae34bff1b0d6252cbe36e filename.txt").Build();
            DiffOptions options = DiffOptions.Builder().Cached().Build();
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed("diff --git a/filename.txt b/filename.txt");
            builder.Feed("index 0123456..789abcd 100644");
            builder.Feed("--- a/filename.txt");
            builder.Feed("+++ b/filename.txt");
            builder.Feed("@@ -4,16 +4,22 @@");
            builder.Feed(" line 4 4");
            builder.Feed(" line 5 5");
            builder.Feed(" line 6 6");
            builder.Feed("-line 7  ");
            builder.Feed(" line 8 7");
            builder.Feed(" line 9 8");
            builder.Feed(" line10 9");
            builder.Feed(" line1110");
            builder.Feed("-line12  ");
            builder.Feed("+line  11");
            builder.Feed(" line1312");
            builder.Feed(" line1413");
            builder.Feed("+line  14");
            builder.Feed(" line1515");
            builder.Feed(" line1616");
            builder.Feed(" line1717");
            builder.Feed("-line18  ");
            builder.Feed("+line  18");
            builder.Feed("+line  19");
            builder.Feed("+line  20");
            builder.Feed("+line  21");
            builder.Feed("+line  22");
            builder.Feed("+line  23");
            builder.Feed("+line  24");
            builder.Feed(" line1925");
            GitDiffResult diff = builder.Build(options);
            var viewModel = DiffViewModel.ParseDiff(change, diff, new DiffDisplayOptions(1, Brushes.White, Brushes.White));
            int offset = 11 * 21;
            TextPointer start = viewModel.Document.ContentStart.GetPositionAtOffset(offset);
            TextPointer end = start.GetPositionAtOffset(10 * 6);

            var range = new TextRange(start, end);
            GitPatch patch = viewModel.CreateResetPatch(range);
            Assert.That(patch.Mode, Is.EqualTo(PatchMode.ResetIndex));
            string[] patchText = patch.Writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            CollectionAssert.AreEqual(@"diff --git a/filename.txt b/filename.txt
--- a/filename.txt
+++ b/filename.txt
@@ -17,8 +17,2 @@
 line1717
-line  18
-line  19
-line  20
-line  21
-line  22
-line  23
 line  24
".Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), patchText);
        }
    }
}
