using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;

namespace IshtariaWikiaBot
{
    
    public static class Parser
    {
        
        public enum PAGE { ERROR, LOGIN, LOGOUT, QUERY, ACTION, LIST, PROP, META, CATEGORIES, UNITS, IMAGES, EDITTOKEN, EDIT }
        public static PAGE IdentifyPage(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            XmlNode root = doc.SelectSingleNode("api");
            if (root.HasChildNodes)
            {
                XmlNode cmd = root.FirstChild;
                switch (cmd.Name.ToLower())
                {
                    case "edit": return PAGE.EDIT;
                    case "login": return PAGE.LOGIN;
                    case "query": return IdentifyQuery(cmd);
                    case "action": return PAGE.ACTION;
                    case "list": return PAGE.LIST;
                    case "prop": return PAGE.PROP;
                    case "meta": return PAGE.META;
                }
            }            
            return PAGE.LOGOUT;
        }
        static PAGE IdentifyQuery(XmlNode node)
        {
            switch (node.FirstChild.Name.ToLower())
            {
                case "categorymembers": return PAGE.CATEGORIES;
                case "pages": return IdentifyPages(node.FirstChild);
                case "normalized": return IdentifyNormalized(node.FirstChild);
            }
            return PAGE.QUERY;
        }
        static PAGE IdentifyNormalized(XmlNode node)
        {
            if (node.FirstChild.Name.ToLower() == "n") return PAGE.IMAGES;
            if (node.FirstChild.FirstChild.Attributes["edittoken"] != null) return PAGE.EDITTOKEN;
            return PAGE.IMAGES;
        }
        static PAGE IdentifyPages(XmlNode node)
        {
            if (node.FirstChild.Attributes["edittoken"] != null) return PAGE.EDITTOKEN;
            if (node.FirstChild.Attributes["pageid"] != null) return PAGE.UNITS;
            return PAGE.QUERY;
        }
        public static Login ParseLogin(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            return parseLogin(doc.SelectSingleNode("api/login"));
        }
        static Login parseLogin(XmlNode node)
        {
            Login res = new Login();
            foreach (XmlAttribute a in node.Attributes)
            {
                switch (a.Name.ToLower())
                {
                    case "result": res.result = Login.LoginResultFromString(a.Value); break;
                    case "token": res.token = a.Value; break;
                    case "cookieprefix": res.cookieprefix = a.Value; break;
                    case "lguserid": res.lguserid = a.Value; break;
                    case "lgusername": res.lgusername = a.Value; break;
                    case "lgtoken": res.lgtoken = a.Value; break;
                    case "sessionid": res.sessionid = a.Value; break;
                }
            }
            return res;
        }
        public static List<Category> ParseCategories(string page, ref string continueStr)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            XmlNode continueNode = doc.SelectSingleNode("api/query-continue");
            if (continueNode != null)
                continueStr = continueNode.SelectSingleNode("categorymembers").Attributes["cmcontinue"].Value;
            return parseCategories(doc.SelectSingleNode("api/query/categorymembers"));
        }
        static List<Category> parseCategories(XmlNode node)
        {
            List<Category> categories = new List<Category>();
            foreach (XmlNode category in node.ChildNodes)
                categories.Add(parseCategory(category));
            return categories;
        }
        static Category parseCategory(XmlNode node)
        {
            Category category = new Category();
            foreach (XmlAttribute a in node.Attributes)
            {
                switch (a.Name.ToLower())
                {
                    case "pageid": category.pageid = ToInt(a.Value); break;
                    case "title": category.title = a.Value; break;
                }
            }
            return category;
        }
        public static List<Unit> ParseUnits(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            return parseUnits(doc.SelectSingleNode("api/query/pages"));
        }
        static List<Unit> parseUnits(XmlNode node)
        {
            List<Unit> cards = new List<Unit>();
            foreach (XmlNode card in node.ChildNodes)
                cards.Add(parseUnit(card));
            return cards;
        }
        static Unit parseUnit(XmlNode node)
        {
            Unit u = parseUnitInfo(node.SelectSingleNode("revisions/rev").FirstChild.Value);
            foreach (XmlAttribute a in node.Attributes)
            {
                switch (a.Name.ToLower())
                {
                    case "pageid": u.id = ToInt(a.Value); break;
                    case "title": u.name = a.Value; break;
                    case "touched": u.revision = Convert.ToDateTime(a.Value); break;
                    default : break;
                }
            }     
            return u;
        }
        static Unit parseUnitInfo(string text)
        {
            Unit u = new Unit();
            string p_pattern = "\\|(.*?)\\=(.*?)\\n";
            MatchCollection mc = Regex.Matches(text, p_pattern);
            int atk = 0, hp = 0, sk1 = 0, sk2 = 0;
            foreach (Match m in mc)
            {
                string propery = m.Groups[1].Value.ToString().ToLower().Trim();
                string oldvalue = m.Groups[2].Value.ToString().Trim();
                Regex regex = new Regex(@"(<br />|<br/>|<br>)+");
                string value = regex.Replace(oldvalue, "&lt;br&gt;");
                switch (propery)
                {
                    case "gender": u.gender = parseGender(value); break;
                    case "max evo": u.evos = ToInt(value); break;
                    case "rarity": u.rarity = value; break;
                    case "element": u.element = parseElement(value); break;
                    case "type": u.type = parseType(value); break;
                    case "cost": u.cost = ToInt(value); break;
                    case "atk 0":
                    case "atk 1":
                    case "atk 2":
                    case "atk 3":
                    case "atk 4":
                        atk = ToInt(value);
                        u.atk = atk > u.atk ? atk : u.atk;
                        break;
                    case "hp 0":
                    case "hp 1":
                    case "hp 2":
                    case "hp 3":
                    case "hp 4":
                        hp = ToInt(value);
                        u.hp = hp > u.hp ? hp : u.hp;
                        break;
                    case "1st skill name 0":
                    case "1st skill name 1":
                    case "1st skill name 2":
                    case "1st skill name 3":
                    case "1st skill name 4":
                    case "skill 1":
                    case "skill 1+":
                    case "skill 1 name":
                        u.skill1.name = value == string.Empty ? u.skill1.name : value;
                        break;
                    case "2nd skill name 0":
                    case "2nd skill name 1":
                    case "2nd skill name 2":
                    case "2nd skill name 3":
                    case "2nd skill name 4":
                    case "skill 2":
                    case "skill 2+":
                    case "skill 2 name":
                        u.skill2.name = value == string.Empty ? u.skill2.name : value;
                        break;
                    case "1st skill type 0":
                    case "1st skill type 1":
                    case "1st skill type 2":
                    case "1st skill type 3":
                    case "1st skill type 4":
                    case "skill 1 type":
                        u.skill1.type = value == string.Empty ? u.skill1.type : parseSkillType(value);
                        break;
                    case "2nd skill type 0":
                    case "2nd skill type 1":
                    case "2nd skill type 2":
                    case "2nd skill type 3":
                    case "2nd skill type 4":
                    case "skill 2 type":
                        u.skill2.type = value == string.Empty ? u.skill2.type : parseSkillType(value);
                        break;
                    case "1st skill desc 0":
                    case "1st skill desc 1":
                    case "1st skill desc 2":
                    case "1st skill desc 3":
                    case "1st skill desc 4":
                    case "skill 1 desc":
                        u.skill1.desc = value == string.Empty ? u.skill1.desc : parseSkillDescription(value);
                        break;
                    case "skill 1 desc+":
                    case "skill 1 desc++":
                    case "skill 1 desc ascension":
                        u.skill1.desc += "&lt;br&gt;" + parseSkillDescription(value);
                        break;
                    case "2nd skill desc 0":
                    case "2nd skill desc 1":
                    case "2nd skill desc 2":
                    case "2nd skill desc 3":
                    case "2nd skill desc 4":
                    case "skill 2 desc":
                        u.skill2.desc = value == string.Empty ? u.skill2.desc : parseSkillDescription(value);
                        break;
                    case "skill 2 desc+":
                    case "skill 2 desc++":
                    case "skill 2 desc ascension":
                        u.skill2.desc += "&lt;br&gt;" + parseSkillDescription(value);
                        break;
                    case "1st skill proc 0":
                    case "1st skill proc 1":
                    case "1st skill proc 2":
                    case "1st skill proc 3":
                    case "1st skill proc 4":
                    case "skill 1 proc":
                    case "skill 1 proc+":
                        sk1 = ToInt(value);
                        u.skill1.proc = sk1 > u.skill1.proc ? sk1 : u.skill1.proc;
                        break;
                    case "2nd skill proc 0":
                    case "2nd skill proc 1":
                    case "2nd skill proc 2":
                    case "2nd skill proc 3":
                    case "2nd skill proc 4":
                    case "skill 2 proc":
                    case "skill 2 proc+":
                        sk2 = ToInt(value);
                        u.skill2.proc = sk2 > u.skill2.proc ? sk2 : u.skill2.proc;
                        break;
                    case "ability":
                        u.ability = value;
                        break;
                    case "unreleased":
                        u.unreleased = value == "yes" ? true : false;
                        break;
                    default: break;
                }
            }
            return u;            
        }
        static string parseSkillType(string name)
        {
            switch (name.ToLower())
            {
                case "multi": return "Multi";
                case "multi&rush": return "Multi&Rush";
                case "passive": return "Passive";
                case "rush": return "Rush";
                case "support": return "Support";
                case "unique": return "Unique";
            }
            return string.Empty;
        }
        static string parseElement(string name)
        {
            switch (name.ToLower())
            {
                case "earth": return "Earth";
                case "fire": return "Fire";
                case "null": return "Null";
                case "water": return "Water";
            }
            return string.Empty;
        }
        static string parseGender(string name) //Only works in lower case 
        {
            return name.ToLower();
        }
        static string parseType(string name)
        {
            switch (name.ToLower())
            {
                case "flurry": return "Flurry";
                case "pound": return "Pound";
                case "slice": return "Slice";                   
            }
            return string.Empty;
        }
        static string parseSkillDescription(string description)
        {            
            if (description.IndexOf("|")>0)
            {
                return description.Split('|')[1];
            }
            return description;
        }
        public static string ParseImage(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            return parseImage(doc.SelectSingleNode("api/query/pages/page"));             
        }        
        static string parseImage(XmlNode node)
        {
            string res = string.Empty;
            foreach (XmlAttribute a in node.Attributes)
            {
                if (a.Name == "missing") return "";
                if (a.Name == "title") res = a.Value.Replace("File:", "");
            }
            return res;
        }
        public static string ParseEditToken(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            XmlNode node = doc.SelectSingleNode("api/query/pages/page");
            return node.Attributes["edittoken"].Value;  
        }
        public static string ParseEdit(string page)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(page);
            XmlNode node = doc.SelectSingleNode("api/edit");
            return node.Attributes["result"].Value;  
        }
        static int ToInt(this string s)
        {
            s = s.Replace(".", "").Replace(",", "");
            return IsNumeric(s) ? Convert.ToInt32(s) : 0;
        }
        static bool IsNumeric(this string s)
        {
            int output;
            bool res = int.TryParse(s, out output);
            return res;
        }
    }

}
