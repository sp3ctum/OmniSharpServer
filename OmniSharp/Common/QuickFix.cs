﻿using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;
using OmniSharp.Solution;

namespace OmniSharp.Common
{
    public class QuickFix
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public string Text { get; set; }

        /// <summary>
        ///   Initialize a QuickFix pointing to the first line of the
        ///   given region in the given file.
        /// </summary>
        public static QuickFix ForFirstLineInRegion
            (DomRegion region, CSharpFile file) {

            return QuickFix.ForFirstLineInRegion
                (region, file.Document);
        }

        public static QuickFix ForFirstLineInRegion
            (DomRegion region, IDocument document) {
            return new QuickFix
                { FileName = region.FileName
                , Line     = region.BeginLine
                , Column   = region.BeginColumn

                // Note that we could display an arbitrary amount of
                // context to the user: ranging from one line to tens,
                // hundreds..
                , Text = document.GetText
                    ( offset: document.GetOffset(region.Begin)
                    , length: document.GetLineByNumber
                                (region.BeginLine).Length)
                    .Trim()};
        }

        /// <summary>
        ///   Creates a new QuickFix representing the non-bodyRegion
        ///   of the given region. Can be used to create QuickFixes
        ///   for AST members. The resulting QuickFix will then
        ///   contain the name and type signature of the member.
        /// </summary>
        /// <example>
        ///   For the region containing a "public string GetText(...)
        ///   {return null}" this method will return a QuickFix whose
        ///   Text is "public string GetText(...) ". So the returned
        ///   Text contains the type signature and not the body.
        /// </example>
        public static QuickFix ForNonBodyRegion
            (DomRegion region, IDocument document, DomRegion bodyRegion) {

            var text = GetNonBodyRegion(region, document, bodyRegion);

            return new QuickFix
                { FileName = region.FileName
                , Line     = region.BeginLine
                , Column   = region.BeginColumn
                , Text     = text};

        }

        public static QuickFix ForNonBodyRegion
            (IMember member, IDocument document) {
            return ForNonBodyRegion
                (member.UnresolvedMember, document);
        }

        public static QuickFix ForNonBodyRegion
            (IUnresolvedMember member, IDocument document) {
            var text = GetNonBodyRegion
                (member.Region, document, member.BodyRegion);
            return new QuickFix
                { FileName = member.Region.FileName
                , Line     = member.Region.BeginLine
                , Column   = member.Region.BeginColumn
                , Text     = text};
        }

        public static QuickFix ForNonBodyRegion
            ( IMember   member
            , IDocument document
            , string    additionalTextToAdd
            , bool      addAdditionalTextToStart = true) {
            return ForNonBodyRegion
                ( member.UnresolvedMember
                , document
                , additionalTextToAdd
                , addAdditionalTextToStart);
        }

        public static QuickFix ForNonBodyRegion
            ( IUnresolvedMember member
            , IDocument         document
            , string            additionalTextToAdd
            , bool              addAdditionalTextToStart = true) {
            var qf = ForNonBodyRegion(member, document);
            var text = GetNonBodyRegion(member.Region, document, member.BodyRegion);

            if (addAdditionalTextToStart)
                qf.Text = additionalTextToAdd + text;
            else
                qf.Text = text + additionalTextToAdd;

            return qf;
        }

        static string GetNonBodyRegion
            (DomRegion region, IDocument document, DomRegion bodyRegion) {

            // Delegates have no body, so they will crash if we don't do this
            if (bodyRegion.BeginLine == 0
                && bodyRegion.EndLine == 0)
                bodyRegion = region;

            var begin     = document.GetOffset(region.Begin);
            var bodyStart = document.GetOffset(bodyRegion.Begin);

            var typeSignatureLength = bodyStart - begin;

            // Note: We remove extra spaces and newlines from the type
            // signature to make displaying it easier in Vim. Other
            // editors might not have a problem with displaying
            // results with multiple lines.
            var text = document.GetText
                ( offset: document.GetOffset(region.Begin)
                , length: typeSignatureLength)
                .MultipleWhitespaceCharsToSingleSpace();

            return text;
        }
    }
}
