using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

// توجه شود که نیم اسپیس باید همین باشد 
// لایبرری بانک در این نیم اسپیس است
namespace PAX.PCPOS.WinSample
{
    // ارسال و دریافت اطلاعات به کارت خوان بانک شهر
    class Shahr
    {
        // مشخص کردن ابجکتی برای خروجی کلاس
        private Dictionary<string, string> httpResponse;
        // تعریف لاگر برای ثبت خطاها و تغییرات
        LogWriter loger = new LogWriter("");

        public Shahr(Dictionary<string, string> httpResponse)
        {
            // گرفتن فضا برای خروجی
            this.httpResponse = new Dictionary<string, string>();
            this.httpResponse = httpResponse;
        }

        // فراهم کردن پارامتر ها و سپس ارسال به تابع بانک
        public Dictionary<string, string> pay(NameValueCollection query)
        {
            // جداسازی ای پی از تنظیمات
            string IP = query.Get("IP");
            // بررسی اعتبار ای پی وارد شده
            // آیا این ای پی وارد شده پینگ می دهد یا نه
            if (PingHost(IP) == false)
            {
                // تولید خطای مناسب
                httpResponse.Add("ErrorCode", "200");
                httpResponse.Add("ErrorMessage", "ip جواب نمیده");
                httpResponse.Add("MerchantId", "");
                httpResponse.Add("TerminalId", "");
                httpResponse.Add("SerialNumber", "");
                httpResponse.Add("MerchantName", "");

                httpResponse.Add("Success", "False");
                httpResponse.Add("WarningCode", "0");
                // PrintToOutPut(httpResponse, context);
                return httpResponse;
            }
            // ارسال تنظیمات به تابع بانک و گرفتن خروجی
            PCPOS.PosCommander.Instance.Init(IPAddress.Parse(IP), Int32.Parse(query.Get("TimeOut")));
            Response res2 = PCPOS.PosCommander.Instance.BillPayment(query.Get("AdditionalData"), query.Get("BillId"), query.Get("PaymentId"), Boolean.Parse(query.Get("Print")));

            httpResponse.Add("ErrorCode", res2.ErrorCode.ToString());

            // if user cancel the pay operation

            if (res2.ErrorCode == 3)
                httpResponse.Add("ErrorMessage", "لغو درخواست");
            
            // فراهم کردن داده ها برای خروجی
            httpResponse.Add("MerchantId", res2.PosInformation.MerchantId);
            httpResponse.Add("TerminalId", res2.PosInformation.TerminalId);
            httpResponse.Add("SerialNumber", res2.PosInformation.SerialNumber);
            httpResponse.Add("MerchantName", res2.PosInformation.MerchantName);

            httpResponse.Add("Success", res2.Success.ToString());
            httpResponse.Add("WarningCode", res2.WarningCode.ToString());
            if (res2.TransactionInfo != null)
            {
                httpResponse.Add("Amount", res2.TransactionInfo.Amount);
                httpResponse.Add("DateTime", res2.TransactionInfo.DateTime);
                httpResponse.Add("PAN", res2.TransactionInfo.PAN);
                httpResponse.Add("ResponseCode", res2.TransactionInfo.ResponseCode);
                httpResponse.Add("ErrorMessage", translateError(res2.TransactionInfo.ResponseCode));
                httpResponse.Add("RRN", res2.TransactionInfo.RRN);
                httpResponse.Add("Stan", res2.TransactionInfo.Stan);
                httpResponse.Add("SVC", res2.TransactionInfo.SVC);

                loger.LogWrite(translateError(res2.TransactionInfo.ResponseCode));
            }
            else
            {
                httpResponse.Add("Amount", "");
                httpResponse.Add("DateTime", "");
                httpResponse.Add("PAN", "");
                httpResponse.Add("ResponseCode", "");
                httpResponse.Add("RRN", "");
                httpResponse.Add("Stan", "");
                httpResponse.Add("SVC", "");
            }

            return httpResponse;

        }

        // آیا این ای پی پینک می دهد
        public static bool PingHost(string nameOrAddress)
        {
            bool pingable = false;
            Ping pinger = new Ping();
            try
            {
                PingReply reply = pinger.Send(nameOrAddress);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {

                // Discard PingExceptions and return false;
            }
            return pingable;
        }

        // تابعی که کد خطای تولید شده را به معال آن تبدیل میکند و مختص همین بانک است
        public string translateError(string errorCode)
        {
            string text = "خطا در پرداخت:";

            int errorValue = int.Parse(errorCode);

            switch (errorValue)
            {
                case 0:
                    text += "تراکنش موفق انجام شده است.";
                    break;
                case 1:
                    text += "از انجام تراکنش صرف نظر شد.";
                    break;
                case 2:
                    text += "شده است reverse قبلا.";
                    break;
                case 3:
                    text += "پذیرنده کارت نامعتبر است.";
                    break;
                case 5:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 6:
                    text += "خطای داخلی.";
                    break;
                case 9:
                    text += "سیستم مشغول است - لطفا بعدا تراکنش ارسال نمائید.";
                    break;
                case 10:
                    text += "Partial Dispense.";
                    break;
                case 12:
                    text += "تراکنش نامعتبر.";
                    break;
                case 13:
                    text += "مبلغ نادرست.";
                    break;
                case 14:
                    text += "شماره کارت شناخته شده نیست.";
                    break;
                case 15:
                    text += "صادر کننده ناشناخته میباشد.";
                    break;
                case 17:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 19:
                    text += "ورود مجدد تراکنش.";
                    break;
                case 20:
                    text += "کد پاسخ نامعتبر است.";
                    break;
                case 21:
                    text += "هیچ عملی انجام نمیگیرد.";
                    break;
                case 22:
                    text += "(System Malfunction) عملکرد نادرست سیستم.";
                    break;
                case 25:
                    text += "رکورد پیدا نشد.";
                    break;
                case 30:
                    text += "قالب پیام نامعتبر است.";
                    break;
                case 31:
                    text += "پذیرنده کارت نامعتبر است.";
                    break;
                case 32:
                    text += "به دلیل آن که وجه بصورت کامل به مشتری پرداخت نشده، تراکنش اصلاحیه آن صادر شده است.";
                    break;
                case 33:
                    text += "تاریخ انقضای کارت سپری شده است.";
                    break;
                case 34:
                    text += "NOT approval";
                    break;
                case 36:
                    text += "کارت محدود شده است.";
                    break;
                case 38:
                    text += "از حد مجاز گذشته است (PIN) ورود شماره شناسائی فردی.";
                    break;
                case 39:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 40:
                    text += "تراکنش نامشخص.";
                    break;
                case 41:
                    text += "کارت مفقود شده یا مسدود موقت است.";
                    break;
                case 42:
                    text += "کارت دزدیده یا مسدود دائم است.";
                    break;
                case 51:
                    text += "کمبود وجه.";
                    break;
                case 54:
                    text += "تاریخ استفاده از کارت به پایان رسیده است.";
                    break;
                case 55:
                    text += "رمز نامعتبر است.";
                    break;
                case 56:
                    text += "کارت نامعتبر.";
                    break;
                case 57:
                    text += "تراکنش غیر مجاز.";
                    break;
                case 58:
                    text += "تراکنش غیر مجاز.";
                    break;
                case 61:
                    text += "مبلغ بیش از حد مجاز.";
                    break;
                case 62:
                    text += "کارت محدود شده است.";
                    break;
                case 64:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 65:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 66:
                    text += "شماره حساب فعال نیست.";
                    break;
                case 67:
                    text += "شد Capture کارت.";
                    break;
                case 68:
                    text += "جواب دریافتی با تاخیر آمده است.";
                    break;
                case 75:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 76:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 77:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                case 78:
                    text += "کارت فعال نیستد.";
                    break;
                case 79:
                    text += "حساب تعریف نشده است.";
                    break;
                case 80:
                    text += "پردازش تراکنش دارای اشکال است.";
                    break;
                case 84:
                    text += "عدم دریافت پاسخ.";
                    break;
                case 85:
                    text += "نامعتبر است Originator شماره.";
                    break;
                case 90:
                    text += "Cutoff درحال پردازش.";
                    break;
                case 91:
                    text += "عدم دریافت پاسخ.";
                    break;
                case 92:
                    text += "صادر کننده کارت نامعتبر است.";
                    break;
                case 93:
                    text += "تراکنش کامل نشده است.";
                    break;
                case 94:
                    text += "تراکنش تکراری است.";
                    break;
                case 96:
                    text += "از انجام تراکنش صرفنظر شد.";
                    break;
                default:
                    text += "..";
                    break;
            }

            return text;
        }

    }
}
