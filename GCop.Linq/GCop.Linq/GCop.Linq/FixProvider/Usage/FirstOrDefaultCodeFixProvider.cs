﻿namespace GCop.Linq.FixProvider.Usage
{
    using Core;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(FirstOrDefaultCodeFixProvider)), Shared]
    public class FirstOrDefaultCodeFixProvider : GCopCodeFixProvider
    {
        private string Title => "Remove Any method and use FirstOfDefault method";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("GCop513");

        protected override void RegisterCodeFix()
        {
            var token = Root.FindToken(DiagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConditionalExpressionSyntax>().FirstOrDefault();
            if (token == null) return;
            Context.RegisterCodeFix(CodeAction.Create(Title, action => RemoveAny(Context.Document, token, action), Title), Diagnostic);
        }

        private async Task<Document> RemoveAny(Document document, ConditionalExpressionSyntax conditional, CancellationToken cancellationToken)
        {
            ExpressionSyntax newExpression = null;
            try
            {
                var leftInvocation = conditional.Condition as InvocationExpressionSyntax;
                var identifier = leftInvocation.DescendantNodes().OfType<MemberAccessExpressionSyntax>().LastOrDefault()?.GetIdentifier();
                var argument = leftInvocation.ArgumentList.Arguments.FirstOrDefault()?.ToString();
                newExpression = SyntaxFactory.ParseExpression(identifier + ".FirstOrDefault(" + argument + ") ?? " + conditional.WhenFalse.ToString());
            }
            catch (Exception ex)
            {
                //No logging needed
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(conditional, newExpression);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}