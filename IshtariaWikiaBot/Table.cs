using System.Collections.Generic;

namespace IshtariaWikiaBot
{
    public static class Table
    {
        private static string Header
        {
            get
            {
                string res = string.Empty;
                res += "{{Warning|info=This is a bot generated page. If you notice any missing or incorrect data, please edit the corresponding unit page and it will be updated here next time the bot runs.}}\n";
                res +="{| class=\"wikitable filterable sortable center\"\n";
                res += "|-\n";
                res += "! class=\"unsortable\"| Image\n";
                res += "! class=\"unfilterable\"| Name\n";
                res += "! class=\"unsortable\"| Rarity\n";
                res += "! class=\"unsortable\"| Type\n";
                res += "! class=\"unsortable\"| Element\n";
                res += "! class=\"unsortable\"| Skill 1\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Skill 1 %\n";
                res += "! class=\"unsortable\"| Skill 2\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Skill 2 %\n";
                res += "! class=\"unfilterable\"| Ability\n";                
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Max ATK\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Max HP\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Deck Score\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Max Evo\n";
                res += "! class=\"unfilterable\" data-sort-type=\"number\"| Cost\n"; 
                return res;
            }
        }
        private static string Foot { get { return "|}"; } }

        public static string GetTable(List<Unit> Units)
        {
            string res = "";            
            foreach (Unit u in Units)            
                res += u.ToTableRow();
            
            return Header + res + Foot ;
        }       
    }
}
