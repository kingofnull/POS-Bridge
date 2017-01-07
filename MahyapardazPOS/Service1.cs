using System.IO;
using System.Net;
using System.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Collections;
using System.Collections.Specialized;
using System.ServiceProcess;
using System.Diagnostics;
using System.Web.Script.Serialization;

using System.ComponentModel;
using System.Data;
using System.Drawing;

using System.Threading;
using System.Configuration;
using System.Net.NetworkInformation;
using System.IO.Ports;



namespace PAX.PCPOS.WinSample
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();

        }

        protected override void OnContinue()
        {

        }

        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        private Thread _thread;
        LogWriter loger = new LogWriter("");

        protected override void OnStart(string[] args)
        {
            string Name = ConfigurationSettings.AppSettings.Get("MySetting");


            _thread = new Thread(WorkerThreadFunc);
            _thread.Name = "My Worker Thread";
            _thread.IsBackground = true;
            _thread.Start();


        }

        private void WorkerThreadFunc()
        {
            while (!_shutdownEvent.WaitOne(0))
            {
                // Replace the Sleep() call with the work you need to do
                var prefix = "http://localhost:8080/";
                HttpListener listener = new HttpListener(); ;
                listener.Prefixes.Add(prefix);
                try
                {
                    listener.Start();
                }
                catch (HttpListenerException hlex)
                {
                    Console.WriteLine(hlex.Message);
                    return;
                }
                try
                {
                    while (listener.IsListening)
                    {
                        var context = listener.GetContext();

                        ProcessRequest(context);
                    }
                }
                catch (HttpListenerException hlex)
                {
                    listener.Close();
                    loger.LogWrite(hlex.Message);
                }

            }
        }

        private static void ProcessRequest(HttpListenerContext context)
        {

            //Console.WriteLine(res2.Success.ToString());
            // Get the data from the HTTP stream
            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
            NameValueCollection query = HttpUtility.ParseQueryString(body);
            //string Data;
            Dictionary<string, string> httpResponse = new Dictionary<string, string>();
           try
            {
                if (query.Get("IP") != "")
                {


                   try
                    {
                        switch (query.Get("Type"))
                        {

                            case "shahr":
                                #region PC POS Shahr bank
                                Shahr shahrPos = new Shahr(httpResponse);
                                httpResponse = shahrPos.pay(query);
                                #endregion
                                break;

                            default:
                                httpResponse.Add("ErrorCode", "-8000");
                                httpResponse.Add("ErrorMessage", "لطفا نوع بانک را مشخص کنید");
                                 PrintToOutPut(httpResponse, context);
                                return;


                        }


                    }
                    catch (Exception e)
                    {
                        httpResponse.Add("ErrorCode", "-9000");
                        httpResponse.Add("ErrorMessage", e.Message);
                    }

                }

                else if (query.Get("Port") != "")
                {


                    switch (query.Get("Type"))
                    {
                        case "parsian":
                            #region PC POS Parsian bank
                            Parsian parsianPos = new Parsian(httpResponse);
                            httpResponse = parsianPos.pay(query);
                            parsianPos = null;

                            #endregion
                            break;

                        default:
                            httpResponse.Add("ErrorCode", "-8000");
                            httpResponse.Add("ErrorMessage", "لطفا نوع بانک را مشخص کنید");
                            PrintToOutPut(httpResponse, context);
                            return;
                           
                    }

                }

                PrintToOutPut(httpResponse, context);

            }
            catch (Exception e)
            {
                httpResponse["ErrorCode"] =  "200";
                httpResponse["ErrorMessage"] =   e.Message;
                httpResponse["MerchantId"] =   "";
                httpResponse["TerminalId"] =   "";
                httpResponse["SerialNumber"] =   "";
                httpResponse["MerchantName"] =   "";

                httpResponse["Success"] =   "False";
                httpResponse["WarningCode"] =   "0";

                LogWriter loger = new LogWriter("Start log in Main Thread ProcessRequest");
                loger.LogWrite(e.Message);

                PrintToOutPut(httpResponse, context);

            }
            


        }

        public static void PrintToOutPut(Dictionary<string, string> dictionary, HttpListenerContext context)
        {
            var json = new JavaScriptSerializer().Serialize(dictionary);

            byte[] b = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = 200;
            context.Response.KeepAlive = false;
            context.Response.ContentLength64 = b.Length;
            context.Response.Headers.Add("Access-Control-Allow-Origin: *");
            // System.Threading.Thread.Sleep(10000);
            var output = context.Response.OutputStream;
            output.Write(b, 0, b.Length);

            context.Response.Close();
        }


        protected override void OnStop()
        {
            _shutdownEvent.Set();
            if (!_thread.Join(3000))
            { // give the thread 3 seconds to stop
                _thread.Abort();
            }
        }
    }
}
