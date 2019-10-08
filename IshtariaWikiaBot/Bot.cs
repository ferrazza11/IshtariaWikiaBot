using System.Collections.Generic;

namespace IshtariaWikiaBot
{
    public class Bot
    {
        Api a;
        List<Category> _c;
        List<Unit> _u;
        List<Unit> _nu;
        static string _currentdir = System.AppDomain.CurrentDomain.BaseDirectory;
        static string _unitspath = _currentdir + "\\units.xml";        
        public Bot()
        {
            _u = System.IO.File.Exists(_unitspath) ? XML<List<Unit>>.Read(_unitspath) : new List<Unit>();                      
        }

        void OnLoggedOut(object sender)
        {
            a.ClearLogs();
            a.OnUpdateLog -= OnUpdateLog;
            a.OnLoggedIn -= OnLoggedIn;
            a.OnCategories -= OnCategories;
            a.OnUnits -= OnUnits;
            a.OnImage -= OnImage;
            a.OnEditToken -= OnEditToken;
            a.OnEdit -= OnEdit;
            a.OnLoggedOut -= OnLoggedOut;
        }
        public void Start(string user, string password)
        {
            _nu = new List<Unit>();
            _c = new List<Category>();
            a = new Api();
            a.OnUpdateLog += OnUpdateLog;
            a.OnLoggedIn += OnLoggedIn;
            a.OnCategories += OnCategories;
            a.OnUnits += OnUnits;
            a.OnImage += OnImage;
            a.OnEditToken += OnEditToken;
            a.OnEdit += OnEdit;
            a.OnLoggedOut += OnLoggedOut;
            a.Login(user, password);
        }
        void OnEdit(object sender, bool succeed)
        {
            a.Logout();            
        }
        void OnEditToken(object sender, string token)
        {
            a.EditPage(_u, token);
        }
        void OnImage(object sender, string ImageName, bool finished)
        {
            try
            {
                Unit c = _u.Find(x => x.name == ImageName.Replace(".png", ""));
                if (c != null) c.hasimage = true;
                if (finished)
                {
                    XML<List<Unit>>.Write(_u, _unitspath);
                    a.EditToken();
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("EXCEPTION -> {0}", ex.Message);
            }
        }       
        void OnUnits(object sender, List<Unit> data)
        {
            _nu.AddRange(data);
            if (_nu.Count == _c.Count)
                CheckUnits();
        }      
        void OnCategories(object sender, List<Category> data, bool finished)
        {
            _c.AddRange(data);
            if (finished) a.Units(_c);
        }    
        void OnLoggedIn(object sender, Login data)
        {
            a.Categories();
        }
        void OnUpdateLog(object sender, CommunicationLogs log)
        {
            try
            {
                System.Console.WriteLine(string.Format("{0} {1} {2}", log.Request, log.State.ToString(), log.Info));
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("EXCEPTION -> {0}", ex.Message);
            }
        }
        void CheckUnits()
        {
            bool mustUpdate=false;
            foreach (Unit n in _nu)
            {
                if (_u.Exists(x => x.id == n.id))
                {
                   mustUpdate |= CheckAndUpdateUnit(n);                   
                }
                else //New Unit 
                {
                    mustUpdate = true;
                    _u.Add(n);
                }
            }
            if (mustUpdate)
            {
                XML<List<Unit>>.Write(_u, _unitspath);
                Update();
            }
            else 
            {
                System.Console.WriteLine("No units modified");
            }         
        }
        bool CheckAndUpdateUnit(Unit lastRevision)
        {
            Unit u = _u.Find(x => x.id == lastRevision.id);
            if (u.revision != lastRevision.revision)
            {
                _u.Remove(u);
                _u.Add(lastRevision);
                return true;
            }
            return false;
        }
        void Update()
        {
            foreach (Unit u in _u)
            {
                if (!u.hasimage && !u.unreleased)
                {
                    a.Image(u.name);
                }
            }
        }               
    }
}
