﻿using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Formatting;

namespace DNA.CSharp.MVVM
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(MakeRelayCommandRefactoring)), Shared]
    public class MakeRelayCommandRefactoring : CodeRefactoringProvider
    {
        private string Title = "Implement RelayCommand<T>";
        public async sealed override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // Find the node at the selection.
            var node = root.FindNode(context.Span);

            // Only offer a refactoring if the selected node is a class statement node.
            var classDecl = node as ClassDeclarationSyntax;
            if (classDecl == null)
            {
                return;
            }
            var action = CodeAction.Create(title:Title, createChangedDocument:c => RebuildClassAsync(context.Document, classDecl, c), equivalenceKey:Title);

            // Register this code action.
            context.RegisterRefactoring(action);
        }

        private async Task<Document> RebuildClassAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {

            var newImplementation = @"
	private bool _isEnabled;

	private readonly Action _handler;
	public RelayCommand(Action handler)
	{
		_handler = handler;
	}

	public event EventHandler CanExecuteChanged;

	public bool IsEnabled {
		get { return _isEnabled; }
		set {
			if ((value != _isEnabled)) {
				_isEnabled = value;
				if (CanExecuteChanged != null) {
					CanExecuteChanged(this, EventArgs.Empty);
				}
			}
		}
	}

	public bool CanExecute(object parameter)
	{
		return IsEnabled;
	}

	public void Execute(object parameter)
	{
		_handler();
	}
";

            var newClassTree = SyntaxFactory.ParseSyntaxTree(newImplementation).
        GetRoot().DescendantNodes().
        Where(n => n.IsKind(SyntaxKind.FieldDeclaration) || n.IsKind(SyntaxKind.MethodDeclaration) || n.IsKind(SyntaxKind.PropertyDeclaration)
                || n.IsKind(SyntaxKind.ConstructorDeclaration) || n.IsKind(SyntaxKind.EventDeclaration) || n.IsKind(SyntaxKind.EventFieldDeclaration)).
        Cast<MemberDeclarationSyntax>().
        Select(decl => decl.WithAdditionalAnnotations(Formatter.Annotation, Simplifier.Annotation)).
        ToArray();


            ClassDeclarationSyntax newClassBlock = SyntaxFactory.ClassDeclaration("RelayCommand").AddTypeParameterListParameters(SyntaxFactory.TypeParameter("T")).WithOpenBraceToken(SyntaxFactory.ParseToken("{")).
                    WithCloseBraceToken(SyntaxFactory.ParseToken("}").WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed));

            newClassBlock = newClassBlock.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("ICommand")));

            var newClassNode = newClassBlock.AddMembers(newClassTree);

            var root = await document.GetSyntaxRootAsync();

            var newRoot = root.ReplaceNode(classDeclaration, newClassNode);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }
    }
}