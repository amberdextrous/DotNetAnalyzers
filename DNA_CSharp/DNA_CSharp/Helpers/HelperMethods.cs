using Microsoft.CodeAnalysis.CodeRefactorings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNA.CSharp.Helpers
{
    static class HelperMethods
    {
        async static Task<bool> IsWPF(CodeRefactoringContext context)
        {
            var model = await context.Document.GetSemanticModelAsync();
            var isSupported = model.Compilation.GetTypeByMetadataName("System.Windows.Navigation.JournalEntry");

            if (isSupported == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
