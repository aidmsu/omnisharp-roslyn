using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models;

namespace OmniSharp
{
    public partial class OmnisharpController
    {

        [HttpPost("signatureHelp")]
        public async Task<SignatureHelp> GetSignatureHelp(Request request)
        {
            foreach (var document in _workspace.GetDocuments(request.FileName))
            {
                var response = await GetSignatureHelp(document, request);
                if (response != null)
                {
                    return response;
                }
            }
            return null;
        }

        private async Task<SignatureHelp> GetSignatureHelp(Document document, Request request)
        {
            var sourceText = await document.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(request.Line - 1, request.Column - 1));

            var invocation = await FindInvocationExpression(document, position);
            if (invocation == null)
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync();
            var signatureHelp = new SignatureHelp();

            // define active parameter by position
            foreach (var comma in invocation.ArgumentList.Arguments.GetSeparators())
            {
                if (comma.Span.Start > position)
                {
                    break;
                }
                signatureHelp.ActiveParameter += 1;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(invocation.Expression);
            if (symbolInfo.Symbol is IMethodSymbol)
            {
                // method has no overloads
                signatureHelp.ActiveSignature = 0;
                signatureHelp.Signatures = new[] { BuildSignature(symbolInfo.Symbol as IMethodSymbol) };
            }
            else if (!symbolInfo.CandidateSymbols.IsEmpty)
            {
                // method has overloads
                var signatures = new List<SignatureHelpItem>();
                foreach (var symbol in symbolInfo.CandidateSymbols)
                {
                    var methodSymbol = symbol as IMethodSymbol;
                    if (methodSymbol == null)
                    {
                        continue;
                    }

                    if (methodSymbol.Parameters.Count() >= signatureHelp.ActiveParameter)
                    {
                        signatureHelp.ActiveSignature = signatures.Count;
                    }
                    signatures.Add(BuildSignature(methodSymbol));
                }
                signatureHelp.Signatures = signatures;
            }
            else
            {
                // not a method symbol and no overloads
                return null;
            }

            return signatureHelp;
        }

        private SignatureHelpItem BuildSignature(IMethodSymbol symbol)
        {
            var signature = new SignatureHelpItem();
            signature.Name = symbol.Name;
            signature.Documentation = symbol.GetDocumentationCommentXml();
            signature.Parameters = symbol.Parameters.Select(parameter =>
            {
                return new SignatureHelpParameter()
                {
                    Name = parameter.Name,
                    Documentation = parameter.GetDocumentationCommentXml()
                };
            });
            return signature;
        }

        private async Task<InvocationExpressionSyntax> FindInvocationExpression(Document document, int position)
        {
            var tree = await document.GetSyntaxTreeAsync();
            var node = tree.GetRoot().FindToken(position).Parent;

            while (node != null)
            {
                var invocation = node as InvocationExpressionSyntax;
                if (invocation != null && invocation.ArgumentList.FullSpan.IntersectsWith(position))
                {
                    return invocation;
                }
                node = node.Parent;
            }

            return null;
        }
    }
}
