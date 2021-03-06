﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace OmniSharp.AutoComplete
{
    public class SnippetGenerator
    {
        public ConversionFlags ConversionFlags { get; set; }
        CSharpFormattingOptions _policy;
        private TextWriterTokenWriter _writer;

        int _counter = 1;
        bool _includePlaceholders;

        public SnippetGenerator(bool includePlaceholders)
        {
            _includePlaceholders = includePlaceholders;
        }
        
        public string Generate(ISymbol symbol)
        {
            if (symbol == null)
                throw new ArgumentNullException("symbol");

            StringWriter writer = new StringWriter();
            _writer = new TextWriterTokenWriter(writer);
            _policy = FormattingOptionsFactory.CreateMono();

            TypeSystemAstBuilder astBuilder = CreateAstBuilder();
            astBuilder.AlwaysUseShortTypeNames = true;
            AstNode node = astBuilder.ConvertSymbol(symbol);


            if (symbol is ITypeDefinition)
                WriteTypeDeclarationName((ITypeDefinition)symbol, _writer, _policy);
            else if (symbol is IMember)
                WriteMemberDeclarationName((IMember)symbol, _writer, _policy);
            else
                _writer.WriteIdentifier(Identifier.Create(symbol.Name));

            if (HasParameters(symbol))
            {
                _writer.WriteToken(symbol.SymbolKind == SymbolKind.Indexer ? Roles.LBracket : Roles.LPar, symbol.SymbolKind == SymbolKind.Indexer ? "[" : "(");
                var parameters = new List<ParameterDeclaration>(node.GetChildrenByRole(Roles.Parameter));
                if (parameters.Any())
                {
                    WriteCommaSeparatedList(parameters);
                }
                _writer.WriteToken(symbol.SymbolKind == SymbolKind.Indexer ? Roles.RBracket : Roles.RPar, symbol.SymbolKind == SymbolKind.Indexer ? "]" : ")");
            }
            if (_includePlaceholders)
            {
                _writer.WriteToken(Roles.Text, "$0");
	
            }
            return writer.ToString();
        }

        static bool HasParameters(ISymbol e)
        {
            switch (e.SymbolKind)
            {
                case SymbolKind.TypeDefinition:
                case SymbolKind.Indexer:
                case SymbolKind.Method:
                case SymbolKind.Operator:
                case SymbolKind.Constructor:
                case SymbolKind.Destructor:
                    return true;
                default:
                    return false;
            }
        }

        TypeSystemAstBuilder CreateAstBuilder()
        {
            TypeSystemAstBuilder astBuilder = new TypeSystemAstBuilder();
            return astBuilder;
        }

        void WriteTypeDeclarationName(ITypeDefinition typeDef, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
        {
            var astBuilder = new TypeSystemAstBuilder();
            EntityDeclaration node = astBuilder.ConvertEntity(typeDef);
            if (typeDef.DeclaringTypeDefinition != null)
            {
                WriteTypeDeclarationName(typeDef.DeclaringTypeDefinition, writer, formattingPolicy);
                writer.WriteToken(Roles.Dot, ".");
            }
            writer.WriteIdentifier(node.NameToken);
            WriteTypeParameters(writer, node.GetChildrenByRole(Roles.TypeParameter));
        }

        public void WriteTypeParameters(TokenWriter writer, IEnumerable<AstNode> typeParameters)
        {
            if (typeParameters.Any())
            {
                writer.WriteToken(Roles.LChevron, "<");
                WriteCommaSeparatedList(typeParameters);
                writer.WriteToken(Roles.RChevron, ">");
            }
        }

        void WriteCommaSeparatedList(IEnumerable<AstNode> list)
        {
            var last = list.Last();
            foreach (AstNode node in list)
            {
                if (_includePlaceholders)
                {
                    _writer.WriteToken(Roles.Text, "$");
                    _writer.WriteToken(Roles.Text, "{");
                    _writer.WriteToken(Roles.Text, _counter.ToString());
                    _writer.WriteToken(Roles.Text, ":");
                }
                var outputVisitor = new CSharpOutputVisitor(_writer, _policy);
                node.AcceptVisitor(outputVisitor);

                if (_includePlaceholders)
                {
                    _writer.WriteToken(Roles.Text, "}");
                }

                if (node != last)
                {
                    this.Comma(node);
                }

                _counter++;
            }
        }

        /// <summary>
        /// Writes a space depending on policy.
        /// </summary>
        void Space(bool addSpace = true)
        {
            if (addSpace)
            {
                _writer.Space();
            }
        }

        /// <summary>
        /// Writes a comma.
        /// </summary>
        /// <param name="nextNode">The next node after the comma.</param>
        /// <param name="noSpaceAfterComma">When set prevents printing a space after comma.</param>
        void Comma(AstNode nextNode, bool noSpaceAfterComma = false)
        {
            Space(_policy.SpaceBeforeBracketComma);
            // TODO: Comma policy has changed.
            _writer.WriteToken(Roles.Comma, ",");
            Space(!noSpaceAfterComma && _policy.SpaceAfterBracketComma);
            // TODO: Comma policy has changed.
        }

        void WriteMemberDeclarationName(IMember member, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
        {
            TypeSystemAstBuilder astBuilder = CreateAstBuilder();
            EntityDeclaration node = astBuilder.ConvertEntity(member);
            switch (member.SymbolKind)
            {
                case SymbolKind.Indexer:
                    writer.WriteKeyword(Roles.Identifier, "this");
                    break;
                case SymbolKind.Constructor:
                    WriteQualifiedName(member.DeclaringType.Name, writer, formattingPolicy);
                    var typeNode = astBuilder.ConvertEntity(member.DeclaringTypeDefinition);
                    WriteTypeParameters(writer, typeNode.GetChildrenByRole(Roles.TypeParameter));
                    break;
                case SymbolKind.Destructor:
                    writer.WriteToken(DestructorDeclaration.TildeRole, "~");
                    WriteQualifiedName(member.DeclaringType.Name, writer, formattingPolicy);
                    break;
                case SymbolKind.Operator:
                    switch (member.Name)
                    {
                        case "op_Implicit":
                            writer.WriteKeyword(OperatorDeclaration.ImplicitRole, "implicit");
                            writer.Space();
                            writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
                            writer.Space();
                            ConvertType(member.ReturnType, writer, formattingPolicy);
                            break;
                        case "op_Explicit":
                            writer.WriteKeyword(OperatorDeclaration.ExplicitRole, "explicit");
                            writer.Space();
                            writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
                            writer.Space();
                            ConvertType(member.ReturnType, writer, formattingPolicy);
                            break;
                        default:
                            writer.WriteKeyword(OperatorDeclaration.OperatorKeywordRole, "operator");
                            writer.Space();
                            var operatorType = OperatorDeclaration.GetOperatorType(member.Name);
                            if (operatorType.HasValue)
                                writer.WriteToken(OperatorDeclaration.GetRole(operatorType.Value), OperatorDeclaration.GetToken(operatorType.Value));
                            else
                                writer.WriteIdentifier(node.NameToken);
                            break;
                    }
                    break;
                default:
                    writer.WriteIdentifier(Identifier.Create(member.Name));
                    break;
            }
        }

        void WriteQualifiedName(string name, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
        {
            var node = AstType.Create(name);
            var outputVisitor = new CSharpOutputVisitor(writer, formattingPolicy);
            node.AcceptVisitor(outputVisitor);
        }

        private void ConvertType(IType type, TokenWriter writer, CSharpFormattingOptions formattingPolicy)
        {
            TypeSystemAstBuilder astBuilder = CreateAstBuilder();
            astBuilder.AlwaysUseShortTypeNames = true;
            AstType astType = astBuilder.ConvertType(type);
            astType.AcceptVisitor(new CSharpOutputVisitor(writer, formattingPolicy));
        }
    }
}
