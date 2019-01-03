using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StatNotifier
{
    public class regMatch
    {
        public enum RESULTS{
            UNMATCH,
            FOUND,
            MATCH
        }
        public String exps { get; set; }
        public int accumlate { get; set; }
        public RESULTS result { get; private set; }
        public int pos { get; private set; }
        public int len { get; private set; }
        int count;
        public regMatch( String exps, int accumlate)
        {
            this.exps = exps;
            this.accumlate = accumlate;
            if (accumlate <= 0) accumlate = 1;
            count = 0;
        }
        public bool checkMatch(String text) //不一致:-1, 一致有り:0 規定回数一致:1
        {
            try
            {
                if (text == null)
                {
                    count = 0;
                    result = RESULTS.UNMATCH;
                }
                else
                {

                    //if (System.Text.RegularExpressions.Regex.IsMatch(text, exps))
                    System.Text.RegularExpressions.Match m = Regex.Match(text, exps);
                    if (m.Success)
                    {
                        pos = m.Index;
                        len = m.Length;
                        if (++count >= accumlate)
                        {
                            count = 0;
                            result = RESULTS.MATCH;
                        }
                        else
                        {
                            result = RESULTS.FOUND;
                        }
                    }
                    else
                    {
                        count = 0;
                        result = RESULTS.UNMATCH;
                    }
                }
            }
            catch (Exception) {
                count = 0;
                result = RESULTS.UNMATCH;
            }
            return result!=RESULTS.UNMATCH;
        }
        
    }
}
