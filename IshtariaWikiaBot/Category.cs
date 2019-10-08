using System;

namespace IshtariaWikiaBot
{
    [Serializable]
    public class Category
    {
        public long pageid { get; set; }        
        public string title { get; set; }
    }
}
