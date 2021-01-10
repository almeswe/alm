namespace alm.Other.String
{
    public static class StringMethods
    {
        public static string Reverse(string String)
        {
            string reversedstr = string.Empty;
            for (int i = String.Length - 1; i >= 0; i--)
                reversedstr += String[i];
            return reversedstr;
        }
        public static string UpperCaseFirstChar(string String)
        {
            char fchar = String[0];
            return fchar.ToString().ToUpper() + String.Substring(1,String.Length-1);
        }
        public static string LastAfterDot(string String)
        {
            string TypeWithoutNameSpace = string.Empty;
            for (int i = String.Length - 1; i >= 0; i--)
                if (String[i] != '.')
                    TypeWithoutNameSpace += String[i];
                else break;
            return Reverse(TypeWithoutNameSpace);
        }
        public static string[] SplitStringByFours(string String)
        {
            string string4 = string.Empty;
            System.Collections.Generic.List<string> strings4 = new System.Collections.Generic.List<string>();

            for (int i = 0; i < String.Length; i++)
            {
                if (string4.Length == 4)
                {
                    strings4.Add(string4);
                    string4 = string.Empty;
                    string4 += String[i];
                }
                else string4 += String[i];
                if (i == String.Length - 1)
                    if (string4.Length <= 4)
                        strings4.Add(string4);
            }
            return strings4.ToArray();
        }
        public static string CharNTimes(int n,char ch)
        {
            string str = string.Empty;
            for (int i = 1;i <= n;i++)
                str += ch.ToString();
            return str;
        }
        public static string[] SplitSubstrings(string String)
        {
            string sub = string.Empty;
            bool marks = false;

            System.Collections.Generic.List<string> subs = new System.Collections.Generic.List<string>();

            for (int i = 0; i < String.Length; i++)
            {
                if (String[i] == '"') marks = marks ? false : true;
                if(!marks)
                    if (String[i] == ' ' || String[i] == '\t')
                    {
                        if (sub.Trim() == string.Empty) continue;
                        subs.Add(sub);
                        sub = string.Empty;
                        continue;
                    }
                sub += String[i].ToString();
            }
            if (sub.Trim() != "") subs.Add(sub.Trim());
            return subs.ToArray();
        }
        public static string SubstractChar(string String,char ch)
        {
            string str = string.Empty;
            for (int i = 0; i < String.Length; i++)
                if (String[i] != ch)
                    str += String[i].ToString();
            return str;
        }
        public static string DeleteFirstSameChar(string str, char ch)
        {
            bool meet = false;
            string newstr = string.Empty;
            
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != ch)
                { 
                    meet = true;
                    newstr += str[i];
                }
                else
                    if (meet)
                        newstr += str[i];
            }

            return newstr;
        }

        public static string DeleteFirstSameChars(string str, char ch, char ch2)
        {
            bool meet = false;
            string newstr = string.Empty;

            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] != ch && str[i] != ch2)
                {
                    meet = true;
                    newstr += str[i];
                }
                else
                    if (meet)
                        newstr += str[i];
            }

            return newstr;
        }

        public static int CountFirstChars(string str, char ch)
        {
            int counter = 0;
            for (int i = 0; i < str.Length; i++)
                if (str[i] == ch)
                    counter++;
                else
                    break;
            return counter;
        }
        public static int Tabulations(string String)
        {
            int t = 0;
            for (int i = 0; i < String.Length; i++)
                if (String[i] == '\t')
                    t++;
            return t;
        }
    }
}
