using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOEmbeddingService.Models
{
    public class MethodSourceInfo
    {
        public string Identifier { get; set; }
        public string Source { get; set; }
        public TextSpan Span {  get; set; }
        public MethodDeclarationSyntax Node { get; set; }
        public int Tokens { get; set; }
    }
}
