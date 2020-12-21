namespace alm.Other.InnerTypes
{

    public interface IType
    {
        string Representation { get; set; }

    }

    public abstract class InnerType
    {
        public virtual int Bytes => 0;
        public abstract string Representation { get; }
        public static InnerType GetFromString(string StringType)
        {
            switch (StringType.ToLower())
            {
                case "integer": return new Integer32();
                case "boolean": return new Boolean();
                case "string" : return new String();
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
                default : return null;
            }
        }
        public override string ToString() => this.Representation;

        public override bool Equals(object obj)
        {
            return obj is InnerType type &&
                   Bytes == type.Bytes &&
                   Representation == type.Representation;
        }

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

    public sealed class Integer32 : InnerType
    {
        public override int Bytes => 4;
        public override string Representation => "integer";
        public const int MaxValue = System.Int32.MaxValue;
        public const int MinValue = System.Int32.MinValue;
    }
    public sealed class String : InnerType
    {
        private int _len;
        public override int Bytes => _len;
        public override string Representation => "string";
    }
    public sealed class Boolean : InnerType
    {
        public override int Bytes => 4;
        public override string Representation => "boolean";
    }
    public sealed class Void : InnerType
    {
        public override int Bytes => 0;
        public override string Representation => "void";
    }
    public sealed class Underfined : InnerType
    {
        public override int Bytes => 0;
        public override string Representation => "underfined";
    }
}
