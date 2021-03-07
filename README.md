# SourceGenSecrets

SourceGenSecrets makes use of the Source Generator functionality in Roslyn to assign values read from environment variables to class properties. This is useful in cases where you don't want to commit secrets into version control, or want to vary constants by environment/build.

As an example, suppose you create an environment variable `ClientId` with value `cf2d3966-1771-4604-85d2-46996b7c2040`. Using SourceGenSecrets, you can create a constants class:

```csharp
using SourceGenSecrets;

namespace MyApplication
{
    public static partial class Constants
    {
        [SourceGenSecret(EnvironmentVariableName = "ClientId")]
        public static string ClientId { get; private set; }
    }
}
````

If you were to inspect the resulting assmebly produced by the build, you will find that the `Constants` class appears to be:

```csharp
using SourceGenSecrets;

namespace MyApplication
{
    public static partial class Constants
    {
        [SourceGenSecret(EnvironmentVariableName = "ClientId")]
        public static string ClientId { get; private set; }

        static Constants()
        {
            ClientId = "cf2d3966-1771-4604-85d2-46996b7c2040"
        }
    }
}
````

## Usage

1. Ensure the class you want SourceGenSecrets to operate upon is a partial class.
1. Add `[SourceGenSecret(EnvironmentVariableName = "<Your Environment Variable Name")]` to your property.
1. Ensure you have an environment variable declared with the name you specified in the `SourceGenSecret` attribute on your property.
1. Build your project.

## License

The MIT License (MIT)

Copyright © 2020 Ryan Graham

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.