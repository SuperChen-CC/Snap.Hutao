﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Snap.Hutao.SourceGeneration.DependencyInjection;

/// <summary>
/// 注入代码生成器
/// 旨在使用源生成器提高注入效率
/// 防止在运行时动态查找注入类型
/// </summary>
[Generator]
internal sealed class InjectionGenerator : ISourceGenerator
{
    private const string InjectAsSingletonName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Singleton";
    private const string InjectAsTransientName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Transient";
    private const string InjectAsScopedName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectAs.Scoped";
    private const string CRLF = "\r\n";

    /// <inheritdoc/>
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a syntax receiver that will be created for each generation pass
        context.RegisterForSyntaxNotifications(() => new InjectionSyntaxContextReceiver());
    }

    /// <inheritdoc/>
    public void Execute(GeneratorExecutionContext context)
    {
        // retrieve the populated receiver
        if (context.SyntaxContextReceiver is not InjectionSyntaxContextReceiver receiver)
        {
            return;
        }

        StringBuilder sourceCodeBuilder = new();
        sourceCodeBuilder.Append($$"""
            // Copyright (c) DGP Studio. All rights reserved.
            // Licensed under the MIT license.

            namespace Snap.Hutao.Core.DependencyInjection;
            
            internal static partial class ServiceCollectionExtension
            {
                [global::System.CodeDom.Compiler.GeneratedCodeAttribute("{{nameof(InjectionGenerator)}}","1.0.0.0")]
                [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
                public static partial IServiceCollection AddInjections(this IServiceCollection services)
                {
            """);

        FillWithInjectionServices(receiver, sourceCodeBuilder);
        sourceCodeBuilder.Append("""

                    return services;
                }
            }
            """);

        context.AddSource("ServiceCollectionExtension.g.cs", SourceText.From(sourceCodeBuilder.ToString(), Encoding.UTF8));
    }

    private static void FillWithInjectionServices(InjectionSyntaxContextReceiver receiver, StringBuilder sourceCodeBuilder)
    {
        List<string> lines = new();
        StringBuilder lineBuilder = new();

        foreach (INamedTypeSymbol classSymbol in receiver.Classes)
        {
            AttributeData injectionInfo = classSymbol
                .GetAttributes()
                .Single(attr => attr.AttributeClass!.ToDisplayString() == InjectionSyntaxContextReceiver.AttributeName);

            lineBuilder
                .Clear()
                .Append(CRLF);

            ImmutableArray<TypedConstant> arguments = injectionInfo.ConstructorArguments;
            TypedConstant injectAs = arguments[0];

            string injectAsName = injectAs.ToCSharpString();
            switch (injectAsName)
            {
                case InjectAsSingletonName:
                    lineBuilder.Append(@"        services.AddSingleton<");
                    break;
                case InjectAsTransientName:
                    lineBuilder.Append(@"        services.AddTransient<");
                    break;
                case InjectAsScopedName:
                    lineBuilder.Append(@"        services.AddScoped<");
                    break;
                default:
                    throw new InvalidOperationException($"非法的 InjectAs 值: [{injectAsName}]");
            }

            if (arguments.Length == 2)
            {
                TypedConstant interfaceType = arguments[1];
                lineBuilder.Append($"{interfaceType.Value}, ");
            }

            lineBuilder.Append($"{classSymbol.ToDisplayString()}>();");

            lines.Add(lineBuilder.ToString());
        }

        foreach (string line in lines.OrderBy(x => x))
        {
            sourceCodeBuilder.Append(line);
        }
    }

    private class InjectionSyntaxContextReceiver : ISyntaxContextReceiver
    {
        /// <summary>
        /// 注入特性的名称
        /// </summary>
        public const string AttributeName = "Snap.Hutao.Core.DependencyInjection.Annotation.InjectionAttribute";

        /// <summary>
        /// 所有需要注入的类型符号
        /// </summary>
        public List<INamedTypeSymbol> Classes { get; } = new();

        /// <inheritdoc/>
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            // any class with at least one attribute is a candidate for injection generation
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0)
            {
                // get as named type symbol
                if (context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is INamedTypeSymbol classSymbol)
                {
                    if (classSymbol.GetAttributes().Any(ad => ad.AttributeClass!.ToDisplayString() == AttributeName))
                    {
                        Classes.Add(classSymbol);
                    }
                }
            }
        }
    }
}