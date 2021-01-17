using System;

namespace alm.Other.InnerTypes
{
    public abstract class InnerType
    {
        public abstract string Representation { get; }
        public virtual bool PossibleAsVariableType { get; protected set; } = true;

        public Type GetEquivalence()
        {
            switch (this.Representation.ToLower())
            {
                //переписать
                case "string" : return typeof(string);
                case "boolean": return typeof(bool);
                case "float"  : return typeof(float);
                case "void"   : return typeof(void);
                case "char"   : return typeof(char);
                case "byte"   : return typeof(sbyte);
                case "short"  : return typeof(short);
                case "integer": return typeof(int);

                case "integer[]": return typeof(int[]);
                case "string[]" : return typeof(string[]);
                case "boolean[]": return typeof(bool[]);
                case "float[]"  : return typeof(float[]);
                case "char[]"   : return typeof(char[]);
                default:
                    throw new System.Exception("");
            }
        }
        public ArrayType GetArrayType()
        {
            string typeString = this.Representation+"[]";
            return (ArrayType)GetFromString(typeString);
        }

        public static InnerType GetFromString(string StringType)
        {
            switch (StringType.ToLower())
            {
                //переписать
                case "boolean": return new Boolean();
                case "string" : return new String();
                case "float"  : return new Real32();
                case "void"   : return new Void();
                case "byte"   : return new Int8();
                case "short"  : return new Int16();
                case "integer": return new Int32();
                case "char"   : return new Char();

                case "integer[]": return new Int32Array();
                case "string[]" : return new StringArray();
                case "boolean[]": return new BooleanArray();
                case "float[]"  : return new Real32Array();
                case "char[]"   : return new CharArray();

                case "AnyArray": return new AnyArray();
                default: 
                    throw new System.Exception("");
            }
        }

        public override string ToString() => this.Representation;

        public static bool operator !=(InnerType fType, InnerType sType)
        {
            if (fType is ArrayType && sType is AnyArray ||
                fType is AnyArray && sType is ArrayType)
                return false;

            if (fType is null || sType is null)
                return false;
            if (fType.Representation != sType.Representation)
                return true;
            return false;
        }
        public static bool operator ==(InnerType fType, InnerType sType)
        {
            if (fType is ArrayType && sType is AnyArray ||
                fType is AnyArray  && sType is ArrayType)
                return true;

            if (fType is null || sType is null) 
                return false;
            if (fType.Representation == sType.Representation) 
                return true;
            return false;
        }
    }

    public abstract class SingleType : InnerType
    {

    }

    public abstract class NumericType : SingleType
    {
        public abstract int CastPriority { get; }

        public InnerType[] CanCastTo;

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

        public override string Representation => "numeric";
    }

    public sealed class Int32 : NumericType
    {
        public override int CastPriority => 3;
        public override string Representation => "integer";
    }
    public sealed class Int16 : NumericType
    {
        public override int CastPriority => 2;
        public override string Representation => "short";
    }
    public sealed class Int8 : NumericType
    {
        public override int CastPriority => 1;
        public override string Representation => "byte";
    }
    public sealed class Real32 : NumericType
    {
        public override int CastPriority => 4;
        public override string Representation => "float";
    }
    public sealed class Char : NumericType
    {
        public override int CastPriority => 1;

        public override string Representation => "char";
    }
    public sealed class Boolean : SingleType
    {
        public override string Representation => "boolean";
    }

    public abstract class ArrayType : InnerType
    {
        public int Dimensions { get; private set; }

        //На время
        public virtual InnerType GetElementType() { return new Underfined(); } 
    }

    public sealed class AnyArray : ArrayType
    {
        public override string Representation => "AnyArray";
    }

    public sealed class Int32Array : ArrayType
    {
        public override string Representation => "integer[]";
        public override InnerType GetElementType() => new Int32();
    }

    public sealed class BooleanArray : ArrayType
    {
        public override string Representation => "boolean[]";
        public override InnerType GetElementType() => new Boolean();
    }

    public sealed class StringArray : ArrayType
    {
        public override string Representation => "string[]";
        public override InnerType GetElementType() => new String();
    }

    public sealed class CharArray : ArrayType
    {
        public override string Representation => "char[]";
        public override InnerType GetElementType() => new Char();
    }

    public sealed class Real32Array : ArrayType
    {
        public override string Representation => "float[]";
        public override InnerType GetElementType() => new Real32();
    }

    public sealed class String : ArrayType
    {
        public override string Representation => "string";
        public override InnerType GetElementType() => new Char();
    }

    public sealed class Void : InnerType
    {
        public override string Representation => "void";
        public override bool PossibleAsVariableType => false;
    }
    public sealed class Underfined : InnerType
    {
        public override string Representation => "underfined";
    }
}
