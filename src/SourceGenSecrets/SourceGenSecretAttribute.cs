using System;

namespace SourceGenSecrets
{
	[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class SourceGenSecretAttribute : Attribute
	{
		public SourceGenSecretAttribute()
		{
		}
		public string EnvironmentVariableName { get; set; }
	}
}
