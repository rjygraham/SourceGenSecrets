using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace SourceGenSecrets
{
	[Generator]
	public class SourceGenSecretsGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			// Register a syntax receiver that will be created for each generation pass
			context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// retrieve the populated receiver 
			if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
			{
				return;
			}

			INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("SourceGenSecrets.SourceGenSecretAttribute");

			// Group the properties by class, and generate the source
			foreach (IGrouping<INamedTypeSymbol, IPropertySymbol> group in receiver.Properties.GroupBy(f => f.ContainingType))
			{
				string classSource = ProcessGroup(group.Key, group.ToList(), attributeSymbol);
				context.AddSource($"{group.Key.Name}.g.cs", SourceText.From(classSource, Encoding.UTF8));
			}
		}

		private string ProcessGroup(INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, ISymbol attributeSymbol)
		{
			var source = new StringBuilder();
			ProcessNamespace(ref source, classSymbol, properties, attributeSymbol);
			return source.ToString();
		}

		private void ProcessNamespace(ref StringBuilder source, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, ISymbol attributeSymbol)
		{
			var namespaceName = classSymbol.ContainingNamespace.ToString();
			source.Append($"namespace {namespaceName} {{");

			if (classSymbol.ContainingType != null)
			{
				ProcessContainingType(ref source, classSymbol.ContainingType, classSymbol, properties, attributeSymbol);
			}
			else
			{
				ProcessClass(ref source, classSymbol, properties, attributeSymbol);
			}

			source.Append("}");
		}

		private void ProcessContainingType(ref StringBuilder source, INamedTypeSymbol containingTypeSymbol, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, ISymbol attributeSymbol)
		{
			var accessibility = containingTypeSymbol.DeclaredAccessibility.ToString().ToLower();
			var @static = containingTypeSymbol.IsStatic
				? "static"
				: "";

			source.Append($"{accessibility} {@static} partial class {containingTypeSymbol.Name} {{");

			if (containingTypeSymbol.ContainingType != null)
			{
				ProcessContainingType(ref source, containingTypeSymbol.ContainingType, classSymbol, properties, attributeSymbol);
			}
			else
			{
				ProcessClass(ref source, classSymbol, properties, attributeSymbol);
			}

			source.Append("}");
		}

		private void ProcessClass(ref StringBuilder source, INamedTypeSymbol classSymbol, List<IPropertySymbol> properties, ISymbol attributeSymbol)
		{
			var accessibility = classSymbol.DeclaredAccessibility.ToString().ToLower();
			var @static = classSymbol.IsStatic
				? "static"
				: "";

			source.Append($"{accessibility} {@static} partial class {classSymbol.Name} {{");
			source.Append($"static {classSymbol.Name} () {{");

			ProcessProperties(ref source, properties, attributeSymbol);

			source.Append("}}");
		}

		private void ProcessProperties(ref StringBuilder source, List<IPropertySymbol> properties, ISymbol attributeSymbol)
		{
			foreach (var property in properties)
			{
				var attribute = property.GetAttributes().SingleOrDefault(s => s.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
				var environmentVariableName = attribute.NamedArguments.SingleOrDefault(kvp => kvp.Key == "EnvironmentVariableName").Value;
				var value = Environment.GetEnvironmentVariable(environmentVariableName.Value.ToString());

				switch (property.Type.Name.ToLower())
				{
					case "string":
						source.Append($"{property.Name} = \"{value}\";");
						break;
					case "char":
						source.Append($"{property.Name} = '{value}';");
						break;
					case "decimal":
						if (value.EndsWith("M"))
						{
							source.Append($"{property.Name} = {value};");
						}
						else
						{
							source.Append($"{property.Name} = {value}M;");
						}
						break;
					case "single":
						if (value.EndsWith("f"))
						{
							source.Append($"{property.Name} = {value};");
						}
						else
						{
							source.Append($"{property.Name} = {value}f;");
						}
						break;
					default:
						source.Append($"{property.Name} = {value};");
						break;
				}
			}
		}

		/// <summary>
		/// Created on demand before each generation pass
		/// </summary>
		class SyntaxReceiver : ISyntaxContextReceiver
		{
			public List<IPropertySymbol> Properties { get; } = new List<IPropertySymbol>();

			/// <summary>
			/// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
			/// </summary>
			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				if (context.Node is AttributeSyntax attribute && attribute.Name.ToString().Equals("SourceGenSecret"))
				{
					var propertySymbol = context.SemanticModel.GetDeclaredSymbol(attribute.Parent.Parent) as IPropertySymbol;
					if (propertySymbol != null)
					{
						Properties.Add(propertySymbol);
					}
				}
			}
		}

	}
}
