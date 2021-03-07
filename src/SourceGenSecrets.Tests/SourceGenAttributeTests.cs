using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenSecrets;
using Xunit;

namespace RoslynSecrets.Tests
{
	public class SourceGenAttributeTests
	{
		[Fact]
		public void TopLevelReplacement()
		{
			var source = new StringBuilder("using SourceGenSecrets;");
			source.Append(File.ReadAllText("SourceGenSecretAttribute.cs"));
			source.Append(@"
            namespace MyCode
            {
                public static partial class Bar
                {"
			);
			source.Append(GetPropertiesSection());
			source.Append(@"}
            }");

			SetEnvironmentVariables();

			Compilation comp = CreateCompilation(source.ToString());
			var newComp = RunGenerators(comp, out var generatorDiags, new SourceGenSecretsGenerator());
			Assert.Empty(generatorDiags);
			Assert.Empty(newComp.GetDiagnostics());
		}

		[Fact]
		public void NestedClassReplacement()
		{
			var source = new StringBuilder("using SourceGenSecrets;");
			source.Append(File.ReadAllText("SourceGenSecretAttribute.cs"));
			source.Append(@"
			namespace MyCode
            {
                public static partial class Foo
                {
                    public static partial class Bar
                    {
			");
			source.Append(GetPropertiesSection());
			source.Append(@"}
                }
            }");

			SetEnvironmentVariables();

			Compilation comp = CreateCompilation(source.ToString());
			var newComp = RunGenerators(comp, out var generatorDiags, new SourceGenSecretsGenerator());
			Assert.Empty(generatorDiags);
			Assert.Empty(newComp.GetDiagnostics());
		}

		private string GetPropertiesSection()
		{
			return @"
				[SourceGenSecret(EnvironmentVariableName = ""StringValue"")]
                public static string StringValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""CharValue"")]
                public static char CharValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""BoolValue"")]
                public static bool BoolValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""IntValue"")]
                public static int IntValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""DecimalValue"")]
				public static decimal DecimalValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""DecimalValueWithoutSuffix"")]
				public static decimal DecimalValueWithoutSuffix { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""FloatValue"")]
				public static float FloatValue { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""FloatValueWithoutSuffix"")]
				public static float FloatValueWithoutSuffix { get; private set; }

				[SourceGenSecret(EnvironmentVariableName = ""DoubleValue"")]
				public static double DoubleValue { get; private set; }
			";
		}

		private void SetEnvironmentVariables()
		{
			Environment.SetEnvironmentVariable("StringValue", "Test String");
			Environment.SetEnvironmentVariable("CharValue", "a");
			Environment.SetEnvironmentVariable("BoolValue", "true");
			Environment.SetEnvironmentVariable("IntValue", "100");
			Environment.SetEnvironmentVariable("DecimalValue", "98.6M");
			Environment.SetEnvironmentVariable("DecimalValueWithoutSuffix", "98.6");
			Environment.SetEnvironmentVariable("FloatValue", "98.6f");
			Environment.SetEnvironmentVariable("FloatValueWithoutSuffix", "98.6");
			Environment.SetEnvironmentVariable("DoubleValue", "98.6");
		}

		private static Compilation CreateCompilation(string source)
			=> CSharpCompilation.Create("compilation",
				new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
				new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

		private static GeneratorDriver CreateDriver(Compilation c, params ISourceGenerator[] generators)
			=> CSharpGeneratorDriver.Create(generators, parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options);

		private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
		{
			CreateDriver(c, generators).RunGeneratorsAndUpdateCompilation(c, out var d, out diagnostics);
			return d;
		}
	}
}
