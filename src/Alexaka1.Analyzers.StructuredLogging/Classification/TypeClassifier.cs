// Copyright (c) 2026 alexaka1

using Microsoft.CodeAnalysis;

namespace Alexaka1.Analyzers.StructuredLogging.Classification;

internal static class TypeClassifier
{
    public static bool NeedsDestructuring(ITypeSymbol? type)
    {
        if (type is null || type.TypeKind == TypeKind.Error)
        {
            return false;
        }

        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            type is INamedTypeSymbol nullable &&
            nullable.TypeArguments.Length == 1)
        {
            return NeedsDestructuringCore(nullable.TypeArguments[0], inspectingObjectItself: true);
        }

        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.None &&
            named.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic" &&
            named.MetadataName == "Dictionary`2" &&
            named.TypeArguments.Length == 2)
        {
            return NeedsDestructuringCore(named.TypeArguments[0], inspectingObjectItself: true);
        }

        if (TryGetEnumerableElement(type, out var element))
        {
            return NeedsDestructuringCore(element, inspectingObjectItself: true);
        }

        return NeedsDestructuringCore(type, inspectingObjectItself: true);
    }

    private static bool NeedsDestructuringCore(ITypeSymbol type, bool inspectingObjectItself)
    {
        if (type.SpecialType == SpecialType.System_Object)
        {
            return !inspectingObjectItself;
        }

        if (IsPredefinedNumeric(type) ||
            type.SpecialType == SpecialType.System_String ||
            IsGuid(type))
        {
            return false;
        }

        if (type.TypeKind is TypeKind.Interface or TypeKind.Struct or TypeKind.Enum or TypeKind.TypeParameter or TypeKind.Pointer or TypeKind.Delegate)
        {
            return false;
        }

        if (type.TypeKind == TypeKind.Array)
        {
            return type is IArrayTypeSymbol array && NeedsDestructuringCore(array.ElementType, inspectingObjectItself: true);
        }

        if (type.TypeKind != TypeKind.Class)
        {
            return false;
        }

        if (HasUsefulToString(type))
        {
            return false;
        }

        return type.BaseType is null || NeedsDestructuringCore(type.BaseType, inspectingObjectItself: false);
    }

    private static bool HasUsefulToString(ITypeSymbol type)
    {
        foreach (var member in type.GetMembers("ToString"))
        {
            if (member is IMethodSymbol { IsStatic: false, Parameters.Length: 0, ReturnType.SpecialType: SpecialType.System_String } method &&
                method.IsOverride)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGuid(ITypeSymbol type)
    {
        return type.ContainingNamespace?.ToDisplayString() == "System" && type.Name == "Guid";
    }

    private static bool IsPredefinedNumeric(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Char:
            case SpecialType.System_IntPtr:
            case SpecialType.System_UIntPtr:
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetEnumerableElement(ITypeSymbol type, out ITypeSymbol element)
    {
        element = null!;
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
            named.TypeArguments.Length == 1)
        {
            element = named.TypeArguments[0];
            return true;
        }

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T &&
                iface.TypeArguments.Length == 1)
            {
                element = iface.TypeArguments[0];
                return true;
            }
        }

        return false;
    }
}
