using System;
using System.Net;
using System.Text;
using System.Timers;
using System.Collections;
using System.IO;
using System.Collections.Generic;

namespace Tools.AsyncHttpRequest
{
    public class RequestPackage
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public string URL { get; set; }
        public bool AllowRedirect { get; set; }
        public bool Json { get; set; }        
        public byte[] postData { get; set; }
        public bool MultipartForm { get; set; }
        public string Boundary { get; set; }
        public CookieContainer Cookies { get; set; }  
    }
    public class RequestObject
    {
        public int RequestId { get; set; }
        public HttpWebRequest request;
        public ResponseObject responseInfo;        
        public RequestObject()
        {
            responseInfo = new ResponseObject();
        }
    }
    public class ResponseObject
    {
        public int RequestId { get; set; }
        public HttpWebResponse WebObject { get; set;}
        public string Document { get; set;}
    }
    public class ResponseObjectEventArgs : EventArgs
    {
        public ResponseObjectEventArgs(ResponseObject e) { Response = e; }             
        public ResponseObject Response { get; private set; }
    }
    public class AsyncWebRequest
    {
        public enum REQUEST_STATUS { IDLE, BUSY }
        public delegate void ResponseReceivedEvent(object sender, ResponseObjectEventArgs e);
        public event ResponseReceivedEvent OnResponseReceived;

        private Queue FiFo { get; set; }
        private CookieContainer Cookies { get; set; }
        public double Interval { get { return interval; } }
        private double interval = 500;
        public static System.Threading.ManualResetEvent allDone;
        public static REQUEST_STATUS Status { get; set; }
        private bool working { set; get; }
        private long lastRequestId { get; set; }
        Timer t;
        public AsyncWebRequest(double interval = 500)
        {
            this.interval = interval;
            Cookies = new CookieContainer();
            FiFo = new Queue();
            t = new Timer(Interval);
            t.Elapsed += t_Elapsed;
        }
        public int AddPostMultyPartForm(int requestId, string URL, Dictionary<string, object> parameters, CookieContainer newCookies = null)
        {
            string Boundary = string.Format("----------{0:N}", Guid.NewGuid());
            return AddToFifo(new RequestPackage() { Id = requestId, URL = URL, Cookies = Cookies, Method = "POST", postData = GetMultipartFormData(parameters, Boundary), MultipartForm = true, Boundary = Boundary });
        }
        public int AddPost(int requestId, string URL, string postData,  CookieContainer newCookies = null, bool Json = false)
        {
            return AddPost(requestId, URL, Encoding.ASCII.GetBytes(postData), newCookies, Json);
        }
        public int AddPost(int requestId, string URL, byte[] postData = null, CookieContainer newCookies = null, bool Json = false)
        {
            return AddToFifo(new RequestPackage() { Id = requestId, URL = URL, Cookies = Cookies, Method = "POST", postData = postData, Json = Json });
        }
        public int AddGet(int requestId, string URL, CookieContainer newCookies = null, bool AlloWRedirect = false)
        {
            return AddToFifo(new RequestPackage() { Id = requestId, URL = URL, Cookies = newCookies != null ? newCookies : Cookies, Method = "GET", postData = null, AllowRedirect = AlloWRedirect });            
        }
        private int AddToFifo(RequestPackage rp)
        {
            FiFo.Enqueue(rp);
            if (!working)
            {
                working = true;
                t.Start();                
            }
            return FiFo.Count;
        }
        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            t.Stop();
            if (FiFo.Count > 0 && Status == REQUEST_STATUS.IDLE)
            {
                RequestPackage o = (RequestPackage)FiFo.Dequeue();
                MakeRequest(o);
            }            
            t.Start();
        }        
        ~AsyncWebRequest()
        {
            t.Stop();
            working = false;
        }
        void MakeRequest(RequestPackage request)
        {
            try
            {
                Status = REQUEST_STATUS.BUSY;
                Cookies = request.Cookies == null ? Cookies : request.Cookies;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                
                HttpWebRequest req = WebRequest.Create(request.URL) as HttpWebRequest;                
                req.ProtocolVersion = HttpVersion.Version10;
                req.Method = request.Method;
                req.AllowAutoRedirect = request.AllowRedirect;
                req.KeepAlive = true;                
                req.ContentType = "text/html, application/xhtml+xml, image/jxr, */*";
                req.CookieContainer = Cookies;
                if (request.Method == "POST")
                {
                    if (request.Json)
                        req.ContentType = "application/json";                    
                    else if(request.MultipartForm)
                        req.ContentType = "multipart/form-data; boundary=" + request.Boundary;
                    else
                        req.ContentType = "application/x-www-form-urlencoded";
                    req.ContentLength = 0;
                    if (request.postData != null)
                    {
                        req.ContentLength = request.postData.Length;
                        Stream outputStream = req.GetRequestStream();
                        outputStream.Write(request.postData, 0, request.postData.Length);
                        outputStream.Close();
                    }
                }
                RequestObject ro = new RequestObject();
                ro.RequestId = request.Id;
                ro.request = req;
                allDone = new System.Threading.ManualResetEvent(false);
                IAsyncResult res = (IAsyncResult)req.BeginGetResponse(new AsyncCallback(ReadCallBack), ro);
                allDone.WaitOne();
                OnResponseReceived(this, new ResponseObjectEventArgs(ro.responseInfo));
                Status = REQUEST_STATUS.IDLE;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        protected static void ReadCallBack(IAsyncResult ar)
        {
            RequestObject req = (RequestObject)ar.AsyncState;
            req.responseInfo.RequestId = req.RequestId;            
            req.responseInfo.WebObject = req.request.GetResponse() as HttpWebResponse;
            Stream responseStream = req.responseInfo.WebObject.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            req.responseInfo.Document = NormalizeDocument(reader.ReadToEnd());            
            reader.Close();
            allDone.Set();            
        }
        private static string NormalizeDocument(string document)
        {            
            for (int i = 32; i < 257; i++)
                document = document.Replace("&#" + i + ";", ((char)i).ToString());
            return document;
        }
        private static readonly Encoding encoding = Encoding.UTF8;
        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;
            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0 , encoding.GetByteCount("\r\n"));

                needsCLRF = true;
                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0 , encoding.GetByteCount(header));
                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0 , fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0 , encoding.GetByteCount(postData));
                }
            }
            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0 , encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0 , formData.Length);
            formDataStream.Close();
            return formData;
        }
        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
}
