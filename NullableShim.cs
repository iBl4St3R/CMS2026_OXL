// NullableShim.cs
// Fixes CS0656: missing NullableAttribute in MelonLoader / Il2Cpp .NET 6 projects

namespace System.Runtime.CompilerServices
{
    internal sealed class NullableAttribute : Attribute
    {
        public NullableAttribute(byte _) { }
        public NullableAttribute(byte[] _) { }
    }

    internal sealed class NullableContextAttribute : Attribute
    {
        public NullableContextAttribute(byte _) { }
    }

    internal sealed class NullablePublicOnlyAttribute : Attribute
    {
        public NullablePublicOnlyAttribute(bool _) { }
    }
}