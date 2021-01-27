using System;
using System.Collections.Generic;

using static alm.Other.String.StringMethods;

namespace alm.Core.InnerTypes
{
    public abstract class InnerType
    {
        private static readonly InnerType[] ALMTypes = new InnerType[]
        {
            new Char(),
            new Void(),
            new Int8(),
            new Int16(),
            new Int32(),
            new Single(),
            new String(),
            new Boolean(),
        };

        public virtual string ALMRepresentation => string.Empty;
        public virtual string NETRepresentation => string.Empty;
        public virtual bool PossibleAsVariableType { get; protected set; } = true;

        public Type GetEquivalence() => Type.GetType(this.NETRepresentation);

        public virtual ArrayType CreateArrayInstance(int dimension = 1) => null;

        public static InnerType Parse(string typeString)
        {
            //TODO generic
            if (typeString == "AnyArray")
                return new AnyArray();

            for (int i = 0; i < ALMTypes.Length; i++)
                if (ALMTypes[i].ALMRepresentation == typeString)
                    return ALMTypes[i];

            //think is case of array type: 
            string singleTypeString = string.Empty;
            for (int i = 0; i < typeString.Length; i++)
            {
                if (typeString[i] == '[')
                    break;
                singleTypeString += typeString[i];
            }

            if (singleTypeString.Length == typeString.Length)
                return null;

            //parse for array dimension
            int dimensions = 1;
            for (int i = singleTypeString.Length + 1; typeString[i] != ']' && i < typeString.Length - 1; i++)
                if (typeString[i] == ',')
                    dimensions++;
                else
                    return null;

            for (int i = 0; i < ALMTypes.Length; i++)
                if (ALMTypes[i].ALMRepresentation == singleTypeString && ALMTypes[i].PossibleAsVariableType)
                    return ALMTypes[i].CreateArrayInstance(dimensions);

            return null;
        }

        public override int GetHashCode()
        {
            int hashCode = -1090945007;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ALMRepresentation);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(NETRepresentation);
            hashCode = hashCode * -1521134295 + PossibleAsVariableType.GetHashCode();
            return hashCode;
        }
        public override bool Equals(object obj)
        {
            return obj is InnerType type &&
                   ALMRepresentation == type.ALMRepresentation &&
                   NETRepresentation == type.NETRepresentation &&
                   PossibleAsVariableType == type.PossibleAsVariableType;
        }

        public static bool operator ==(InnerType fType, InnerType sType)
        {
            if (fType is ArrayType && sType is AnyArray ||
                fType is AnyArray && sType is ArrayType)
                return true;

            if (fType is null || sType is null)
                return false;
            if (fType.ALMRepresentation == sType.ALMRepresentation)
                return true;
            return false;
        }
        public static bool operator !=(InnerType fType, InnerType sType)
        {
            if (fType == sType)
                return false;
            return true;
        }

        public override string ToString() => this.ALMRepresentation;
    }

    public abstract class PrimitiveType : InnerType
    {
        public virtual bool PossibleAsArrayType { get; protected set; } = true;
    }

    public abstract class NumericType : PrimitiveType
    {
        public abstract int CastPriority { get; }
        public override string ALMRepresentation => "numeric";
        public override bool PossibleAsArrayType => true;

        public virtual InnerType[] CanCastTo => new InnerType[] { };

        public bool CanCast(NumericType toThisType)
        {
            if (this.CastPriority <= toThisType.CastPriority)
                return true;
            return false;
        }
        public bool _CanCast(NumericType toThisType)
        {
            for (int i = 0; i < CanCastTo.Length; i++)
                if (CanCastTo[i] == toThisType)
                    return true;
            return false;
        }
    }

    public sealed class Void : PrimitiveType
    {
        public override string ALMRepresentation => "void";
        public override string NETRepresentation => "System.Void";

        public override bool PossibleAsVariableType => false;
        public override bool PossibleAsArrayType => false;
    }

    public sealed class Int32 : NumericType
    {
        public override int CastPriority => 3;
        public override string ALMRepresentation => "integer";
        public override string NETRepresentation => "System.Int32";

        public override ArrayType CreateArrayInstance(int dimension = 1) => new Int32Array(dimension);
    }
    public sealed class Int16 : NumericType
    {
        public override int CastPriority => 2;
        public override string ALMRepresentation => "short";
        public override string NETRepresentation => "System.Int16";
    }
    public sealed class Int8 : NumericType
    {
        public override int CastPriority => 1;
        public override string ALMRepresentation => "byte";
        public override string NETRepresentation => "System.SByte";
    }
    public sealed class Single : NumericType
    {
        public override int CastPriority => 4;
        public override string ALMRepresentation => "float";
        public override string NETRepresentation => "System.Single";

        public override ArrayType CreateArrayInstance(int dimension = 1) => new SingleArray(dimension);
    }
    public sealed class Char : NumericType
    {
        public override int CastPriority => 1;

        public override string ALMRepresentation => "char";
        public override string NETRepresentation => "System.Char";

        public override ArrayType CreateArrayInstance(int dimension = 1) => new CharArray(dimension);
    }
    public sealed class Boolean : PrimitiveType
    {
        public override string ALMRepresentation => "boolean";
        public override string NETRepresentation => "System.Boolean";

        public override ArrayType CreateArrayInstance(int dimension = 1) => new BooleanArray(dimension);
    }

    public abstract class ArrayType : InnerType
    {
        public int Dimension { get; private set; }
        public ArrayType(int dimensions = 1) => Dimension = dimensions;
        
        protected virtual string GetLocalTypeString(string atomLocalType)
        {
            string type = atomLocalType + '[';
            for (int i = 0; i < Dimension - 1; i++)
                type += ',';
            type += ']';
            return type;
        }
        protected virtual string GetSystemTypeString(string atomSystemType)
        {
            string type = "System." + UpperCaseFirstChar(atomSystemType) + '[';
            for (int i = 0; i < Dimension - 1; i++)
                type += ',';
            type += ']';
            return type;
        }

        public virtual InnerType GetAtomElementType() => this.GetDimensionElementType(Dimension);
        public virtual InnerType GetDimensionElementType(int dimension)
        {
            if (Dimension < dimension || dimension < 0)
                return null;

            int newDimensions = Dimension - dimension;
            string atomLocalType = string.Empty;
            for (int i = 0;i < ALMRepresentation.Length; i++)
            {
                if (ALMRepresentation[i] == '[')
                    break;
                atomLocalType += ALMRepresentation[i];
            }

            if (newDimensions == 0)
                return Parse(atomLocalType);

            string typeString = atomLocalType + '[';
            for (int i = 0; i < newDimensions - 1; i++)
                typeString += ',';
            typeString += ']';
            return Parse(typeString);
        }
    }

    //change on generic
    public sealed class AnyArray : ArrayType
    {
        public override string ALMRepresentation => "AnyArray";
    }

    public sealed class Int32Array : ArrayType
    {
        public Int32Array(int dimensions) : base(dimensions) { }

        public override string ALMRepresentation => this.GetLocalTypeString("integer");
        public override string NETRepresentation => this.GetSystemTypeString("Int32");
    }

    public sealed class BooleanArray : ArrayType
    {
        public BooleanArray(int dimensions) : base(dimensions) { }

        public override string ALMRepresentation => this.GetLocalTypeString("boolean");
        public override string NETRepresentation => this.GetSystemTypeString("Boolean");
    }

    public sealed class StringArray : ArrayType
    {
        public StringArray(int dimensions) : base(dimensions) { }

        public override string ALMRepresentation => this.GetLocalTypeString("string");
        public override string NETRepresentation => this.GetSystemTypeString("String");
        public override InnerType GetAtomElementType() => new String();
        public override InnerType GetDimensionElementType(int dimensions)
        {
            if (Dimension - dimensions == -1)
                return new Char();
            else
                return base.GetDimensionElementType(dimensions);
        }
    }

    public sealed class CharArray : ArrayType
    {
        public CharArray(int dimensions) : base(dimensions) { }

        public override string ALMRepresentation => this.GetLocalTypeString("char");
        public override string NETRepresentation => this.GetSystemTypeString("Char");
    }

    public sealed class SingleArray : ArrayType
    {
        public SingleArray(int dimensions) : base(dimensions) { }

        public override string ALMRepresentation => this.GetLocalTypeString("float");
        public override string NETRepresentation => this.GetSystemTypeString("Single");
    }

    public sealed class String : ArrayType
    {
        public override string ALMRepresentation => "string";
        public override string NETRepresentation => "System.String";
        public override ArrayType CreateArrayInstance(int dimension = 1) => new StringArray(dimension);
        public override InnerType GetAtomElementType() => new Char();
        public override InnerType GetDimensionElementType(int dimension)
        {
            if (dimension == 1)
                return new Char();
            else
                return null;
        }
    }

    public sealed class Underfined : InnerType
    {
        public override string ALMRepresentation => "underfined";
        public override bool PossibleAsVariableType => false;
    }
}
