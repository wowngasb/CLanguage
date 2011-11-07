﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CLanguage
{
    public abstract class CType
    {
        public TypeQualifiers TypeQualifiers { get; set; }

        public Location Location { get; set; }

        public abstract int GetSize(EmitContext c);

        public static readonly CVoidType Void = new CVoidType();

        public virtual bool IsIntegral
        {
            get
            {
                return false;
            }
        }

        public CType()
        {
            Location = Location.Null;
        }

        public virtual bool IsVoid
        {
            get
            {
                return false;
            }
        }
    }

    public enum Signedness
    {
        Unsigned = 0,
        Signed = 1,
    }

    public class CBasicType : CType
    {
        public string Name { get; private set; }
        public Signedness Signedness { get; private set; }
        public string Size { get; private set; }

        public CBasicType(string name, Signedness signedness, string size)
        {
            Name = name;
            Signedness = signedness;
            Size = size;
        }

        public override bool IsIntegral
        {
            get
            {
                return Name == "int" || Name == "char";
            }
        }

        public static readonly CBasicType ConstChar = new CBasicType("char", Signedness.Unsigned, "") { TypeQualifiers = TypeQualifiers.Const };
        public static readonly CBasicType UnsignedChar = new CBasicType("char", Signedness.Unsigned, "");
        public static readonly CBasicType SignedChar = new CBasicType("char", Signedness.Signed, "");
        public static readonly CBasicType UnsignedShortInt = new CBasicType("int", Signedness.Unsigned, "short");
        public static readonly CBasicType SignedShortInt = new CBasicType("int", Signedness.Signed, "short");
        public static readonly CBasicType UnsignedInt = new CBasicType("int", Signedness.Unsigned, "");
        public static readonly CBasicType SignedInt = new CBasicType("int", Signedness.Signed, "");
        public static readonly CBasicType UnsignedLongInt = new CBasicType("int", Signedness.Unsigned, "long");
        public static readonly CBasicType SignedLongInt = new CBasicType("int", Signedness.Signed, "long");
        public static readonly CBasicType UnsignedLongLongInt = new CBasicType("int", Signedness.Unsigned, "long long");
        public static readonly CBasicType SignedLongLongInt = new CBasicType("int", Signedness.Signed, "long long");
        public static readonly CBasicType Float = new CBasicType("float", Signedness.Signed, "");
        public static readonly CBasicType Double = new CBasicType("double", Signedness.Signed, "");

        public override int GetSize(EmitContext c)
        {
            if (Name == "char")
            {
                return c.MachineInfo.CharSize;
            }
            else if (Name == "int")
            {
                if (Size == "short")
                {
                    return c.MachineInfo.ShortIntSize;
                }
                else if (Size == "long")
                {
                    return c.MachineInfo.LongIntSize;
                }
                else if (Size == "long long")
                {
                    return c.MachineInfo.LongLongIntSize;
                }
                else
                {
                    return c.MachineInfo.IntSize;
                }
            }
            else if (Name == "float")
            {
                return c.MachineInfo.FloatSize;
            }
            else if (Name == "double")
            {
                return c.MachineInfo.DoubleSize;
            }
            else
            {
                throw new NotSupportedException(this.ToString());
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Signedness, Size, Name);
        }
    }

    public class CFunctionType : CType
    {
        public class Parameter
        {
            public string Name { get; set; }
            public CType ParameterType { get; set; }
            public Parameter(string name, CType parameterType)
            {
                Name = name;
                ParameterType = parameterType;
            }
            public override string ToString()
            {
                return ParameterType + " " + Name;
            }
        }

        public CType ReturnType { get; private set; }
        public List<Parameter> Parameters { get; private set; }

        public CFunctionType(CType returnType)
        {
            ReturnType = returnType;
            Parameters = new List<Parameter>();
        }

        public override int GetSize(EmitContext c)
        {
            return c.MachineInfo.PointerSize;
        }

        public override string ToString()
        {
            var s = "(Function " + ReturnType + " (";
            var head = "";
            foreach (var p in Parameters)
            {
                s += head;
                s += p;
                head = " ";
            }
            s += "))";
            return s;
        }
    }

    public class CPointerType : CType
    {
        public CType InnerType { get; private set; }

        public CPointerType(CType innerType)
        {
            InnerType = innerType;
        }

        public static readonly CPointerType PointerToConstChar = new CPointerType(CBasicType.ConstChar);

        public override int GetSize(EmitContext c)
        {
            return c.MachineInfo.PointerSize;
        }

        public override string ToString()
        {
            return "(Pointer " + InnerType + ")";
        }
    }

    public class CVoidType : CType
    {
        public CVoidType()
        {
        }

        public override bool IsVoid
        {
            get
            {
                return true;
            }
        }

        public override int GetSize(EmitContext c)
        {
            c.Report.Error(2070, "'void': illegal sizeof operand");
            return 0;
        }

        public override string ToString()
        {
            return "void";
        }
    }

    public class CArrayType : CType
    {
        public CType ElementType { get; private set; }
        public Expression LengthExpression { get; set; }

        public CArrayType(CType elementType, Expression lengthExpression)
        {
            ElementType = elementType;
            LengthExpression = lengthExpression;
        }

        public override int GetSize(EmitContext c)
        {
            var innerSize = ElementType.GetSize(c);
            if (LengthExpression == null)
            {
                c.Report.Error(2133, Location, "unknown size");
                return 0;
            }
            var lexp = LengthExpression;
            if (lexp is ConstantExpression)
            {
                var cexp = (ConstantExpression)lexp;

                if (cexp.ConstantType.IsIntegral)
                {
                    var length = Convert.ToInt32(cexp.Value);
                    return length * innerSize;
                }
                else
                {
                    c.Report.Error(2058, LengthExpression.Location, "constant expression is not integral");
                    return 0;
                }
            }
            else
            {
                c.Report.Error(2057, LengthExpression.Location, "expected constant expression");
                return 0;
            }
        }

        public override string ToString()
        {
            return string.Format("(Array {0} {1})", ElementType, LengthExpression);
        }
    }


}
