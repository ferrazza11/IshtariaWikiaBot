namespace IshtariaWikiaBot
{
    /*
     * {"login":{"result":"NeedToken","token":"1520cc90638c2d3c5c913c51f6e5a27d","cookieprefix":"wikicities"}}
     * {"login":{"result":"Success","lguserid":30809861,"lgusername":"****","lgtoken":"258efbf1465f8d7e7ad64fc7e6132081","cookieprefix":"wikicities","sessionid":"5f220d98dbdeba9d745d68ee1c37ad8f"}}
     */

    public class Login
    {
        public enum LoginResult { NeedToken, WrongToken, Failed, Aborted, Succeed }
        public LoginResult result { get; internal set;}
        public string lguserid { get; internal set; }
        public string lgusername { get; internal set; }
        public string token { get; internal set; }
        public string lgtoken { get; internal set; }
        public string cookieprefix { get; internal set; }
        public string sessionid { get; internal set; }
        public static LoginResult LoginResultFromString(string str)
        {
            switch (str.ToLower())
            {
                case "needtoken": return LoginResult.NeedToken;
                case "wrongtoken": return LoginResult.WrongToken;
                case "failed": return LoginResult.Failed;
                case "aborted": return LoginResult.Aborted;
                default: return LoginResult.Succeed;
            }
        }
               
    }
}
