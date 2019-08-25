using NUnit.Framework;
using Shouldly;
using System;
using Techsola.EmbedDependencies.ILAsmSyntax;

namespace Techsola.EmbedDependencies.Tests.ILAsmSyntax
{
    public static class ILAsmParserTests
    {
        private static readonly TestFormattingTypeProvider P = TestFormattingTypeProvider.Instance;

        private static void AssertCallTree(string syntax, string expected)
        {
            ILAsmParser.ParseType(syntax, P).ShouldBe(expected);
        }

        private static T AssertException<T>(string syntax) where T : Exception
        {
            return Should.Throw<T>(() => ILAsmParser.ParseType(syntax, P));
        }

        [Test]
        public static void ParseType_should_throw_ArgumentException_for_whitespace([Values(null, "", " ")] string whitespace)
        {
            var ex = AssertException<ArgumentException>(whitespace);

            ex.ParamName.ShouldBe("typeSyntax");
        }

        [Test]
        public static void ParseType_should_throw_FormatException_for_invalid_character()
        {
            AssertException<FormatException>("/");
        }

        [Test]
        public static void Generic_type_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!{index}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_type_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!0x{index:x}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_type_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!0x{index:X}",
                P.GetGenericTypeParameter(index));
        }

        [Test]
        public static void Generic_method_parameter([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!{index}",
                P.GetGenericMethodParameter(index));
        }

        [Test]
        public static void Generic_method_parameter_using_lowercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!0x{index:x}",
                P.GetGenericMethodParameter(index));
        }

        [Test]
        public static void Generic_method_parameter_using_uppercase_hex([Values(0, 1, int.MaxValue)] int index)
        {
            AssertCallTree(
                $"!!0x{index:X}",
                P.GetGenericMethodParameter(index));
        }

        [TestCase("bool", PrimitiveTypeCode.Boolean)]
        [TestCase("char", PrimitiveTypeCode.Char)]
        [TestCase("float32", PrimitiveTypeCode.Single)]
        [TestCase("float64", PrimitiveTypeCode.Double)]
        [TestCase("int8", PrimitiveTypeCode.SByte)]
        [TestCase("int16", PrimitiveTypeCode.Int16)]
        [TestCase("int32", PrimitiveTypeCode.Int32)]
        [TestCase("int64", PrimitiveTypeCode.Int64)]
        [TestCase("native int", PrimitiveTypeCode.IntPtr)]
        [TestCase("native unsigned int", PrimitiveTypeCode.UIntPtr)]
        [TestCase("object", PrimitiveTypeCode.Object)]
        [TestCase("string", PrimitiveTypeCode.String)]
        [TestCase("typedref", PrimitiveTypeCode.TypedReference)]
        [TestCase("unsigned int8", PrimitiveTypeCode.Byte)]
        [TestCase("unsigned int16", PrimitiveTypeCode.UInt16)]
        [TestCase("unsigned int32", PrimitiveTypeCode.UInt32)]
        [TestCase("unsigned int64", PrimitiveTypeCode.UInt64)]
        [TestCase("void", PrimitiveTypeCode.Void)]
        public static void Primitives(string primitiveSyntax, PrimitiveTypeCode typeCode)
        {
            AssertCallTree(
                primitiveSyntax,
                P.GetPrimitiveType(typeCode));
        }

        [Test]
        public static void By_reference()
        {
            AssertCallTree(
                "bool&",
                P.GetByReferenceType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Pointer()
        {
            AssertCallTree(
                "bool*",
                P.GetPointerType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Pinned()
        {
            AssertCallTree(
                "bool pinned",
                P.GetPinnedType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean)));
        }

        [Test]
        public static void Simple_array()
        {
            AssertCallTree(
                "bool[]",
                P.GetArrayType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                    rank: 1));
        }

        [Test]
        public static void Multidimentional_array()
        {
            AssertCallTree(
                "bool[,,,]",
                P.GetArrayType(
                    P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                    rank: 4));
        }

        [TestCase("[...]")]
        [TestCase("[1]")]
        [TestCase("[1...]")]
        [TestCase("[1...2]")]
        [TestCase("[,...]")]
        [TestCase("[,,1]")]
        [TestCase("[,1...,]")]
        [TestCase("[1...2,,]")]
        public static void Array_with_bounds_not_supported(string syntax)
        {
            AssertException<NotSupportedException>("bool" + syntax);
        }

        [Test]
        public static void Nested_arrays()
        {
            AssertCallTree(
                "bool[,][][,,]",
                 P.GetArrayType(
                     P.GetArrayType(
                        P.GetArrayType(
                            P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                            rank: 2),
                        rank: 1),
                     rank: 3));
        }

        [Test]
        public static void Class_with_simple_name()
        {
            AssertCallTree(
                "class Foo",
                P.GetTypeFromReference(isValueType: false, null, "", "Foo"));
        }

        [Test]
        public static void Value_type_with_simple_name()
        {
            AssertCallTree(
                "valuetype Foo",
                P.GetTypeFromReference(isValueType: true, null, "", "Foo"));
        }

        [Test]
        public static void Simple_namespace_and_name()
        {
            AssertCallTree(
                "class SomeNamespace.Foo",
                P.GetTypeFromReference(false, null, "SomeNamespace", "Foo"));
        }

        [Test]
        public static void Dotted_namespace_and_name()
        {
            AssertCallTree(
                "class Some.Namespace.With.A.Lot.Of.Dots.Foo",
                P.GetTypeFromReference(false, null, "Some.Namespace.With.A.Lot.Of.Dots", "Foo"));
        }

        [Test]
        public static void Simple_nested_name()
        {
            AssertCallTree(
                "class Foo/Bar",
                P.GetTypeFromReference(false, null, "", "Foo", new[] { "Bar" }));
        }

        [Test]
        public static void Dotted_nested_name()
        {
            AssertCallTree(
                "class Foo/This.Is.Legal",
                P.GetTypeFromReference(false, null, "", "Foo", new[] { "This.Is.Legal" }));
        }

        [Test]
        public static void Multi_level_nested_name()
        {
            AssertCallTree(
                "class Foo/Bar/Baz",
                P.GetTypeFromReference(false, null, "", "Foo", new[] { "Bar", "Baz" }));
        }

        [Test]
        public static void Multi_level_nested_dotted_names()
        {
            AssertCallTree(
                "class A.B.C/D.E.F/G.H.I",
                P.GetTypeFromReference(false, null, "A.B", "C", new[] { "D.E.F", "G.H.I" }));
        }

        [Test]
        public static void Class_with_simple_assembly()
        {
            AssertCallTree(
                "class [a]Foo",
                P.GetTypeFromReference(isValueType: false, "a", "", "Foo"));
        }

        [Test]
        public static void Value_type_with_simple_assembly()
        {
            AssertCallTree(
                "valuetype [a]Foo",
                P.GetTypeFromReference(isValueType: true, "a", "", "Foo"));
        }

        [Test]
        public static void Assembly_with_dotted_name()
        {
            AssertCallTree(
                "class [a.b.c]Foo",
                P.GetTypeFromReference(isValueType: false, "a.b.c", "", "Foo"));
        }

        [Test]
        public static void Assembly_with_dotted_name_and_namespace()
        {
            AssertCallTree(
                "class [a.b.c]D.E.F",
                P.GetTypeFromReference(isValueType: false, "a.b.c", "D.E", "F"));
        }

        [Test]
        public static void Netmodule_is_not_supported()
        {
            AssertException<NotSupportedException>(
                "class [.module foo.netmodule]Foo");
        }

        [Test]
        public static void Simple_generic_instantion()
        {
            AssertCallTree(
                "class Foo<bool>",
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo"),
                    new[]
                    {
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean)
                    }));
        }

        [Test]
        public static void Complex_generic_instantion()
        {
            AssertCallTree(
                "class Foo/X<bool,class [a]Some.Namespace.Bar[,]<!0>>",
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo", new[] { "X" }),
                    new[]
                    {
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                        P.GetGenericInstantiation(
                            P.GetArrayType(
                                P.GetTypeFromReference(false, "a", "Some.Namespace", "Bar"),
                                rank: 2),
                            new[]
                            {
                                P.GetGenericTypeParameter(0)
                            })
                    }));
        }

        [Test]
        public static void ParseFieldReference_should_throw_ArgumentException_for_whitespace([Values(null, "", " ")] string whitespace)
        {
            var ex = Should.Throw<ArgumentException>(() => ILAsmParser.ParseFieldReference(whitespace, P));

            ex.ParamName.ShouldBe("fieldReferenceSyntax");
        }

        [Test]
        public static void ParseFieldReference_should_throw_FormatException_for_invalid_character()
        {
            Should.Throw<FormatException>(() => ILAsmParser.ParseFieldReference("/", P));
        }

        [Test]
        public static void Field_reference_with_type_spec()
        {
            var field = ILAsmParser.ParseFieldReference("bool ClassName::FieldName", P);

            field.FieldType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Boolean));

            field.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "ClassName"));

            field.FieldName.ShouldBe("FieldName");
        }

        [Test]
        public static void Field_reference_with_type_reference()
        {
            var field = ILAsmParser.ParseFieldReference("bool class ClassName::FieldName", P);

            field.FieldType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Boolean));

            field.DeclaringType.ShouldBe(P.GetTypeFromReference(false, null, "", "ClassName"));

            field.FieldName.ShouldBe("FieldName");
        }

        [Test]
        public static void Field_reference_with_nested_classes()
        {
            var field = ILAsmParser.ParseFieldReference("class A.B.C/D.E.F/G H.I.J/K.L.M/N::FieldName", P);

            field.FieldType.ShouldBe(P.GetTypeFromReference(
                isValueType: false,
                assemblyName: null,
                namespaceName: "A.B",
                topLevelTypeName: "C",
                nestedTypeNames: new[] { "D.E.F", "G" }));

            field.DeclaringType.ShouldBe(P.GetTypeFromReference(
                isValueType: null,
                assemblyName: null,
                namespaceName: "H.I",
                topLevelTypeName: "J",
                nestedTypeNames: new[] { "K.L.M", "N" }));

            field.FieldName.ShouldBe("FieldName");
        }

        [Test]
        public static void Field_reference_with_array_return_and_assemblies()
        {
            var field = ILAsmParser.ParseFieldReference("class [a] Foo[] class [a]Bar::FieldName", P);

            field.FieldType.ShouldBe(
                P.GetArrayType(
                    P.GetTypeFromReference(false, "a", "", "Foo"),
                    rank: 1));

            field.DeclaringType.ShouldBe(P.GetTypeFromReference(false, "a", "", "Bar"));

            field.FieldName.ShouldBe("FieldName");
        }

        [Test]
        public static void Field_reference_with_generics()
        {
            var field = ILAsmParser.ParseFieldReference("class Foo`1/Bar`1<!0, !1> class Foo`1/Bar`1<bool, int32>::FieldName", P);

            field.FieldType.ShouldBe(
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo`1", nestedTypeNames: new[] { "Bar`1" }),
                    new[]
                    {
                        P.GetGenericTypeParameter(0),
                        P.GetGenericTypeParameter(1)
                    }));

            field.DeclaringType.ShouldBe(
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo`1", nestedTypeNames: new[] { "Bar`1" }),
                    new[]
                    {
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                        P.GetPrimitiveType(PrimitiveTypeCode.Int32)
                    }));

            field.FieldName.ShouldBe("FieldName");
        }

        [Test]
        public static void ParseMethodReference_should_throw_ArgumentException_for_whitespace([Values(null, "", " ")] string whitespace)
        {
            var ex = Should.Throw<ArgumentException>(() => ILAsmParser.ParseMethodReference(whitespace, P));

            ex.ParamName.ShouldBe("methodReferenceSyntax");
        }

        [Test]
        public static void ParseMethodReference_should_throw_FormatException_for_invalid_character()
        {
            Should.Throw<FormatException>(() => ILAsmParser.ParseMethodReference("/", P));
        }

        [Test]
        public static void Method_reference_with_type_spec()
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::Bar()", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));

            method.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "Foo"));

            method.MethodName.ShouldBe("Bar");
        }

        [Test]
        public static void Method_reference_with_type_reference()
        {
            var method = ILAsmParser.ParseMethodReference("void class Foo::Bar()", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));

            method.DeclaringType.ShouldBe(P.GetTypeFromReference(false, null, "", "Foo"));

            method.MethodName.ShouldBe("Bar");
        }

        [Test]
        public static void Static_method()
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::Bar()", P);

            method.Instance.ShouldBeFalse();
            method.InstanceExplicit.ShouldBeFalse();
            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));
        }

        [Test]
        public static void Instance_method()
        {
            var method = ILAsmParser.ParseMethodReference("instance void Foo::Bar()", P);

            method.Instance.ShouldBeTrue();
            method.InstanceExplicit.ShouldBeFalse();
            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));
        }

        [Test]
        public static void Instance_explicit_method()
        {
            var method = ILAsmParser.ParseMethodReference("instance explicit void Foo::Bar()", P);

            method.Instance.ShouldBeTrue();
            method.InstanceExplicit.ShouldBeTrue();
            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));
        }

        [Test]
        public static void Method_reference_without_type_spec_is_not_supported()
        {
            Should.Throw<NotSupportedException>(() => ILAsmParser.ParseMethodReference("void Bar()", P));
        }

        [Test]
        public static void Default_calling_convention_may_be_specified()
        {
            ILAsmParser.ParseMethodReference("default void Foo::Bar()", P);
        }

        [Test]
        public static void Unmanaged_calling_convention_is_not_supported([Values("cdecl", "fastcall", "stdcall", "thiscall")] string unmanagedConvention)
        {
            Should.Throw<NotSupportedException>(() => ILAsmParser.ParseMethodReference("unmanaged " + unmanagedConvention + " void Bar()", P));
        }

        [Test]
        public static void Vararg_calling_convention_is_not_supported()
        {
            Should.Throw<NotSupportedException>(() => ILAsmParser.ParseMethodReference("vararg void Bar()", P));
        }

        [Test]
        public static void Special_method_names([Values(".ctor", ".cctor")] string specialName)
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::" + specialName + "()", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));

            method.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "Foo"));

            method.MethodName.ShouldBe(specialName);
        }

        [Test]
        public static void Dotted_method_name()
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::This.Is.Legal()", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));

            method.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "Foo"));

            method.MethodName.ShouldBe("This.Is.Legal");
        }

        [Test]
        public static void Single_method_parameter()
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::Bar(bool)", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));
            method.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "Foo"));
            method.MethodName.ShouldBe("Bar");

            method.Parameters.ShouldBe(new[]
            {
                P.GetPrimitiveType(PrimitiveTypeCode.Boolean)
            });
        }

        [Test]
        public static void Array_and_generic_method_parameters()
        {
            var method = ILAsmParser.ParseMethodReference("void Foo::Bar(bool[,][,], class Foo<bool, int32>, bool)", P);

            method.ReturnType.ShouldBe(P.GetPrimitiveType(PrimitiveTypeCode.Void));
            method.DeclaringType.ShouldBe(P.GetTypeFromReference(null, null, "", "Foo"));
            method.MethodName.ShouldBe("Bar");

            method.Parameters.ShouldBe(new[]
            {
                P.GetArrayType(
                    P.GetArrayType(
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                        rank: 2),
                    rank: 2),
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo"),
                    new[]
                    {
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                        P.GetPrimitiveType(PrimitiveTypeCode.Int32)
                    }),
                P.GetPrimitiveType(PrimitiveTypeCode.Boolean)
            });
        }

        [Test]
        public static void Varargs_method_parameters_not_supported()
        {
            Should.Throw<NotSupportedException>(() => ILAsmParser.ParseMethodReference("void Foo::Bar(...)", P));
        }

        [Test]
        public static void Generic_method_instantiation()
        {
            var method = ILAsmParser.ParseMethodReference("class Foo`1/Bar`1<!0, !1> class Foo`1/Bar`1<bool, int32>::MethodName<!1, string>(!0, !1, !!0)", P);

            method.ReturnType.ShouldBe(
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo`1", new[] { "Bar`1" }),
                    new[]
                    {
                        P.GetGenericTypeParameter(0),
                        P.GetGenericTypeParameter(1)
                    }));

            method.DeclaringType.ShouldBe(
                P.GetGenericInstantiation(
                    P.GetTypeFromReference(false, null, "", "Foo`1", new[] { "Bar`1" }),
                    new[]
                    {
                        P.GetPrimitiveType(PrimitiveTypeCode.Boolean),
                        P.GetPrimitiveType(PrimitiveTypeCode.Int32)
                    }));

            method.MethodName.ShouldBe("MethodName");

            method.GenericArguments.ShouldBe(new[]
            {
                P.GetGenericTypeParameter(1),
                P.GetPrimitiveType(PrimitiveTypeCode.String)
            });

            method.Parameters.ShouldBe(new[]
            {
                P.GetGenericTypeParameter(0),
                P.GetGenericTypeParameter(1),
                P.GetGenericMethodParameter(0)
            });
        }
    }
}
