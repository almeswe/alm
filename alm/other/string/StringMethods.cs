namespace alm.Other.String
{
    public static class StringMethods
    {
        public static string Reverse(string str)
        {
            string reversedstr = string.Empty;
            for (int i = str.Length - 1; i >= 0; i--)
                reversedstr += str[i];
            return reversedstr;
        }
        public static string UpperCaseFirstChar(string str)
        {
            char fchar = str[0];
            return fchar.ToString().ToUpper() + str.Substring(1,str.Length-1);
        }
        public static string LastAfterDot(string str)
        {
            string TypeWithoutNameSpace = string.Empty;
            for (int i = str.Length - 1; i >= 0; i--)
                if (str[i] != '.')
                    TypeWithoutNameSpace += str[i];
                else break;
            return Reverse(TypeWithoutNameSpace);
        }
        public static string CharNTimes(int n,char ch)
        {
            string str = string.Empty;
            for (int i = 1;i <= n;i++)
                str += ch.ToString();
            return str;
        }

        public static string SubstractChar(string str, char ch)
        {
            string newstr = string.Empty;
            for (int i = 0; i < str.Length; i++)
                if (str[i] != ch)
                    newstr += str[i].ToString();
            return newstr;
        }
        public static int CountFirstSameChar(string str, char ch)
        {
            int counter = 0;
            for (int i = 0; i < str.Length; i++)
                if (str[i] == ch)
                    counter++;
                else
                    break;
            return counter;
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

        public static string[] Split(string str,char ch,char breakCh = '"')
        {
            string sub = string.Empty;

            bool breaked = false;

            System.Collections.Generic.List<string> subs = new System.Collections.Generic.List<string>();

            for (int i = 0; i < str.Length; i++)
            {
                if (i == str.Length-1)
                {
                    if (str[i] != ch)
                        sub += str[i];
                    if (sub.Trim() != string.Empty)
                        subs.Add(sub);
                }
                if (breakCh == str[i])
                    breaked = breaked ? false : true;
                if (str[i] == ch && !breaked)
                {
                    if (sub.Trim() != string.Empty)
                        subs.Add(sub);
                    sub = string.Empty;
                }
                else
                    sub += str[i];
            }
            return subs.ToArray();
        }
        public static string[] SplitStringByFours(string str)
        {
            string string4 = string.Empty;
            System.Collections.Generic.List<string> strings4 = new System.Collections.Generic.List<string>();

            for (int i = 0; i < str.Length; i++)
            {
                if (string4.Length == 4)
                {
                    strings4.Add(string4);
                    string4 = string.Empty;
                    string4 += str[i];
                }
                else string4 += str[i];
                if (i == str.Length - 1)
                    if (string4.Length <= 4)
                        strings4.Add(string4);
            }
            return strings4.ToArray();
        }
    }
}
