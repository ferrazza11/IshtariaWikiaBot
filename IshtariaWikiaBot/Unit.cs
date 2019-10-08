using System;
using System.IO;
using System.Xml.Serialization;

namespace IshtariaWikiaBot
{
    [Serializable]
    public class Unit
    {
        public int id { get; set; }
        public DateTime revision { get; set; } 
        public string rarity { get; set; }
        public string type { get; set; }
        public string element { get; set; }
        public string name { get; set; }
        public string gender { get; set; }
        public Skill skill1 { get; set; }
        public Skill skill2 { get; set; }
        public int atk { get; set; }
        public int hp { get; set; }
        public string ability { get; set; }
        public bool unreleased { get; set; }
        public bool hasimage { get; set; }
        public int cost { get; set; }
        public int evos { get; set; }
        public Unit()
        {
            skill1 = new Skill();
            skill2 = new Skill();
        }
        public string ToTableRow()
        {
            if (gender == null) return string.Empty; //discarting scrolls/grims/etc...
            if (unreleased) return string.Empty; //discarting unreleased units
            string res = string.Empty;
            res += "|-\n";
            res += ImageColumn;
            res += NameColumn;
            res += RarityColumn;
            res += TypeColumn;
            res += ElementColumn;
            res += Skill1Columns;
            res += Skill2Columns;
            res += AbilityColumn;
            res += ATKColumn;
            res += HPColumn;
            res += ScoreColumn;
            res += EvosColumn;
            res += CostColumn;            
            return res;
        }
        string ImageColumn { get { return "| class=\"" + gender + "\"| [[File:" + (hasimage ? name : "Empty-image") + ".png|40px]] \n"; } }
        string NameColumn { get { return "| class=\"left\"| [[" + name + "]]\n"; } }
        string RarityColumn { get { return "| " + rarity + "\n"; } }
        string TypeColumn { get { return "| " + type + "\n"; } }
        string ElementColumn { get { return "| " + element + "\n"; } }
        string Skill1Columns
        {
            get
            {
                string tmp = string.Empty;
                if (skill1.name == string.Empty)
                {
                    tmp += "| -\n";
                    tmp += "| -\n";
                }
                else
                {
                    tmp += "| {{UnitListSkill | type=" + skill1.type + " | name=" + skill1.name + " | desc=" + skill1.desc + "}}\n";
                    tmp += "| " + (skill1.proc > 0 ? skill1.proc.ToString() : "-") + "\n";
                }
                return tmp;
            }
        }
        string Skill2Columns
        {
            get
            {
                string tmp = string.Empty;
                if (skill2.name == string.Empty)
                {
                    tmp += "| -\n";
                    tmp += "| -\n";
                }
                else
                {
                    tmp += "| {{UnitListSkill | type=" + skill2.type + " | name=" + skill2.name + " | desc=" + skill2.desc + "}}\n";
                    tmp += "| " + (skill2.proc > 0 ? skill2.proc.ToString() : "-") + "\n";
                }
                return tmp;
            }
        }
        string AbilityColumn { get { return "| " + (ability == string.Empty ? "-" : ability) + "\n"; } }
        string EvosColumn { get { return "| " + (evos > 0 ? evos.ToString() : "-") + "\n"; } }
        string ATKColumn { get { return "| class=\"atk\"| " + atk + "\n"; } }
        string HPColumn { get { return "| class=\"hp\"| " + hp + "\n"; } }
        string CostColumn { get { return "| " + cost + "\n"; } }
        string ScoreColumn { get { return "| " + GetDeckScore.ToString().Replace(",", ".") + "\n"; } }
        double GetDeckScore
        {
            get
            {
                return (Convert.ToDouble(atk)) * 0.005 + (Convert.ToDouble(hp)) * 0.0015;
            }
        }
    }
    public class Skill
    {
        public string type { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public int proc { get; set; }
    }
}
