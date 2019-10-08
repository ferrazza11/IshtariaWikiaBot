using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Tools.AsyncHttpRequest;
namespace IshtariaWikiaBot
{
    public class Api
    {
        protected static class Urls
        {
            public static string Base { get { return "https://ishtaria.fandom.com/api.php?"; } }
            public static string Format { get { return "&format=xml"; } }
            public static string Login(string user, string password, string token) { return Base + "action=login&lgname=" + user + "&lgpassword=" + password + "&lgtoken=" + token + Format; }
            public static string Categories { get { return Base + "action=query&list=categorymembers&cmtitle=Category:Units&cmnamespace=0&cmlimit=500" + Format; } }
            public static string Units(string pagesId) { return Base + "action=query&prop=revisions|info&pageids=" + pagesId + "&rvprop=content" + Format; }
            public static string Image(string imgname) { return Base + "action=query&titles=FILE:" + imgname + ".png" + Format; }
            public static string EditToken(string page) { return Base + "action=query&prop=info&titles=" + page + "&intoken=edit" + Format; }
            public static string Logout { get { return Base + "action=logout" + Format; } }
        }
        #region ATTRIBUTES
        User _user;
        AsyncWebRequest _async;
        int _lastRequest;
        List<CommunicationLogs> _logs;
        static string _currentdir = "c:\\Bot";
        static string _logspath = _currentdir + "\\logs.log";
        #endregion
        #region DELEGATES
        public delegate void LoginEvent(object sender, Login data);
        public delegate void UpdateLogEvent(object sender, CommunicationLogs log);
        public delegate void CategoriesEvent(object sender, List<Category> data, bool finished);
        public delegate void UnitsEvent(object sender, List<Unit> data);
        public delegate void ImageEvent(object sender, string ImageName, bool finished);
        public delegate void EditTokenEvent(object sender, string token);
        public delegate void EditEvent(object sender, bool succeed);
        public delegate void LogoutEvent(object sender);
        #endregion
        #region EVENTS
        public event LoginEvent OnLoggedIn;
        public event LogoutEvent OnLoggedOut;
        public event UpdateLogEvent OnUpdateLog;
        public event CategoriesEvent OnCategories;
        public event UnitsEvent OnUnits;
        public event ImageEvent OnImage;
        public event EditTokenEvent OnEditToken;
        public event EditEvent OnEdit;
        #endregion
        #region COMMON
        public Api()
        {
            _lastRequest = 0;
            _async = new AsyncWebRequest(10);
            _async.OnResponseReceived += OnResponseReceived;
            _logs = new List<CommunicationLogs>();            
        }
        void OnResponseReceived(object sender, ResponseObjectEventArgs e)
        {
            string DOC = e.Response.Document;
            int ID = e.Response.RequestId;
            CookieCollection cc = e.Response.WebObject.Cookies;
            UpdateLog(ID, CommunicationLogs.CommunicationState.RECEIVED);
            switch (Parser.IdentifyPage(DOC))
            {
                case Parser.PAGE.LOGIN:
                    ProcessLogin(ID, DOC);
                    break;
                case Parser.PAGE.CATEGORIES:
                    ProcessCategories(ID, DOC);
                    break;                
                case Parser.PAGE.UNITS:
                    ProcessUnits(ID, DOC);
                    break;
                case Parser.PAGE.IMAGES:
                    ProcessImages(ID, DOC);
                    break;
                case Parser.PAGE.EDITTOKEN:
                    ProcessEditToken(ID, DOC);
                    break;
                case Parser.PAGE.EDIT:
                    ProcessEdit(ID, DOC);
                    break;
                case Parser.PAGE.LOGOUT:
                    OnLoggedOut(this);
                    break;
                default:
                    break;
            }
        }
        #endregion
        #region LOGIN/OUT
        public void Login(string userName, string password)
        {            
            _user = _user == null ? new User() { user = userName, password = password } : _user;
            Login();
        }
        private void Login()
        {
            _async.AddPost(++_lastRequest, Urls.Login(_user.user, _user.password, _user.token));
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "LOGIN", State = CommunicationLogs.CommunicationState.GET, Info = "User     = " + _user.user });
        }
        void ProcessLogin(int requestId, string doc)
        {
            Login l = Parser.ParseLogin(doc);
            _user.token = l.token;
            switch (l.result)
            {
                case IshtariaWikiaBot.Login.LoginResult.NeedToken:
                case IshtariaWikiaBot.Login.LoginResult.WrongToken:
                    Login();
                    break;
                case IshtariaWikiaBot.Login.LoginResult.Succeed:
                    OnLoggedIn(this, l);
                    break;
                default:
                    break;
            }
        }
        public void Logout()
        {
            _async.AddGet(++_lastRequest, Urls.Logout);
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "LOGOUT", State = CommunicationLogs.CommunicationState.GET });            
        }
        void ProcessLogout(int requestId, string doic)
        {
            OnLoggedOut(this);
        }
        #endregion
        #region CATEGORIES
        public void Categories()
        {
            _async.AddGet(++_lastRequest, Urls.Categories);
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "CATEGORY", State = CommunicationLogs.CommunicationState.GET });
        }        
        void ProcessCategories(int requestId, string doc)
        {
            string nextPetition = string.Empty;
            OnCategories(this, Parser.ParseCategories(doc, ref nextPetition), nextPetition == string.Empty);
            if (!(nextPetition == string.Empty))
            {
                _async.AddGet(++_lastRequest, Urls.Categories + "&cmcontinue=" + nextPetition);
                AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "CATEGORY", State = CommunicationLogs.CommunicationState.GET });
            }
        }       
        #endregion
        #region UNITS
        public void Units(List<Category> u)
        {
            int j = 1, k = 50;
            string s = string.Empty;
            for (int i = 0; i < u.Count; i++)
            {
                s += u[i].pageid;
                if (j == k | i == u.Count - 1)
                {
                    _async.AddGet(++_lastRequest, Urls.Units(s));
                    AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "MOD. UNITS", State = CommunicationLogs.CommunicationState.GET, Info = "From " + u[i - (j - 1)].title + " to " + u[i].title });
                    j = 1;
                    s = string.Empty;
                }
                else
                {
                    j++;
                    s += "|";
                }
            }
        }
        void ProcessUnits(int requestId, string doc)
        {
            OnUnits(this, Parser.ParseUnits(doc));
        }
        #endregion
        #region IMAGES
        public void Image(string ImageName)
        {
            _async.AddGet(++_lastRequest, Urls.Image(ImageName.Replace("&", "%26")));
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "IMAGE", State = CommunicationLogs.CommunicationState.GET, Info = "Name = " + ImageName + ".png" });
                    
        }
        void ProcessImages(int requestId, string doc)
        {
            System.Console.Write("{0, 22:D4} -> ", (_lastRequest - requestId).ToString());
            OnImage(this, Parser.ParseImage(doc), requestId == _lastRequest);
        }
        #endregion
        #region TABLE
        public void EditToken()
        {
            _async.AddGet(++_lastRequest, Urls.EditToken("Unit List"));
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "EDIT TOKEN", State = CommunicationLogs.CommunicationState.GET, Info = "Page = " + "Unit List" });
        }
        void ProcessEditToken(int requestId, string doc)
        {
            OnEditToken(this, Parser.ParseEditToken(doc));
        }
        public void EditPage(List<Unit> Units, string token)
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("action","edit");
            dic.Add("title", "Unit List");
            dic.Add("text", Table.GetTable(Units.OrderBy(l => l.name).ToList()));
            dic.Add("token", token);
            dic.Add("format", "xml");
            _async.AddPostMultyPartForm(++_lastRequest, Urls.Base, dic);
            AddLog(new CommunicationLogs() { Id = _lastRequest, Request = "EDIT PAGE", State = CommunicationLogs.CommunicationState.GET, Info = "PLEASE WAIT" });
        }
        void ProcessEdit(int requestId, string doc)
        {
            OnEdit(this, Parser.ParseEdit(doc)=="Success"); 
        }
        #endregion
        #region LOGS
        public void ClearLogs(bool savelogs = false)
        {
            if (savelogs)
                SaveLogs();
            _lastRequest = 0;
            _logs.Clear();
        }
        void SaveLogs()
        {
            using (StreamWriter w = File.AppendText(_logspath))            
                foreach (CommunicationLogs l in _logs)                
                   w.WriteLine(FormatMessage(l.Request, l.State.ToString(), l.Info));
        }
        public void WriteLog(string text)
        {
            using (StreamWriter w = File.AppendText(_logspath))
                w.WriteLine(System.DateTime.Now.ToString("yyyyMMddhhmmss") + "--->" + text);
        }
        CommunicationLogs.CommunicationState GetState(int requestId)
        {
            return _logs[requestId - 1].State;
        }
        void UpdateLog(int requestId, CommunicationLogs.CommunicationState state, string info)
        {
            _logs[requestId - 1].Info = info;
            UpdateLog(requestId, state);
        }
        void UpdateLog(int requestId, CommunicationLogs.CommunicationState state)
        {
            _logs[requestId - 1].State = state;
            OnUpdateLog(this, _logs[requestId - 1]);
        }
        void AddLog(CommunicationLogs log)
        {
            _logs.Add(log);
            OnUpdateLog(this, _logs[_logs.Count - 1]);
        }
        string FormatMessage(string request, string state, string info)
        {
            return System.DateTime.Now.ToString("YYYYMMddhhmmss") + "--->" + request.PadRight(10) + "--->  " + state.PadRight(8) + " ---> " + info;
        }
        #endregion
    }
    public class User
    {
        public string user { get; set; }
        public string password { get; set; }
        public string token { get; set; }
    }
}