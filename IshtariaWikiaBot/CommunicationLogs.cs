using System;

namespace IshtariaWikiaBot
{
    [Serializable]
    public class CommunicationLogs
    {
        public enum CommunicationState : byte { GET, MOVE, RECEIVED, MOVED, TOUT, ERROR }
        public int Id { get; set; }
        public string Request { get; set; }
        public CommunicationState State { get; set; }
        public string Info { get; set; }
        public CommunicationLogs() { }
    }
}