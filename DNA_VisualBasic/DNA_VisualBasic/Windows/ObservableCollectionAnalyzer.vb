Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class ObservableCollectionAnalyzer
    Inherits DiagnosticAnalyzer

    Public Const DiagnosticId = "DNA001"

    ' You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    Friend Shared ReadOnly Title As String = "List(Of T) is improper for data-binding"
    Friend Shared ReadOnly MessageFormat As String = "Assignment '{0}' is of type List(Of T)"
    Friend Shared ReadOnly Description As String = "Use ObservableCollection(Of T) instead of List(Of T) for data-binding"
    Friend Const Category = "Syntax"

    Friend Shared Rule As New DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, True, helpLinkUri:="https://github.com/AlessandroDelSole/DotNetAnalyzers/wiki/DNA001---List(Of-T)-is-improper-for-data-binding")

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        ' TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeAssignementStatements, SyntaxKind.SimpleAssignmentStatement)
    End Sub

    Public Sub AnalyzeAssignementStatements(context As SyntaxNodeAnalysisContext)
        Dim node = TryCast(context.Node, AssignmentStatementSyntax)
        If node Is Nothing Then
            Return
        End If

        'Controlla se il nodo contiene ItemsSource o DataContext,
        'che sono proprietà "bindabili"
        Dim leftPart = node.Left.ToFullString
        If leftPart.Contains("ItemsSource") = False Then
            If leftPart.Contains("DataContext") = False Then
                Return
            End If
        End If

        Dim symbol = TryCast(context.SemanticModel.GetSymbolInfo(node.Right).Symbol, ILocalSymbol)
        If symbol Is Nothing Then
            Return
        End If

        Dim coll = symbol.Locations.First
        Dim collString = coll.ToString
        Dim collNodeType = coll.SourceTree
        Dim collLine = coll.GetLineSpan
        Dim def = symbol.OriginalDefinition

        Dim returnType = symbol.Type.ToString
        If returnType.ToLowerInvariant.Contains("list(of") Then
            Dim diagn = Diagnostic.Create(Rule, node.Right.GetLocation,
                      node.ToString)
            context.ReportDiagnostic(diagn)
        End If
    End Sub
End Class