namespace alm.Other.InnerTypes
{

    public interface IType
    {
        string Representation { get; set; }

    }

    public abstract class InnerType
    {
        public abstract string Representation { get; }

        public static InnerType GetFromString(string StringType)
        {
            switch (StringType.ToLower())
            {
                case "integer": return new Integer32();
                case "boolean": return new Boolean();
                case "string" : return new String();
                case "float"  : return new Float();
                default: return new Underfined();
            }
        }
        public System.Type GetEquivalence()
        {
            switch(this.Representation.ToLower())
            {
                case "string" : return typeof(string);
                case "integer": return typeof(int);
                case "boolean": return typeof(bool);
                case "float"  : return typeof(float);
                default : return null;
            }
        }
        public override string ToString() => this.Representation;

        public static bool operator !=(InnerType fType, InnerType sType)
        {
            if (fType is null || sType is null) return false;
            if (fType.Representation != sType.Representation) return true;
            return false;
        }
        public static bool operator ==(InnerType fType, InnerType sType)
        {
            if (fType is null || sType is null) return false;
            if (fType.Representation == sType.Representation) return true;
            return false;
        }
    }

    public abstract class NumericType : InnerType
    {
        public abstract int CastPriority { get; }
        public bool CanCast(NumericType toThisType)
        {
            if (this.CastPriority <= toThisType.CastPriority)
                return true;
            return false;
        }
        public override string Representation => "numeric";
    }

    public sealed class Integer32 : NumericType
    {
        public override int CastPriority => 1;
        public override string Representation => "integer";
    }
    public sealed class Float : NumericType
    {
        public override int CastPriority => 2;
        public override string Representation => "float";
    }
    public sealed class String : InnerType
    {
        public override string Representation => "string";
    }
    public sealed class Boolean : InnerType
    {
        public override string Representation => "boolean";
    }
    public sealed class Void : InnerType
    {
        public override string Representation => "void";
    }
    public sealed class Underfined : InnerType
    {
        public override string Representation => "underfined";
    }
}
