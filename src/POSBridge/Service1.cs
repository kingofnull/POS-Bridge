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
            // تابع پیش نیاز هر سرویس ویندوزی 
            // فراخوانی کامپوننت های مورد نیاز برای اجرا در ویندوز
            InitializeComponent();

        }

        // از سرگیری روال ها در صورت توقف سرویس
        protected override void OnContinue()
        {

        }

        // این ابجکت رخ داد، های سیستم را به نخ پاس می دهد
        private ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
        // تعریف نخ برای اجرای فرایند
        private Thread _thread;
        // تعریف لاگر برای خطا یابی برنامه
        LogWriter loger = new LogWriter("");

        // تابعه استارت سرویس در این تابع کدهای لازم برای شروع فعالیت های سرویس وارد می شود
        protected override void OnStart(string[] args)
        {
            // گرفتن مشخصات تنظیمات از متغیرهای تعریف شده در برنامه
            string Name = ConfigurationSettings.AppSettings.Get("MySetting");


            // باز تعریف نخ، برای فضا گیری در حافظه
            _thread = new Thread(WorkerThreadFunc);
            _thread.Name = "My Worker Thread";
            // این نخ باید در پس زمینه وظایف سیستم اجرا شود
            _thread.IsBackground = true;
            // استارت کردن نخ برای شروع فعالیت
            _thread.Start();


        }

        // این تابعی است که به نخ تعریف شده در بالا داده شده تا فعالیت های مورد نیاز را انجام دهد
        private void WorkerThreadFunc()
        {
            // نخ کنونی را بلاک کن تا زمانی که سیگنالی دریافت شود
            while (!_shutdownEvent.WaitOne(0))
            {
                // Replace the Sleep() call with the work you need to do
                // ادرس و پورت کنونی برای شنود
                var prefix = "http://localhost:8080/";
                // تعریف ساده ای از شنودگر تحت پروتکل اچ تی تی پی برای دریافت و ارسال داده ها به صورت جی سون
                HttpListener listener = new HttpListener(); ;
                // الحاق ادرس به شنودگر
                listener.Prefixes.Add(prefix);
                try
                {
                    // شنودگر تحت پروتکل اچ تی تی پی را فعال کن
                    listener.Start();
                }
                catch (HttpListenerException hlex)
                {
                    // در صورت وجود خطا پیامی چاپ کن
                    Console.WriteLine(hlex.Message);
                    return;
                }
                try
                {
                    // تا زمانی که داده ای از سمت وب پاس داده می شود
                    while (listener.IsListening)
                    {
                        // محتوای درخواست ارسال شده را بگیر
                        var context = listener.GetContext();

                        // درخواست را به تابع زیر ارسال کن
                        ProcessRequest(context);
                    }
                }
                catch (HttpListenerException hlex)
                {
                    // اگر خطایی وجود داشت شنودگر را ببند
                    listener.Close();
                    // نوع خطا لاگ کن
                    loger.LogWrite(hlex.Message);
                }

            }
        }

        // تابع اصلی که درخواست ارسال شده به پورت سیستم را پاسخ می دهد
        private static void ProcessRequest(HttpListenerContext context)
        {

            //Console.WriteLine(res2.Success.ToString());
            // Get the data from the HTTP stream
            // جداسازی داده ها از درخواست ارسال شده
            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
            // داده های ارسال شده را در کالکشنی واردکن
            NameValueCollection query = HttpUtility.ParseQueryString(body);
            //string Data;
            // تعریف کالکشن جدید برای ارسال خروجی این تابع
            Dictionary<string, string> httpResponse = new Dictionary<string, string>();
           try
            {
               // اگر درخواست ارسال شده شامل ای پی بود
                if (query.Get("IP") != "")
                {


                   try
                    {
                       // نوع درخواست ارسال شده بررسی می شود
                        switch (query.Get("Type"))
                        {
                            // اگر درخواست از نوع ای پی و برای بانک شهر بود
                            case "shahr":
                                #region PC POS Shahr bank
                                // تابع ارسال و دریافت درخواست به کلاس بانک شهر صدا زده می شود
                                Shahr shahrPos = new Shahr(httpResponse);
                                httpResponse = shahrPos.pay(query);
                                #endregion
                                break;

                            default:
                                // مشخص کردن خطا در خروجی نوع بانک مشخص نیست
                                httpResponse.Add("ErrorCode", "-8000");
                                httpResponse.Add("ErrorMessage", "لطفا نوع بانک را مشخص کنید");
                                 PrintToOutPut(httpResponse, context);
                                return;


                        }


                    }
                    catch (Exception e)
                    {
                        // مشخص کردن خطای زمان اجرا
                        httpResponse.Add("ErrorCode", "-9000");
                        httpResponse.Add("ErrorMessage", e.Message);
                    }

                }
                // اگر درخواست ارسال شده شامل فیلد پورت بود
                else if (query.Get("Port") != "")
                {

                    // بررسی نوع درخواست ارسال شده
                    switch (query.Get("Type"))
                    {
                        // اگر نوع بانک پارسیان بود
                        case "parsian":
                            #region PC POS Parsian bank
                            // تابع ارسال و دریافت درخواست به بانک پارسیان صدا زده می شود
                            Parsian parsianPos = new Parsian(httpResponse);
                            httpResponse = parsianPos.pay(query);
                            parsianPos = null;

                            #endregion
                            break;

                        default:
                            // تولید خطای خروجی - نوع بانک مشخص نمی  باشد
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
                // اگر در طول ارسال و دریافت خطایی رخ داد پیام مناسب تولید می شود 
                // و به سمت کاربر ارسال می شود
                httpResponse["ErrorCode"] =  "200";
                httpResponse["ErrorMessage"] =   e.Message;
                httpResponse["MerchantId"] =   "";
                httpResponse["TerminalId"] =   "";
                httpResponse["SerialNumber"] =   "";
                httpResponse["MerchantName"] =   "";

                httpResponse["Success"] =   "False";
                httpResponse["WarningCode"] =   "0";

                // لاگ کردن در صورتی که خطایی رخ داده بود
                LogWriter loger = new LogWriter("Start log in Main Thread ProcessRequest");
                loger.LogWrite(e.Message);

                // تابعی که نتیجه کار را به خروجی ارسال می کند
                PrintToOutPut(httpResponse, context);

            }
            


        }

        // تابع مورد نیاز برای نوشتن داده های تولید شده در خروجی 
        // خروجی به صورت جی سون بوده و به مرورگر ارسال می شود
        public static void PrintToOutPut(Dictionary<string, string> dictionary, HttpListenerContext context)
        {
            var json = new JavaScriptSerializer().Serialize(dictionary);

            // تولید داده و هدرهای مورد نیاز برای ارسال سمت مرورگر
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

        // زمانی که سرویس متوقف شد موارد زیر را انجام بده
        protected override void OnStop()
        {
            // ابجکتی که رخ داد های  ارسالی را کنترل می کند را بازنشانی کن
            _shutdownEvent.Set();
            // بعد از گذشت سه ثانیه نخ مورد نظر را متوقف کن
            if (!_thread.Join(3000))
            { // give the thread 3 seconds to stop
                _thread.Abort();
            }
        }
    }
}
