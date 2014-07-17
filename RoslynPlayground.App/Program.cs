using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace RoslynPlayground.App
{
    class Program
    {
        static void Main(string[] args)
        {
            var tree = CSharpSyntaxTree.ParseFile(".\\TestFile.cs");

            var root = tree.GetRoot().KillRegions();
            
            var text = root.GetText().ToString();
            Console.WriteLine(text);

            //var newRoot = root.ReplaceTrivia(children, (trivia, syntaxTrivia) => new SyntaxTrivia());

            //var location1 = startRegion.GetLocation();
            //var location2 = endRegion.GetLocation();

            //var startNode = startRegion.GetStructure();
            //var endNode = endRegion.GetStructure();

            //var startText = startNode.GetText();
            //var endText = endNode.GetText();

            // root = root.RemoveNode(startNode, SyntaxRemoveOptions.KeepDirectives);
            //root.ReplaceNode(endNode, (SyntaxNode)null);
        }
    }

    public static class SyntaxNodeExtensions
    {
        public static SyntaxNode KillRegions(this SyntaxNode node)
        {
            var childsWithTrivias = node
                .DescendantNodesAndTokens()
                .Where(child => child.HasLeadingTrivia)
                .SelectMany(child => child.GetLeadingTrivia(), (child, leadingTrivia) => new {child, leadingTrivia})
                .ToList();

            var childs = childsWithTrivias.Select(ct => ct.child).ToList();

            var startChild = childsWithTrivias
                .Where(t => (t.leadingTrivia.CSharpKind() == SyntaxKind.RegionDirectiveTrivia))
                .Select(t => t.child)
                .First();

            var endChild = childsWithTrivias
                .Where(t => (t.leadingTrivia.CSharpKind() == SyntaxKind.EndRegionDirectiveTrivia))
                .Select(t => t.child)
                .Last();

            var startIndex = childs.IndexOf(startChild);
            var endIndex = childs.IndexOf(endChild);

            var childsToRemove = childs
                .Where((c, i) => i >= startIndex && i <= endIndex)
                .SelectMany(c => c.GetLeadingTrivia());


            return node.ReplaceTrivia(childsToRemove, (_, __) => new SyntaxTrivia());
        }


        // see: http://magenic.com/BlogArchive/ModifyingCodewithProjectRoslyn
        public static SyntaxNode Deregionize(this SyntaxNode node)
        {
            var nodesWithRegionDirectives = node
                .DescendantNodesAndTokens()
                .Where(child => child.HasLeadingTrivia)
                .SelectMany(child => child.GetLeadingTrivia(), (child, leadingTrivia) => new { child, leadingTrivia })
                .Where(t => (t.leadingTrivia.CSharpKind() == SyntaxKind.RegionDirectiveTrivia ||
                              t.leadingTrivia.CSharpKind() == SyntaxKind.EndRegionDirectiveTrivia))
                .Select(t => t.child);

            var triviaToRemove = new List<SyntaxTrivia>();

            foreach (var nodeWithRegionDirective in nodesWithRegionDirectives)
            {
                var triviaList = nodeWithRegionDirective.GetLeadingTrivia();

                for (var i = 0; i < triviaList.Count; i++)
                {
                    var currentTrivia = triviaList[i];

                    if (currentTrivia.CSharpKind() == SyntaxKind.RegionDirectiveTrivia ||
                        currentTrivia.CSharpKind() == SyntaxKind.EndRegionDirectiveTrivia)
                    {
                        triviaToRemove.Add(currentTrivia);

                        if (i > 0)
                        {
                            var previousTrivia = triviaList[i - 1];
                            if (previousTrivia.CSharpKind() == SyntaxKind.WhitespaceTrivia)
                            {
                                triviaToRemove.Add(previousTrivia);
                            }
                        }
                    }
                }
            }

            return triviaToRemove.Count > 0
                ? node.ReplaceTrivia(triviaToRemove, (_, __) => new SyntaxTrivia())
                : node;
        }
    }
}
